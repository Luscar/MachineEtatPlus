using System;

namespace MachineEtatPlus.Model;

/// <summary>
/// Attente enregistree pour une instance bloquee. Une instance peut porter plusieurs attentes
/// concurrentes, par exemple un signal double d'une echeance de securite.
/// </summary>
public sealed class ProcessWait
{
    /// <summary>Cree une attente.</summary>
    /// <param name="id">Identifiant de l'attente.</param>
    /// <param name="instanceId">Instance bloquee.</param>
    /// <param name="nodeId">Noeud sur lequel l'instance est bloquee.</param>
    /// <param name="kind">Nature de l'attente.</param>
    /// <param name="createdAt">Horodatage de creation.</param>
    public ProcessWait(Guid id, Guid instanceId, string nodeId, WaitKind kind, DateTimeOffset createdAt)
    {
        Id = id;
        InstanceId = instanceId;
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        Kind = kind;
        CreatedAt = createdAt;
    }

    /// <summary>Identifiant de l'attente.</summary>
    public Guid Id { get; }

    /// <summary>Instance bloquee.</summary>
    public Guid InstanceId { get; }

    /// <summary>Noeud sur lequel l'instance est bloquee.</summary>
    public string NodeId { get; }

    /// <summary>Nature de l'attente.</summary>
    public WaitKind Kind { get; }

    /// <summary>Horodatage de creation.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Echeance a laquelle l'attente doit se declencher (attentes <see cref="WaitKind.Timer"/>).</summary>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>Nom du signal attendu (attentes <see cref="WaitKind.Signal"/>).</summary>
    public string? SignalName { get; set; }

    /// <summary>Cle de correlation permettant de router un signal vers la bonne instance.</summary>
    public string? CorrelationKey { get; set; }

    /// <summary>Cle fonctionnelle de l'interaction attendue (attentes <see cref="WaitKind.Interaction"/>).</summary>
    public string? InteractionKey { get; set; }

    /// <summary>Destinataire de l'interaction attendue (utilisateur, role, file...).</summary>
    public string? Assignee { get; set; }

    /// <summary>Instance enfant attendue (attentes <see cref="WaitKind.SubProcess"/>).</summary>
    public Guid? ChildInstanceId { get; set; }
}
