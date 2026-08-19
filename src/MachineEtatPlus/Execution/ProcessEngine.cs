using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MachineEtatPlus.Execution;

/// <summary>
/// Implementation du moteur. Un appel public represente un pas d'avancement effectue dans la
/// transaction de l'application cliente : le moteur enchaine les noeuds tant qu'il le peut,
/// puis rend la main sans jamais valider ni annuler la transaction.
/// </summary>
public sealed class ProcessEngine : IProcessEngine
{
    private readonly IProcessDefinitionProvider _definitions;
    private readonly IProcessStore _store;
    private readonly IProcessJobScheduler _scheduler;
    private readonly IDbSessionAccessor _sessions;
    private readonly ICommandDispatcher _commands;
    private readonly IDecisionDispatcher _decisions;
    private readonly IProcessClock _clock;
    private readonly ProcessEngineOptions _options;
    private readonly IServiceProvider? _services;
    private readonly ILogger _logger;

    /// <summary>Cree le moteur.</summary>
    /// <param name="definitions">Catalogue des definitions.</param>
    /// <param name="store">Persistance implementee par l'application cliente.</param>
    /// <param name="scheduler">Gestion de taches implementee par l'application cliente.</param>
    /// <param name="sessions">Acces a la connexion et a la transaction de l'application cliente.</param>
    /// <param name="commands">Routage des commandes metier ; <c>null</c> pour n'autoriser que les traitements en ligne.</param>
    /// <param name="decisions">Routage des requetes de decision ; <c>null</c> pour n'autoriser que les evaluations en ligne.</param>
    /// <param name="clock">Horloge ; <c>null</c> pour l'horloge systeme.</param>
    /// <param name="options">Reglages ; <c>null</c> pour les valeurs par defaut.</param>
    /// <param name="services">Conteneur expose aux traitements via le contexte.</param>
    /// <param name="logger">Journal technique.</param>
    public ProcessEngine(
        IProcessDefinitionProvider definitions,
        IProcessStore store,
        IProcessJobScheduler scheduler,
        IDbSessionAccessor sessions,
        ICommandDispatcher? commands = null,
        IDecisionDispatcher? decisions = null,
        IProcessClock? clock = null,
        ProcessEngineOptions? options = null,
        IServiceProvider? services = null,
        ILogger<ProcessEngine>? logger = null)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _commands = commands ?? UnavailableDispatcher.Instance;
        _decisions = decisions ?? UnavailableDispatcher.Instance;
        _clock = clock ?? SystemProcessClock.Instance;
        _options = options ?? new ProcessEngineOptions();
        _services = services;
        _logger = logger ?? (ILogger)NullLogger<ProcessEngine>.Instance;
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var run = NewRun();
        ProcessDefinition definition = _definitions.Get(request.DefinitionKey, request.Version);

        var variables = new ProcessVariables();
        if (request.Variables is not null)
        {
            variables.CopyFrom(request.Variables);
        }

        ProcessInstance instance = ProcessInstance.Create(
            request.InstanceId ?? Guid.NewGuid(),
            definition.Key,
            definition.Version,
            definition.StartNodeId,
            _clock.UtcNow,
            variables);
        instance.CorrelationId = request.CorrelationId;

        await _store.InsertInstanceAsync(instance, cancellationToken).ConfigureAwait(false);
        run.Instances[instance.Id] = instance;
        await AppendHistoryAsync(instance, ProcessEventType.Started, null, null, definition.ToString(), cancellationToken).ConfigureAwait(false);

        run.Ready.Enqueue(new PendingRun(instance, null));
        await DriveAsync(run, cancellationToken).ConfigureAwait(false);
        return BuildResult(instance, run);
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> ResumeAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var run = NewRun();
        ProcessInstance instance = await LoadAsync(run, instanceId, cancellationToken).ConfigureAwait(false);

        if (instance.Status != ProcessStatus.Running)
        {
            return BuildResult(instance, run);
        }

