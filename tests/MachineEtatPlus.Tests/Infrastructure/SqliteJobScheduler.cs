using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;

namespace MachineEtatPlus.Tests.Infrastructure;

/// <summary>
/// Gestion de taches cliente de reference, en table d'outbox : les reveils sont ecrits dans la
/// meme transaction que l'avancement, donc un rollback les fait disparaitre avec lui.
/// </summary>
internal sealed class SqliteJobScheduler : IProcessJobScheduler
{
    private readonly IDbSessionAccessor _sessions;

    public SqliteJobScheduler(IDbSessionAccessor sessions) => _sessions = sessions;

    public async Task ScheduleAsync(ProcessJob job, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "INSERT INTO process_job (instance_id, node_id, kind, run_at, wait_id, reason) " +
            "VALUES (@instance, @node, @kind, @runAt, @wait, @reason)");

        AddParameter(command, "@instance", job.InstanceId.ToString());
        AddParameter(command, "@node", job.NodeId);
        AddParameter(command, "@kind", (int)job.Kind);
        AddParameter(command, "@runAt", job.RunAt?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        AddParameter(command, "@wait", job.WaitId?.ToString());
        AddParameter(command, "@reason", job.Reason);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "UPDATE process_job SET cancelled = 1 WHERE instance_id = @instance AND cancelled = 0");
        AddParameter(command, "@instance", instanceId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ScheduledJob>> GetPendingAsync()
    {
        using DbCommand command = CreateCommand(
            "SELECT instance_id, node_id, kind, run_at, wait_id, reason FROM process_job WHERE cancelled = 0 ORDER BY id");

        var results = new List<ScheduledJob>();
        using DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            results.Add(new ScheduledJob(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                (ProcessJobKind)reader.GetInt32(2),
                reader.IsDBNull(3) ? null : DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return results;
    }

    private DbCommand CreateCommand(string sql)
    {
        IDbSession session = _sessions.Current
            ?? throw new InvalidOperationException("Aucune session active.");
        DbCommand command = session.Connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = session.Transaction;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}

internal sealed record ScheduledJob(
    Guid InstanceId,
    string NodeId,
    ProcessJobKind Kind,
    DateTimeOffset? RunAt,
    Guid? WaitId,
    string? Reason);
