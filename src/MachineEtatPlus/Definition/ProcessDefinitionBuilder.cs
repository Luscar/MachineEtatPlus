using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Execution;

namespace MachineEtatPlus.Definition;

/// <summary>Constructeur fluide d'une definition de processus.</summary>
/// <example>
/// <code>
/// ProcessDefinition definition = ProcessDefinition.Create("commande", 1)
///     .Start("debut", to: "verifier")
///     .Decision("verifier", d => d
///         .Query(ctx => new ClientSolvableQuery(ctx.GetVariable&lt;int&gt;("clientId")))
///         .When("oui", "reserver")
///         .When("non", "refus"))
///     .Business("reserver", b => b
///         .Command(ctx => new ReserverStockCommand(ctx.GetVariable&lt;int&gt;("commandeId")))
///         .To("attendrePaiement"))
///     .Wait("attendrePaiement", w => w
///         .ForSignal("paiement-recu")
///         .After(TimeSpan.FromDays(2))
///         .OnSignal("expedier")
///         .OnTimer("refus"))
///     .SubProcess("expedier", "expedition", s => s
///         .Input((ctx, vars) =&gt; vars.Set("commandeId", ctx.GetVariable&lt;int&gt;("commandeId")))
///         .To("fin"))
///     .End("fin", outcome: "livree")
///     .End("refus", outcome: "refusee")
///     .Build();
/// </code>
/// </example>
public sealed class ProcessDefinitionBuilder
{
    private readonly string _key;
    private readonly int _version;
    private readonly List<NodeDefinition> _nodes = new List<NodeDefinition>();
    private string? _name;
    private string? _startNodeId;

