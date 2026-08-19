using System;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Execution;

/// <summary>Resultat d'un pas d'avancement, du point de vue de l'instance visee.</summary>
public sealed class ProcessExecutionResult
{
    internal ProcessExecutionResult(
        Guid instanceId,
        ProcessStatus status,
        string currentNodeId,
        int stepsExecuted,
        WaitKind? waitKind,
        string? outcome,
        string? faultMessage,
        bool deferred)
    {
        InstanceId = instanceId;
        Status = status;
        CurrentNodeId = currentNodeId;
        StepsExecuted = stepsExecuted;
        WaitKind = waitKind;
        Outcome = outcome;
        FaultMessage = faultMessage;
        Deferred = deferred;
    }

    /// <summary>Instance concernee.</summary>
    public Guid InstanceId { get; }

    /// <summary>Etat de l'instance a la fin du pas.</summary>
    public ProcessStatus Status { get; }

    /// <summary>Noeud sur lequel l'instance s'est arretee.</summary>
    public string CurrentNodeId { get; }

    /// <summary>Nombre total de noeuds franchis pendant le pas, sous-processus compris.</summary>
    public int StepsExecuted { get; }

    /// <summary>Nature de l'attente qui bloque l'instance, le cas echeant.</summary>
    public WaitKind? WaitKind { get; }

    /// <summary>Resultat final si l'instance est terminee.</summary>
    public string? Outcome { get; }

    /// <summary>Message d'erreur si l'instance est en echec.</summary>
    public string? FaultMessage { get; }

    /// <summary>
    /// Indique que l'avancement a ete interrompu par le budget de pas et confie a une tache
    /// externe : l'instance reste executable.
    /// </summary>
    public bool Deferred { get; }

    /// <summary>Indique si l'instance est terminee, quelle qu'en soit l'issue.</summary>
    public bool IsFinished
        => Status == ProcessStatus.Completed || Status == ProcessStatus.Faulted || Status == ProcessStatus.Cancelled;
}
