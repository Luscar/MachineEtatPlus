using System;

namespace MachineEtatPlus.Abstractions;

/// <summary>Nature d'une tache demandee a l'ordonnanceur externe.</summary>
public enum ProcessJobKind
{
    /// <summary>Reveil a echeance : l'instance attend une date/heure.</summary>
    Timer = 0,

    /// <summary>
    /// Poursuite immediate : le moteur a atteint son budget de pas pour la transaction courante
    /// et confie la suite a une nouvelle transaction.
    /// </summary>
    Continuation = 1,
}

/// <summary>
/// Demande de reveil adressee a la gestion de taches de l'application cliente.
/// </summary>
public sealed class ProcessJob
{
    /// <summary>Cree une demande de reveil.</summary>
    /// <param name="instanceId">Instance a reveiller.</param>
    /// <param name="definitionKey">Cle de la definition executee.</param>
    /// <param name="nodeId">Noeud concerne.</param>
    /// <param name="kind">Nature de la tache.</param>
    public ProcessJob(Guid instanceId, string definitionKey, string nodeId, ProcessJobKind kind)
    {
        InstanceId = instanceId;
        DefinitionKey = definitionKey ?? throw new ArgumentNullException(nameof(definitionKey));
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        Kind = kind;
    }

    /// <summary>Instance a reveiller.</summary>
    public Guid InstanceId { get; }

    /// <summary>Cle de la definition executee.</summary>
    public string DefinitionKey { get; }

    /// <summary>Noeud concerne.</summary>
    public string NodeId { get; }

    /// <summary>Nature de la tache.</summary>
    public ProcessJobKind Kind { get; }

    /// <summary>Instant du reveil ; <c>null</c> signifie des que possible.</summary>
    public DateTimeOffset? RunAt { get; set; }

    /// <summary>Attente a l'origine de la tache, lorsqu'il y en a une.</summary>
    public Guid? WaitId { get; set; }

    /// <summary>Motif lisible, utile pour la supervision.</summary>
    public string? Reason { get; set; }
}