    internal ProcessDefinitionBuilder(string key, int version)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La cle du processus est obligatoire.", nameof(key));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "La version doit etre strictement positive.");
        }

        _key = key;
        _version = version;
    }

    /// <summary>Donne un libelle lisible au processus.</summary>
    /// <param name="name">Libelle.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Declare le noeud de depart.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="to">Premier noeud a executer.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Start(string id, string to)
    {
        if (_startNodeId is not null)
        {
            throw new ProcessDefinitionException($"Le processus '{_key}' declare deja un noeud de depart ('{_startNodeId}').");
        }

        _startNodeId = id;
        return Add(new StartNodeDefinition(id, null, new[] { new Transition(to) }));
    }

    /// <summary>Declare un noeud metier configure finement.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="configure">Configuration du noeud.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Business(string id, Action<BusinessNodeBuilder> configure)
        => AddConfigured(id, new BusinessNodeBuilder(), configure);

    /// <summary>Declare un noeud metier qui dispatche une commande puis poursuit vers <paramref name="to"/>.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="command">Fabrique de la commande.</param>
    /// <param name="to">Noeud suivant.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Business(string id, Func<INodeExecutionContext, object> command, string to)
        => Business(id, b => b.Command(command).To(to));

    /// <summary>Declare un noeud metier execute en ligne puis poursuit vers <paramref name="to"/>.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="handler">Traitement asynchrone.</param>
    /// <param name="to">Noeud suivant.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Business(string id, Func<INodeExecutionContext, CancellationToken, Task> handler, string to)
        => Business(id, b => b.Execute(handler).To(to));

    /// <summary>Declare un noeud de decision.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="configure">Configuration du noeud.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Decision(string id, Action<DecisionNodeBuilder> configure)
        => AddConfigured(id, new DecisionNodeBuilder(), configure);

    /// <summary>Declare un noeud d'attente.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="configure">Configuration du noeud.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Wait(string id, Action<WaitNodeBuilder> configure)
        => AddConfigured(id, new WaitNodeBuilder(), configure);

    /// <summary>Declare un noeud interactif.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="interactionKey">Cle fonctionnelle de l'interaction attendue.</param>
    /// <param name="configure">Configuration du noeud.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Interactive(string id, string interactionKey, Action<InteractiveNodeBuilder> configure)
        => AddConfigured(id, new InteractiveNodeBuilder(interactionKey), configure);

    /// <summary>Declare un noeud sous-processus.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="childDefinitionKey">Cle de la definition enfant.</param>
    /// <param name="configure">Configuration du noeud.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder SubProcess(string id, string childDefinitionKey, Action<SubProcessNodeBuilder> configure)
        => AddConfigured(id, new SubProcessNodeBuilder(childDefinitionKey), configure);

    /// <summary>Declare un noeud de fin en succes.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="outcome">Resultat final publie par l'instance.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder End(string id, string? outcome = null, string? name = null)
        => Add(new EndNodeDefinition(id, name, outcome, isFailure: false));

    /// <summary>Declare un noeud de fin en echec.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="outcome">Resultat final publie par l'instance.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Fail(string id, string? outcome = null, string? name = null)
        => Add(new EndNodeDefinition(id, name, outcome, isFailure: true));

    /// <summary>Ajoute un noeud construit a la main.</summary>
    /// <param name="node">Noeud a ajouter.</param>
    /// <returns>Ce constructeur.</returns>
    public ProcessDefinitionBuilder Add(NodeDefinition node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_nodes.Any(n => string.Equals(n.Id, node.Id, StringComparison.Ordinal)))
        {
            throw new ProcessDefinitionException($"Le processus '{_key}' declare deux fois le noeud '{node.Id}'.");
        }

        _nodes.Add(node);
        return this;
    }

    /// <summary>Valide puis fige la definition.</summary>
    /// <returns>La definition construite.</returns>
    /// <exception cref="ProcessDefinitionException">La definition est incoherente.</exception>
    public ProcessDefinition Build()
    {
        if (_startNodeId is null)
        {
            throw new ProcessDefinitionException($"Le processus '{_key}' ne declare aucun noeud de depart.");
        }

        var errors = new List<string>();
        var ids = new HashSet<string>(_nodes.Select(n => n.Id), StringComparer.Ordinal);

        foreach (NodeDefinition node in _nodes)
        {
            if (node.Kind != NodeKind.End && node.Transitions.Count == 0)
            {
                errors.Add($"Le noeud '{node.Id}' n'a aucune transition sortante.");
            }

            foreach (Transition transition in node.Transitions)
            {
                if (!ids.Contains(transition.Target))
                {
                    errors.Add($"Le noeud '{node.Id}' cible un noeud inexistant '{transition.Target}'.");
                }
            }

            IEnumerable<IGrouping<string, Transition>> duplicates = node.Transitions
                .Where(t => t.Outcome is not null && t.Condition is null)
                .GroupBy(t => t.Outcome!, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (IGrouping<string, Transition> duplicate in duplicates)
            {
                errors.Add($"Le noeud '{node.Id}' declare plusieurs transitions inconditionnelles pour le resultat '{duplicate.Key}'.");
            }

            if (node.Transitions.Count(t => t.IsDefault && t.Condition is null) > 1)
            {
                errors.Add($"Le noeud '{node.Id}' declare plusieurs transitions par defaut inconditionnelles.");
            }

            if (node is WaitNodeDefinition wait)
            {
                if (wait.SignalName is not null && !HasOutcome(node, NodeOutcomes.Signal) && !HasDefault(node))
                {
                    errors.Add($"Le noeud d'attente '{node.Id}' attend un signal sans transition '{NodeOutcomes.Signal}' ni transition par defaut.");
                }

                if (wait.DueAt is not null && !HasOutcome(node, NodeOutcomes.Timer) && !HasDefault(node))
                {
                    errors.Add($"Le noeud d'attente '{node.Id}' declare une echeance sans transition '{NodeOutcomes.Timer}' ni transition par defaut.");
                }
            }

            if (node is InteractiveNodeDefinition interactive
                && interactive.DueAt is not null
                && !HasOutcome(node, NodeOutcomes.Timer)
                && !HasDefault(node))
            {
                errors.Add($"Le noeud interactif '{node.Id}' declare une echeance sans transition '{NodeOutcomes.Timer}' ni transition par defaut.");
            }
        }

        if (!ids.Contains(_startNodeId))
        {
            errors.Add($"Le noeud de depart '{_startNodeId}' est absent.");
        }

        if (!_nodes.Any(n => n.Kind == NodeKind.End))
        {
            errors.Add("Le processus ne declare aucun noeud de fin.");
        }

        foreach (string unreachable in FindUnreachable())
        {
            errors.Add($"Le noeud '{unreachable}' n'est atteignable depuis aucun autre noeud.");
        }

        if (errors.Count > 0)
        {
            throw new ProcessDefinitionException(
                $"Le processus '{_key}' v{_version} est invalide :{Environment.NewLine} - " +
                string.Join($"{Environment.NewLine} - ", errors));
        }

        return new ProcessDefinition(_key, _version, _name, _startNodeId, _nodes.ToArray());
    }

    private static bool HasOutcome(NodeDefinition node, string outcome)
        => node.Transitions.Any(t => string.Equals(t.Outcome, outcome, StringComparison.OrdinalIgnoreCase));

    private static bool HasDefault(NodeDefinition node) => node.Transitions.Any(t => t.IsDefault);

    private IEnumerable<string> FindUnreachable()
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal) { _startNodeId! };
        var queue = new Queue<string>();
        queue.Enqueue(_startNodeId!);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            NodeDefinition? node = _nodes.FirstOrDefault(n => string.Equals(n.Id, current, StringComparison.Ordinal));
            if (node is null)
            {
                continue;
            }

            foreach (Transition transition in node.Transitions)
            {
                if (reachable.Add(transition.Target))
                {
                    queue.Enqueue(transition.Target);
                }
            }
        }

        return _nodes.Select(n => n.Id).Where(id => !reachable.Contains(id));
    }

    private ProcessDefinitionBuilder AddConfigured<TBuilder>(string id, TBuilder builder, Action<TBuilder> configure)
        where TBuilder : NodeBuilder<TBuilder>
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(builder);
        return Add(builder.BuildNode(id));
    }
}
