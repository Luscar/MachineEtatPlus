using System;
using System.Data.Common;
using System.Threading;

namespace MachineEtatPlus.Abstractions;

/// <summary>
/// Implementation par defaut qui conserve la session courante dans le flux d'execution asynchrone.
/// </summary>
/// <remarks>
/// Usage typique cote client :
/// <code>
/// using DbTransaction tx = connection.BeginTransaction();
/// using (accessor.BeginSession(new DbSession(connection, tx)))
/// {
///     await engine.StartAsync(request, ct);
/// }
/// tx.Commit();
/// </code>
/// </remarks>
public sealed class AmbientDbSessionAccessor : IDbSessionAccessor, IDbSessionScopeFactory
{
    private static readonly AsyncLocal<IDbSession?> Ambient = new AsyncLocal<IDbSession?>();

    /// <inheritdoc />
    public IDbSession? Current => Ambient.Value;

    /// <inheritdoc />
    public IDisposable BeginSession(IDbSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        IDbSession? previous = Ambient.Value;
        Ambient.Value = session;
        return new Scope(previous);
    }

    /// <summary>Rend courante une session batie sur la connexion et la transaction fournies.</summary>
    /// <param name="connection">Connexion ouverte.</param>
    /// <param name="transaction">Transaction ambiante, ou <c>null</c>.</param>
    /// <returns>Portee a liberer pour restaurer la session precedente.</returns>
    public IDisposable BeginSession(DbConnection connection, DbTransaction? transaction = null)
        => BeginSession(new DbSession(connection, transaction));

    private sealed class Scope : IDisposable
    {
        private readonly IDbSession? _previous;
        private bool _disposed;

        public Scope(IDbSession? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Ambient.Value = _previous;
        }
    }
}
