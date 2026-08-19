using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Execution;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Traitement metier declenche par un noeud metier. L'implementation appartient a l'application
/// cliente et s'execute dans la transaction fournie par celle-ci.
/// </summary>
/// <typeparam name="TCommand">Type de la commande traitee.</typeparam>
public interface ICommandHandler<in TCommand>
{
    /// <summary>Execute la commande.</summary>
    /// <param name="command">Commande a executer.</param>
    /// <param name="context">Contexte d'execution du noeud.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>
    /// Le resultat qui selectionne la transition sortante, ou <c>null</c> pour emprunter
    /// la transition par defaut.
    /// </returns>
    Task<string?> HandleAsync(TCommand command, INodeExecutionContext context, CancellationToken cancellationToken);
}
