using System;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Permet a l'application cliente de rendre une session courante le temps d'une portee.
/// </summary>
public interface IDbSessionScopeFactory
{
    /// <summary>Rend <paramref name="session"/> courante jusqu'a liberation de la portee retournee.</summary>
    /// <param name="session">Session a activer.</param>
    /// <returns>Portee a liberer pour restaurer la session precedente.</returns>
    IDisposable BeginSession(IDbSession session);
}
