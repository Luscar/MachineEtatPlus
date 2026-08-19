using System;
using System.Threading;
using System.Threading.Tasks;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Gestion de taches externe : le moteur se contente de declarer qu'une instance devra etre
/// reveillee, l'implementation cliente choisit le mecanisme (table d'outbox, Hangfire, Quartz,
/// file de messages, agent SQL...).
/// </summary>
/// <remarks>
/// Ces appels sont effectues a l'interieur de la transaction du client. Une implementation qui
/// ecrit dans une table de la meme base obtient donc gratuitement la semantique
/// <em>outbox</em> : aucune tache n'est publiee si la transaction est annulee.
/// </remarks>
public interface IProcessJobScheduler
{
    /// <summary>Programme le reveil d'une instance.</summary>
    /// <param name="job">Tache a programmer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque la demande est enregistree.</returns>
    Task ScheduleAsync(ProcessJob job, CancellationToken cancellationToken);

    /// <summary>Annule les reveils encore programmes pour une instance.</summary>
    /// <param name="instanceId">Instance concernee.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tache achevee lorsque les reveils sont annules.</returns>
    Task CancelAsync(Guid instanceId, CancellationToken cancellationToken);
}