        run.Ready.Enqueue(new PendingRun(instance, null));
        await DriveAsync(run, cancellationToken).ConfigureAwait(false);
        return BuildResult(instance, run);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessExecutionResult>> SignalAsync(SignalRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        RequireSession();

        IReadOnlyList<ProcessWait> waits;
        if (request.InstanceId.HasValue)
        {
            IReadOnlyList<ProcessWait> all = await _store.GetWaitsAsync(request.InstanceId.Value, cancellationToken).ConfigureAwait(false);
            waits = all
                .Where(w => w.Kind == WaitKind.Signal && string.Equals(w.SignalName, request.SignalName, StringComparison.Ordinal))
                .ToList();
        }
        else
        {
            waits = await _store
                .FindSignalWaitsAsync(request.SignalName, request.CorrelationKey, request.MaxInstances, cancellationToken)
                .ConfigureAwait(false);
        }

        var results = new List<ProcessExecutionResult>(waits.Count);
        foreach (ProcessWait wait in waits)
        {
            results.Add(await ResumeFromWaitAsync(
                wait,
                NodeOutcomes.Signal,
                request.Payload,
                $"signal '{request.SignalName}'",
                cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> CompleteInteractionAsync(
        InteractionCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        RequireSession();

        IReadOnlyList<ProcessWait> waits = await _store.GetWaitsAsync(request.InstanceId, cancellationToken).ConfigureAwait(false);
        ProcessWait? wait = waits.FirstOrDefault(w =>
            w.Kind == WaitKind.Interaction
            && (request.NodeId is null || string.Equals(w.NodeId, request.NodeId, StringComparison.Ordinal)));

        if (wait is null)
        {
            var run = NewRun();
            ProcessInstance current = await LoadAsync(run, request.InstanceId, cancellationToken).ConfigureAwait(false);
            throw new ProcessEngineException(
                $"L'instance '{request.InstanceId}' n'attend aucune interaction" +
                (request.NodeId is null ? string.Empty : $" sur le noeud '{request.NodeId}'") +
                $" (etat : {current.Status}, noeud : '{current.CurrentNodeId}').");
        }

        return await ResumeFromWaitAsync(
            wait,
            request.Outcome,
            request.Payload,
            $"interaction '{wait.InteractionKey}'",
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessExecutionResult>> FireDueTimersAsync(int maxCount = 50, CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Le nombre d'attentes a traiter doit etre strictement positif.");
        }

        RequireSession();

        IReadOnlyList<ProcessWait> waits = await _store
            .FindDueTimerWaitsAsync(_clock.UtcNow, maxCount, cancellationToken)
            .ConfigureAwait(false);

        var results = new List<ProcessExecutionResult>(waits.Count);
        foreach (ProcessWait wait in waits)
        {
            results.Add(await ResumeFromWaitAsync(wait, NodeOutcomes.Timer, null, "echeance", cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> FireTimerAsync(Guid instanceId, Guid? waitId = null, CancellationToken cancellationToken = default)
    {
        RequireSession();

        IReadOnlyList<ProcessWait> waits = await _store.GetWaitsAsync(instanceId, cancellationToken).ConfigureAwait(false);
        ProcessWait? wait = waits.FirstOrDefault(w =>
            w.Kind == WaitKind.Timer && (waitId is null || w.Id == waitId.Value));

        if (wait is null)
        {
            var run = NewRun();
            ProcessInstance current = await LoadAsync(run, instanceId, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                "Echeance ignoree : l'instance {InstanceId} n'attend plus de date/heure (etat {Status}).",
                instanceId,
                current.Status);
            return BuildResult(current, run);
        }

        return await ResumeFromWaitAsync(wait, NodeOutcomes.Timer, null, "echeance", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> CancelAsync(Guid instanceId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var run = NewRun();
        ProcessInstance root = await LoadAsync(run, instanceId, cancellationToken).ConfigureAwait(false);

        if (root.IsFinished)
        {
            return BuildResult(root, run);
        }

        var pending = new Stack<ProcessInstance>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            ProcessInstance instance = pending.Pop();
            if (instance.IsFinished)
            {
                continue;
            }

            DateTimeOffset now = _clock.UtcNow;
            instance.Status = ProcessStatus.Cancelled;
            instance.CompletedAt = now;
            instance.UpdatedAt = now;

            await _store.DeleteWaitsAsync(instance.Id, cancellationToken).ConfigureAwait(false);
            await _scheduler.CancelAsync(instance.Id, cancellationToken).ConfigureAwait(false);
            await AppendHistoryAsync(instance, ProcessEventType.Cancelled, null, null, reason, cancellationToken).ConfigureAwait(false);
            await _store.UpdateInstanceAsync(instance, cancellationToken).ConfigureAwait(false);

            IReadOnlyList<ProcessInstance> children = await _store
                .GetChildInstancesAsync(instance.Id, cancellationToken)
                .ConfigureAwait(false);

            foreach (ProcessInstance child in children)
            {
                pending.Push(Track(run, child));
            }
        }

        await NotifyParentAsync(root, run, cancellationToken).ConfigureAwait(false);
        await DriveAsync(run, cancellationToken).ConfigureAwait(false);
        return BuildResult(root, run);
    }

    private async Task<ProcessExecutionResult> ResumeFromWaitAsync(
        ProcessWait wait,
        string? outcome,
        object? payload,
        string reason,
        CancellationToken cancellationToken)
    {
        var run = NewRun();
        ProcessInstance instance = await LoadAsync(run, wait.InstanceId, cancellationToken).ConfigureAwait(false);

        if (instance.Status != ProcessStatus.Waiting)
        {
            _logger.LogDebug(
                "Reveil ignore ({Reason}) : l'instance {InstanceId} n'est plus en attente (etat {Status}).",
                reason,
                instance.Id,
                instance.Status);
            return BuildResult(instance, run);
        }

        ProcessDefinition definition = _definitions.Get(instance.DefinitionKey, instance.DefinitionVersion);
        NodeDefinition node = definition.GetNode(wait.NodeId);

        await ReleaseWaitsAsync(instance, cancellationToken).ConfigureAwait(false);

        string? payloadVariable = node switch
        {
            WaitNodeDefinition waitNode => waitNode.PayloadVariable,
            InteractiveNodeDefinition interactive => interactive.PayloadVariable,
            _ => null,
        };

        if (payloadVariable is not null)
        {
            instance.Variables.SetObject(payloadVariable, payload);
        }

        var context = new NodeExecutionContext(
            definition,
            node,
            instance,
            run.Session,
            _services,
            _clock.UtcNow,
            payload,
            cancellationToken);

        string? target = ResolveTarget(node, outcome, context)
                         ?? throw new ProcessTransitionException(definition.Key, node.Id, outcome);

        instance.CurrentNodeId = target;
        instance.Status = ProcessStatus.Running;
        await AppendHistoryAsync(instance, ProcessEventType.Resumed, node, outcome, reason, cancellationToken).ConfigureAwait(false);

        run.Ready.Enqueue(new PendingRun(instance, payload));
        await DriveAsync(run, cancellationToken).ConfigureAwait(false);
        return BuildResult(instance, run);
    }

    private async Task DriveAsync(RunState run, CancellationToken cancellationToken)
    {
        while (run.Ready.Count > 0)
        {
            PendingRun pending = run.Ready.Dequeue();
            await RunInstanceAsync(pending, run, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunInstanceAsync(PendingRun pending, RunState run, CancellationToken cancellationToken)
    {
        ProcessInstance instance = pending.Instance;
        object? input = pending.Input;
        ProcessDefinition definition = _definitions.Get(instance.DefinitionKey, instance.DefinitionVersion);

        while (instance.Status == ProcessStatus.Running)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (run.StepsRemaining <= 0)
            {
                await DeferAsync(instance, run, cancellationToken).ConfigureAwait(false);
                break;
            }

            run.StepsRemaining--;
            run.StepsExecuted++;

            NodeDefinition node = definition.GetNode(instance.CurrentNodeId);
            var context = new NodeExecutionContext(
                definition,
                node,
                instance,
                run.Session,
                _services,
                _clock.UtcNow,
                input,
                cancellationToken);
            input = null;

            NodeStep step;
            try
            {
                step = await ExecuteNodeAsync(definition, node, instance, context, run, cancellationToken).ConfigureAwait(false);

                if (step.Action == StepAction.Continue)
                {
                    string target = ResolveTarget(node, step.Outcome, context)
                                    ?? throw new ProcessTransitionException(definition.Key, node.Id, step.Outcome);
                    step = NodeStep.MoveTo(target, step.Outcome);
                }
            }
            catch (Exception exception) when (ShouldCapture(exception))
            {
                step = NodeStep.Fault(exception);
            }

            switch (step.Action)
            {
                case StepAction.MoveTo:
                    await AppendHistoryAsync(instance, ProcessEventType.NodeExecuted, node, step.Outcome, null, cancellationToken).ConfigureAwait(false);
                    instance.CurrentNodeId = step.Target!;
                    break;

                case StepAction.Block:
                    await AppendHistoryAsync(instance, ProcessEventType.Waiting, node, null, step.WaitKind.ToString(), cancellationToken).ConfigureAwait(false);
                    instance.Status = ProcessStatus.Waiting;
                    run.Blocked[instance.Id] = step.WaitKind!.Value;
                    break;

                case StepAction.Finish:
                    await FinishAsync(instance, node, step, cancellationToken).ConfigureAwait(false);
                    break;

                case StepAction.Fault:
                    await FaultAsync(instance, node, step.Error!, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new ProcessEngineException($"Action d'avancement inattendue : {step.Action}.");
            }
        }

        instance.UpdatedAt = _clock.UtcNow;
        await _store.UpdateInstanceAsync(instance, cancellationToken).ConfigureAwait(false);

        if (instance.IsFinished)
        {
            await NotifyParentAsync(instance, run, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<NodeStep> ExecuteNodeAsync(
        ProcessDefinition definition,
        NodeDefinition node,
        ProcessInstance instance,
        INodeExecutionContext context,
        RunState run,
        CancellationToken cancellationToken)
    {
        switch (node)
        {
            case StartNodeDefinition:
                return NodeStep.Continue(null);

            case BusinessNodeDefinition business:
            {
                string? outcome = business.Handler is not null
                    ? await business.Handler(context, cancellationToken).ConfigureAwait(false)
                    : await _commands.DispatchAsync(business.CommandFactory!(context), context, cancellationToken).ConfigureAwait(false);
                return NodeStep.Continue(outcome);
            }

            case DecisionNodeDefinition decision:
            {
                string outcome = decision.Handler is not null
                    ? await decision.Handler(context, cancellationToken).ConfigureAwait(false)
                    : await _decisions.DispatchAsync(decision.QueryFactory!(context), context, cancellationToken).ConfigureAwait(false);

                if (outcome is null)
                {
                    throw new ProcessEngineException(
                        $"Le noeud decision '{node.Id}' du processus '{definition.Key}' n'a produit aucun resultat.");
                }

                return NodeStep.Continue(outcome);
            }

            case WaitNodeDefinition wait:
                return await EnterWaitAsync(wait, instance, context, cancellationToken).ConfigureAwait(false);

            case InteractiveNodeDefinition interactive:
                return await EnterInteractionAsync(interactive, instance, context, cancellationToken).ConfigureAwait(false);

            case SubProcessNodeDefinition subProcess:
                return await EnterSubProcessAsync(subProcess, instance, context, run, cancellationToken).ConfigureAwait(false);

            case EndNodeDefinition end:
                return NodeStep.Finish(end.Outcome, end.IsFailure);

            default:
                throw new ProcessEngineException($"Type de noeud non supporte : {node.GetType().Name}.");
        }
    }

    private async Task<NodeStep> EnterWaitAsync(
        WaitNodeDefinition node,
        ProcessInstance instance,
        INodeExecutionContext context,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;
        DateTimeOffset? dueAt = node.DueAt?.Invoke(context);

        if (dueAt.HasValue && dueAt.Value <= now)
        {
            return NodeStep.Continue(NodeOutcomes.Timer);
        }

        if (!dueAt.HasValue && node.SignalName is null)
        {
            throw new ProcessEngineException(
                $"Le noeud d'attente '{node.Id}' n'a produit ni echeance ni signal : l'instance resterait bloquee indefiniment.");
        }

        if (node.SignalName is not null)
        {
            var signalWait = new ProcessWait(Guid.NewGuid(), instance.Id, node.Id, WaitKind.Signal, now)
            {
                SignalName = node.SignalName,
                CorrelationKey = node.CorrelationKey?.Invoke(context) ?? instance.CorrelationId,
            };
            await _store.InsertWaitAsync(signalWait, cancellationToken).ConfigureAwait(false);
        }

        if (dueAt.HasValue)
        {
            await ScheduleTimerAsync(instance, node, dueAt.Value, now, cancellationToken).ConfigureAwait(false);
        }

        return NodeStep.Block(node.SignalName is not null ? WaitKind.Signal : WaitKind.Timer);
    }

    private async Task<NodeStep> EnterInteractionAsync(
        InteractiveNodeDefinition node,
        ProcessInstance instance,
        INodeExecutionContext context,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;
        DateTimeOffset? dueAt = node.DueAt?.Invoke(context);

        if (dueAt.HasValue && dueAt.Value <= now)
        {
            return NodeStep.Continue(NodeOutcomes.Timer);
        }

        var wait = new ProcessWait(Guid.NewGuid(), instance.Id, node.Id, WaitKind.Interaction, now)
        {
            InteractionKey = node.InteractionKey,
            Assignee = node.Assignee?.Invoke(context),
            CorrelationKey = instance.CorrelationId,
        };
        await _store.InsertWaitAsync(wait, cancellationToken).ConfigureAwait(false);

        if (dueAt.HasValue)
        {
            await ScheduleTimerAsync(instance, node, dueAt.Value, now, cancellationToken).ConfigureAwait(false);
        }

        return NodeStep.Block(WaitKind.Interaction);
    }

    private async Task<NodeStep> EnterSubProcessAsync(
        SubProcessNodeDefinition node,
        ProcessInstance instance,
        INodeExecutionContext context,
        RunState run,
        CancellationToken cancellationToken)
    {
        if (instance.Depth + 1 > _options.MaxSubProcessDepth)
        {
            throw new ProcessEngineException(
                $"Le noeud '{node.Id}' depasse la profondeur maximale de sous-processus ({_options.MaxSubProcessDepth}).");
        }

        DateTimeOffset now = _clock.UtcNow;
        ProcessDefinition childDefinition = _definitions.Get(node.ChildDefinitionKey, node.ChildVersion);

        var childVariables = new ProcessVariables();
        node.InputMapper?.Invoke(context, childVariables);

        ProcessInstance child = ProcessInstance.Create(
            Guid.NewGuid(),
            childDefinition.Key,
            childDefinition.Version,
            childDefinition.StartNodeId,
            now,
            childVariables);
        child.Depth = instance.Depth + 1;
        child.CorrelationId = instance.CorrelationId;

        if (node.WaitForCompletion)
        {
            child.ParentInstanceId = instance.Id;
            child.ParentNodeId = node.Id;
        }

        await _store.InsertInstanceAsync(child, cancellationToken).ConfigureAwait(false);
        run.Instances[child.Id] = child;
        await AppendHistoryAsync(child, ProcessEventType.Started, null, null, childDefinition.ToString(), cancellationToken).ConfigureAwait(false);
        run.Ready.Enqueue(new PendingRun(child, null));

        if (!node.WaitForCompletion)
        {
            return NodeStep.Continue(null);
        }

        var wait = new ProcessWait(Guid.NewGuid(), instance.Id, node.Id, WaitKind.SubProcess, now)
        {
            ChildInstanceId = child.Id,
            CorrelationKey = instance.CorrelationId,
        };
        await _store.InsertWaitAsync(wait, cancellationToken).ConfigureAwait(false);

        return NodeStep.Block(WaitKind.SubProcess);
    }

    private async Task NotifyParentAsync(ProcessInstance child, RunState run, CancellationToken cancellationToken)
    {
        if (child.ParentInstanceId is null)
        {
            return;
        }

        ProcessInstance parent = await LoadAsync(run, child.ParentInstanceId.Value, cancellationToken).ConfigureAwait(false);
        if (parent.Status != ProcessStatus.Waiting)
        {
            return;
        }

        IReadOnlyList<ProcessWait> waits = await _store.GetWaitsAsync(parent.Id, cancellationToken).ConfigureAwait(false);
        ProcessWait? wait = waits.FirstOrDefault(w => w.Kind == WaitKind.SubProcess && w.ChildInstanceId == child.Id);
        if (wait is null)
        {
            return;
        }

        ProcessDefinition parentDefinition = _definitions.Get(parent.DefinitionKey, parent.DefinitionVersion);
        NodeDefinition node = parentDefinition.GetNode(wait.NodeId);

        await ReleaseWaitsAsync(parent, cancellationToken).ConfigureAwait(false);

        var context = new NodeExecutionContext(
            parentDefinition,
            node,
            parent,
            run.Session,
            _services,
            _clock.UtcNow,
            null,
            cancellationToken);

        if (node is SubProcessNodeDefinition subProcess && subProcess.OutputMapper is not null && child.Status == ProcessStatus.Completed)
        {
            subProcess.OutputMapper(context, child.Variables);
        }

        string outcome = child.Status switch
        {
            ProcessStatus.Completed => child.Outcome ?? NodeOutcomes.Completed,
            ProcessStatus.Faulted => NodeOutcomes.Faulted,
            _ => NodeOutcomes.Cancelled,
        };

        string? target = ResolveTarget(node, outcome, context);
        if (target is null)
        {
            if (child.Status == ProcessStatus.Completed)
            {
                throw new ProcessTransitionException(parentDefinition.Key, node.Id, outcome);
            }

            // L'enfant s'est mal termine et le parent ne prevoit pas ce cas : le parent echoue a son tour.
            await FaultAsync(
                parent,
                node,
                new ProcessEngineException(
                    $"Le sous-processus '{child.Id}' s'est termine en '{outcome}' et le noeud '{node.Id}' ne prevoit pas ce resultat."),
                cancellationToken).ConfigureAwait(false);
            parent.UpdatedAt = _clock.UtcNow;
            await _store.UpdateInstanceAsync(parent, cancellationToken).ConfigureAwait(false);
            await NotifyParentAsync(parent, run, cancellationToken).ConfigureAwait(false);
            return;
        }

        parent.CurrentNodeId = target;
        parent.Status = ProcessStatus.Running;
        run.Blocked.Remove(parent.Id);
        await AppendHistoryAsync(
            parent,
            ProcessEventType.Resumed,
            node,
            outcome,
            $"sous-processus {child.Id}",
            cancellationToken).ConfigureAwait(false);

        run.Ready.Enqueue(new PendingRun(parent, null));
    }

    private async Task FinishAsync(ProcessInstance instance, NodeDefinition node, NodeStep step, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;
        instance.Outcome = step.Outcome;
        instance.CompletedAt = now;

        if (step.IsFailure)
        {
            instance.Status = ProcessStatus.Faulted;
            instance.FaultMessage = $"Le processus s'est termine sur le noeud d'echec '{node.Id}'.";
            await AppendHistoryAsync(instance, ProcessEventType.Faulted, node, step.Outcome, instance.FaultMessage, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            instance.Status = ProcessStatus.Completed;
            await AppendHistoryAsync(instance, ProcessEventType.Completed, node, step.Outcome, null, cancellationToken).ConfigureAwait(false);
        }

        await _store.DeleteWaitsAsync(instance.Id, cancellationToken).ConfigureAwait(false);
        await _scheduler.CancelAsync(instance.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task FaultAsync(ProcessInstance instance, NodeDefinition node, Exception error, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;
        instance.Status = ProcessStatus.Faulted;
        instance.FaultMessage = error.Message;
        instance.FaultDetail = error.ToString();
        instance.CompletedAt = now;

        _logger.LogError(
            error,
            "Le noeud {NodeId} de l'instance {InstanceId} a echoue.",
            node.Id,
            instance.Id);

        await AppendHistoryAsync(instance, ProcessEventType.Faulted, node, null, error.Message, cancellationToken).ConfigureAwait(false);
        await _store.DeleteWaitsAsync(instance.Id, cancellationToken).ConfigureAwait(false);
        await _scheduler.CancelAsync(instance.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeferAsync(ProcessInstance instance, RunState run, CancellationToken cancellationToken)
    {
        var job = new ProcessJob(instance.Id, instance.DefinitionKey, instance.CurrentNodeId, ProcessJobKind.Continuation)
        {
            Reason = $"Budget de {_options.MaxStepsPerTransaction} pas atteint dans la transaction courante.",
        };

        await _scheduler.ScheduleAsync(job, cancellationToken).ConfigureAwait(false);
        await AppendHistoryAsync(instance, ProcessEventType.Deferred, null, null, job.Reason, cancellationToken).ConfigureAwait(false);
        run.Deferred.Add(instance.Id);

        _logger.LogInformation(
            "L'instance {InstanceId} a atteint le budget de pas ; la suite est confiee a l'ordonnanceur externe.",
            instance.Id);
    }

    private async Task ScheduleTimerAsync(
        ProcessInstance instance,
        NodeDefinition node,
        DateTimeOffset dueAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var timerWait = new ProcessWait(Guid.NewGuid(), instance.Id, node.Id, WaitKind.Timer, now)
        {
            DueAt = dueAt,
            CorrelationKey = instance.CorrelationId,
        };
        await _store.InsertWaitAsync(timerWait, cancellationToken).ConfigureAwait(false);

        var job = new ProcessJob(instance.Id, instance.DefinitionKey, node.Id, ProcessJobKind.Timer)
        {
            RunAt = dueAt,
            WaitId = timerWait.Id,
            Reason = $"Echeance du noeud '{node.Id}'.",
        };
        await _scheduler.ScheduleAsync(job, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReleaseWaitsAsync(ProcessInstance instance, CancellationToken cancellationToken)
    {
        await _store.DeleteWaitsAsync(instance.Id, cancellationToken).ConfigureAwait(false);
        await _scheduler.CancelAsync(instance.Id, cancellationToken).ConfigureAwait(false);
    }

    private static string? ResolveTarget(NodeDefinition node, string? outcome, INodeExecutionContext context)
    {
        if (outcome is not null)
        {
            foreach (Transition transition in node.Transitions)
            {
                if (string.Equals(transition.Outcome, outcome, StringComparison.OrdinalIgnoreCase)
                    && (transition.Condition is null || transition.Condition(context)))
                {
                    return transition.Target;
                }
            }
        }

        foreach (Transition transition in node.Transitions)
        {
            if (transition.IsDefault && (transition.Condition is null || transition.Condition(context)))
            {
                return transition.Target;
            }
        }

        return null;
    }

    private bool ShouldCapture(Exception exception)
        => !_options.PropagateHandlerExceptions && exception is not OperationCanceledException;

    private Task AppendHistoryAsync(
        ProcessInstance instance,
        ProcessEventType eventType,
        NodeDefinition? node,
        string? outcome,
        string? detail,
        CancellationToken cancellationToken)
    {
        if (!_options.RecordHistory)
        {
            return Task.CompletedTask;
        }

        var entry = new ProcessHistoryEntry(Guid.NewGuid(), instance.Id, eventType, _clock.UtcNow)
        {
            NodeId = node?.Id,
            NodeKind = node?.Kind,
            Outcome = outcome,
            Detail = detail,
        };

        return _store.AppendHistoryAsync(entry, cancellationToken);
    }

    private async Task<ProcessInstance> LoadAsync(RunState run, Guid instanceId, CancellationToken cancellationToken)
    {
        if (run.Instances.TryGetValue(instanceId, out ProcessInstance? cached))
        {
            return cached;
        }

        ProcessInstance instance = await _store
            .GetInstanceAsync(instanceId, _options.LockInstancesForUpdate, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ProcessInstanceNotFoundException(instanceId);

        run.Instances[instanceId] = instance;
        return instance;
    }

    private static ProcessInstance Track(RunState run, ProcessInstance instance)
    {
        if (run.Instances.TryGetValue(instance.Id, out ProcessInstance? cached))
        {
            return cached;
        }

        run.Instances[instance.Id] = instance;
        return instance;
    }

    private RunState NewRun() => new RunState(RequireSession(), _options.MaxStepsPerTransaction);

    private IDbSession RequireSession() => _sessions.Current ?? throw new ProcessSessionMissingException();

    private static ProcessExecutionResult BuildResult(ProcessInstance instance, RunState run)
        => new ProcessExecutionResult(
            instance.Id,
            instance.Status,
            instance.CurrentNodeId,
            run.StepsExecuted,
            run.Blocked.TryGetValue(instance.Id, out WaitKind kind) && instance.Status == ProcessStatus.Waiting
                ? kind
                : (WaitKind?)null,
            instance.Outcome,
            instance.FaultMessage,
            run.Deferred.Contains(instance.Id));

    private enum StepAction
    {
        Continue,
        MoveTo,
        Block,
        Finish,
        Fault,
    }

    private sealed class NodeStep
    {
        private NodeStep(StepAction action) => Action = action;

        public StepAction Action { get; }

        public string? Outcome { get; private set; }

        public string? Target { get; private set; }

        public WaitKind? WaitKind { get; private set; }

        public Exception? Error { get; private set; }

        public bool IsFailure { get; private set; }

        public static NodeStep Continue(string? outcome) => new NodeStep(StepAction.Continue) { Outcome = outcome };

        public static NodeStep MoveTo(string target, string? outcome)
            => new NodeStep(StepAction.MoveTo) { Target = target, Outcome = outcome };

        public static NodeStep Block(WaitKind kind) => new NodeStep(StepAction.Block) { WaitKind = kind };

        public static NodeStep Finish(string? outcome, bool isFailure)
            => new NodeStep(StepAction.Finish) { Outcome = outcome, IsFailure = isFailure };

        public static NodeStep Fault(Exception error) => new NodeStep(StepAction.Fault) { Error = error };
    }

    private readonly struct PendingRun
    {
        public PendingRun(ProcessInstance instance, object? input)
        {
            Instance = instance;
            Input = input;
        }

        public ProcessInstance Instance { get; }

        public object? Input { get; }
    }

    private sealed class RunState
    {
        public RunState(IDbSession session, int stepBudget)
        {
            Session = session;
            StepsRemaining = stepBudget;
        }

        public IDbSession Session { get; }

        public int StepsRemaining { get; set; }

        public int StepsExecuted { get; set; }

        public Dictionary<Guid, ProcessInstance> Instances { get; } = new Dictionary<Guid, ProcessInstance>();

        public Queue<PendingRun> Ready { get; } = new Queue<PendingRun>();

        public Dictionary<Guid, WaitKind> Blocked { get; } = new Dictionary<Guid, WaitKind>();

        public HashSet<Guid> Deferred { get; } = new HashSet<Guid>();
    }
}
