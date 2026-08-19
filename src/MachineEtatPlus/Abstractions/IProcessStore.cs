using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Persistance des instances, des attentes et de l'historique. Le schema des tables est libre :
/// il est defini et implemente par l'application cliente.
/// </summary>
/// <remarks>
/// Toutes les operations doivent s'executer sur la connexion et la transaction exposees par
/// <see cref="IDbSessionAccessor"/> : le moteur appelle le stockage a l'interieur de la transaction
/// du client et ne valide jamais lui-meme.
/// </remarks>
public interface IProcessStore
{
    /// <summary>Charge une instance.</summary>
    /// <param name="instanceId">Identifiant de l'instance.</param>
    /// <param name="forUpdate">
    /// <c>true</c> si le moteur s'apprete a la modifier : l'implementation devrait alors poser
    /// un verrou (par exemple <c>SELECT ... FOR UPDATE</c> ou <c>UPDLOCK</c>).
    /// </param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'instance, ou <c>null</c> si elle n'existe pas.</returns>
    Task<ProcessInstance?> GetInstanceAsync(Guid instanceId, bool forUpdate, CancellationToken cancellationToken);

    /// <summary>Insere une nouvelle instance.</summary>
    /// <param name="instance">Instance a inserer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque l'insertion est faite.</returns>
    Task InsertInstanceAsync(ProcessInstance instance, CancellationToken cancellationToken);

    /// <summary>Met a jour une instance existante.</summary>
    /// <param name="instance">Instance a mettre a jour.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque la mise a jour est faite.</returns>
    Task UpdateInstanceAsync(ProcessInstance instance, CancellationToken cancellationToken);

    /// <summary>Retourne les instances enfants d'une instance.</summary>
    /// <param name="parentInstanceId">Instance parente.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Les instances enfants, eventuellement vides.</returns>
    Task<IReadOnlyList<ProcessInstance>> GetChildInstancesAsync(Guid parentInstanceId, CancellationToken cancellationToken);

    /// <summary>Enregistre une attente.</summary>
    /// <param name="wait">Attente a enregistrer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque l'attente est enregistree.</returns>
    Task InsertWaitAsync(ProcessWait wait, CancellationToken cancellationToken);

    /// <summary>Retourne les attentes en cours d'une instance.</summary>
    /// <param name="instanceId">Instance concernee.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Les attentes en cours.</returns>
    Task<IReadOnlyList<ProcessWait>> GetWaitsAsync(Guid instanceId, CancellationToken cancellationToken);

    /// <summary>Supprime toutes les attentes d'une instance.</summary>
    /// <param name="instanceId">Instance concernee.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque les attentes sont supprimees.</returns>
    Task DeleteWaitsAsync(Guid instanceId, CancellationToken cancellationToken);

    /// <summary>Recherche les attentes correspondant a un signal.</summary>
    /// <param name="signalName">Nom du signal.</param>
    /// <param name="correlationKey">
    /// Cle de correlation ; lorsqu'elle est fournie, seules les attentes portant la meme cle
    /// doivent etre retournees.
    /// </param>
    /// <param name="maxCount">Nombre maximum d'attentes a retourner.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Les attentes correspondantes.</returns>
    Task<IReadOnlyList<ProcessWait>> FindSignalWaitsAsync(
        string signalName,
        string? correlationKey,
        int maxCount,
        CancellationToken cancellationToken);

    /// <summary>Recherche les attentes temporelles echues.</summary>
    /// <param name="now">Instant de reference.</param>
    /// <param name="maxCount">Nombre maximum d'attentes a retourner.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Les attentes dont l'echeance est atteinte.</returns>
    Task<IReadOnlyList<ProcessWait>> FindDueTimerWaitsAsync(
        DateTimeOffset now,
        int maxCount,
        CancellationToken cancellationToken);

    /// <summary>Ajoute une entree a l'historique.</summary>
    /// <param name="entry">Entree a ajouter.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque l'entree est ecrite.</returns>
    Task AppendHistoryAsync(ProcessHistoryEntry entry, CancellationToken cancellationToken);
}
