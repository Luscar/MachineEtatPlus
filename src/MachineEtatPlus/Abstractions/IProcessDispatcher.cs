using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Execution;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Route une commande de noeud metier vers son <see cref="ICommandHandler{TCommand}"/>.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>Execute la commande via le handler correspondant.</summary>
    /// <param name="command">Commande a executer.</param>
    /// <param name="context">Contexte d'execution du noeud.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le resultat qui selectionne la transition sortante, ou <c>null</c>.</returns>
    Task<string?> DispatchAsync(object command, INodeExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Route une requete de noeud decision vers son <see cref="IDecisionHandler{TQuery}"/>.
/// </summary>
public interface IDecisionDispatcher
{
    /// <summary>Evalue la requete via le handler correspondant.</summary>
    /// <param name="query">Requete a evaluer.</param>
    /// <param name="context">Contexte d'execution du noeud.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le resultat qui selectionne la transition sortante.</returns>
    Task<string> DispatchAsync(object query, INodeExecutionContext context, CancellationToken cancellationToken);
}
