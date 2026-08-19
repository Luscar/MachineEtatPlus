using System;
using System.Data.Common;

namespace MachineEtatPlus.Abstractions;

/// <summary>Implementation immuable de <see cref="IDbSession"/>.</summary>
public sealed class DbSession : IDbSession
{
    /// <summary>Cree une session a partir d'une connexion et d'une transaction optionnelle.</summary>
    /// <param name="connection">Connexion ouverte appartenant a l'appelant.</param>
    /// <param name="transaction">Transaction ambiante, ou <c>null</c>.</param>
    public DbSession(DbConnection connection, DbTransaction? transaction = null)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Transaction = transaction;
    }

    /// <inheritdoc />
    public DbConnection Connection { get; }

    /// <inheritdoc />
    public DbTransaction? Transaction { get; }

    /// <summary>
    /// Prepare une commande rattachee a la connexion et a la transaction de la session.
    /// </summary>
    /// <param name="commandText">Texte SQL de la commande.</param>
    /// <returns>La commande, a disposer par l'appelant.</returns>
    public DbCommand CreateCommand(string commandText)
    {
        DbCommand command = Connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = Transaction;
        return command;
    }
}
