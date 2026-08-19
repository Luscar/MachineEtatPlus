namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Donne au moteur acces a la session (connexion + transaction) courante de l'application cliente.
/// </summary>
/// <remarks>
/// Implementez cette interface pour brancher le moteur sur votre propre unite de travail
/// (par exemple un <c>DbContext</c> Entity Framework ou un <c>UnitOfWork</c> maison).
/// A defaut, <see cref="AmbientDbSessionAccessor"/> fournit une implementation basee sur
/// un contexte asynchrone.
/// </remarks>
public interface IDbSessionAccessor
{
    /// <summary>Session courante, ou <c>null</c> si aucune n'est active.</summary>
    IDbSession? Current { get; }
}
