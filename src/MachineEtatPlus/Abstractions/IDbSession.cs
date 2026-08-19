using System.Data.Common;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Unite de travail fournie par l'application cliente : la connexion et, le cas echeant,
/// la transaction dans lesquelles le moteur et les handlers metier doivent travailler.
/// </summary>
/// <remarks>
/// Le moteur n'ouvre, ne valide et n'annule jamais de transaction : il se contente d'utiliser
/// celle qui lui est fournie. C'est l'appelant qui decide du <c>Commit</c> ou du <c>Rollback</c>.
/// </remarks>
public interface IDbSession
{
    /// <summary>Connexion ouverte a utiliser pour toutes les commandes SQL du pas d'execution.</summary>
    DbConnection Connection { get; }

    /// <summary>Transaction ambiante, ou <c>null</c> si l'appelant travaille en auto-commit.</summary>
    DbTransaction? Transaction { get; }
}
