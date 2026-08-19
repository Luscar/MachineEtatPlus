using System;
using MachineEtatPlus.Definition;

namespace MachineEtatPlus.Model;

/// <summary>Evenement journalise par le moteur lors de l'avancement d'une instance.</summary>
public sealed class ProcessHistoryEntry
{
    /// <summary>Cree une entree d'historique.</summary>
    /// <param name="id">Identifiant de l'entree.</param>
    /// <param name="instanceId">Instance concernee.</param>
    /// <param name="eventType">Type d'evenement.</param>
    /// <param name="occurredAt">Horodatage.</param>
    public ProcessHistoryEntry(Guid id, Guid instanceId, ProcessEventType eventType, DateTimeOffset occurredAt)
    {
        Id = id;
        InstanceId = instanceId;
        EventType = eventType;
        OccurredAt = occurredAt;
    }

    /// <summary>Identifiant de l'entree.</summary>
    public Guid Id { get; }

    /// <summary>Instance concernee.</summary>
    public Guid InstanceId { get; }

    /// <summary>Type d'evenement.</summary>
    public ProcessEventType EventType { get; }

    /// <summary>Horodatage.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Noeud concerne, si l'evenement en vise un.</summary>
    public string? NodeId { get; set; }

    /// <summary>Type du noeud concerne.</summary>
    public NodeKind? NodeKind { get; set; }

    /// <summary>Resultat produit par le noeud, s'il y en a un.</summary>
    public string? Outcome { get; set; }

    /// <summary>Complement libre (message d'erreur, nom du signal recu...).</summary>
    public string? Detail { get; set; }
}
