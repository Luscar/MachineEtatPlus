using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MachineEtatPlus.Execution;

/// <summary>
/// Moteur d'avancement des processus. Chaque appel s'execute dans la transaction fournie par
/// l'application cliente et fait progresser l'instance aussi loin que possible : le moteur ne
/// s'arrete que sur un noeud bloquant, une fin de processus, une erreur, ou l'epuisement de son
/// budget de pas.
/// </summary>
public interface IProcessEngine
{
    /// <summary>Demarre une instance et la fait avancer immediatement.</summary>
    /// <param name="request">Demande de demarrage.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de l'instance a la fin du pas.</returns>
    Task<ProcessExecutionResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprend une instance laissee executable, typiquement depuis la tache de poursuite
    /// programmee lorsque le budget de pas a ete atteint.
    /// </summary>
    /// <param name="instanceId">Instance a reprendre.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de l'instance a la fin du pas.</returns>
    Task<ProcessExecutionResult> ResumeAsync(Guid instanceId, CancellationToken cancellationToken = default);

    /// <summary>Emet un signal vers les instances qui l'attendent.</summary>
    /// <param name="request">Demande d'emission.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de chaque instance reveillee ; la liste est vide si aucune n'attendait.</returns>
    Task<IReadOnlyList<ProcessExecutionResult>> SignalAsync(SignalRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cloture une interaction humaine et relance l'instance.</summary>
    /// <param name="request">Demande de cloture.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de l'instance a la fin du pas.</returns>
    Task<ProcessExecutionResult> CompleteInteractionAsync(InteractionCompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Declenche les attentes temporelles echues.</summary>
    /// <param name="maxCount">Nombre maximum d'attentes traitees.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de chaque instance reveillee.</returns>
    Task<IReadOnlyList<ProcessExecutionResult>> FireDueTimersAsync(int maxCount = 50, CancellationToken cancellationToken = default);

    /// <summary>Declenche une attente temporelle precise, designee par la tache programmee.</summary>
    /// <param name="instanceId">Instance concernee.</param>
    /// <param name="waitId">Attente concernee, ou <c>null</c> pour prendre l'attente echue de l'instance.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de l'instance a la fin du pas.</returns>
    Task<ProcessExecutionResult> FireTimerAsync(Guid instanceId, Guid? waitId = null, CancellationToken cancellationToken = default);

    /// <summary>Annule une instance et, en cascade, ses sous-processus encore actifs.</summary>
    /// <param name="instanceId">Instance a annuler.</param>
    /// <param name="reason">Motif consigne dans l'historique.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>L'etat de l'instance apres annulation.</returns>
    Task<ProcessExecutionResult> CancelAsync(Guid instanceId, string? reason = null, CancellationToken cancellationToken = default);
}
