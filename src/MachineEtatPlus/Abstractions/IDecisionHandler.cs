using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Execution;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Evaluation d'une requete de decision. L'implementation appartient a l'application cliente
/// et ne doit pas modifier l'etat metier : elle repond a une question.
/// </summary>
/// <typeparam name="TQuery">Type de la requete evaluee.</typeparam>
public interface IDecisionHandler<in TQuery>
{
    /// <summary>Evalue la requete.</summary>
    /// <param name="query">Requete a evaluer.</param>
    /// <param name="context">Contexte d'execution du noeud.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le resultat qui selectionne la transition sortante.</returns>
    Task<string> EvaluateAsync(TQuery query, INodeExecutionContext context, CancellationToken cancellationToken);
}
