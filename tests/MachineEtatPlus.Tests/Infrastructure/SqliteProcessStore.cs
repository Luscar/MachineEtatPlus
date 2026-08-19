using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Tests.Infrastructure;

/// <summary>
/// Implementation cliente de reference : elle travaille exclusivement sur la connexion et la
/// transaction exposees par <see cref="IDbSessionAccessor"/>, et ne valide jamais la transaction.
/// </summary>
internal sealed class SqliteProcessStore : IProcessStore
{
    private readonly IDbSessionAccessor _sessions;

    public SqliteProcessStore(IDbSessionAccessor sessions) => _sessions = sessions;

    private IDbSession Session => _sessions.Current
        ?? throw new InvalidOperationException("Aucune session active : le client doit fournir sa connexion et sa transaction.");

    public async Task<ProcessInstance?> GetInstanceAsync(Guid instanceId, bool forUpdate, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "SELECT id, definition_key, definition_version, status, current_node_id, variables, " +
            "parent_instance_id, parent_node_id, depth, correlation_id, outcome, fault_message, " +
            "fault_detail, created_at, updated_at, completed_at, row_version " +
            "FROM process_instance WHERE id = @id");
        AddParameter(command, "@id", instanceId.ToString());

        using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task InsertInstanceAsync(ProcessInstance instance, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "INSERT INTO process_instance (id, definition_key, definition_version, status, current_node_id, " +
            "variables, parent_instance_id, parent_node_id, depth, correlation_id, outcome, fault_message, " +
            "fault_detail, created_at, updated_at, completed_at, row_version) " +
            "VALUES (@id, @key, @version, @status, @node, @variables, @parent, @parentNode, @depth, " +
            "@correlation, @outcome, @faultMessage, @faultDetail, @createdAt, @updatedAt, @completedAt, 1)");

        BindInstance(command, instance);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        instance.ConcurrencyToken = 1L;
    }

    public async Task UpdateInstanceAsync(ProcessInstance instance, CancellationToken cancellationToken)
    {
        long expected = instance.ConcurrencyToken is long token ? token : 1L;

        using DbCommand command = CreateCommand(
            "UPDATE process_instance SET status = @status, current_node_id = @node, variables = @variables, " +
            "parent_instance_id = @parent, parent_node_id = @parentNode, depth = @depth, " +
            "correlation_id = @correlation, outcome = @outcome, fault_message = @faultMessage, " +
            "fault_detail = @faultDetail, updated_at = @updatedAt, completed_at = @completedAt, " +
            "row_version = row_version + 1 " +
            "WHERE id = @id AND row_version = @expected");

        BindInstance(command, instance);
        AddParameter(command, "@expected", expected);

        int affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (affected == 0)
        {
            throw new InvalidOperationException(
                $"Conflit de concurrence sur l'instance '{instance.Id}' : elle a ete modifiee par une autre transaction.");
        }

        instance.ConcurrencyToken = expected + 1;
    }

    public async Task<IReadOnlyList<ProcessInstance>> GetChildInstancesAsync(Guid parentInstanceId, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "SELECT id, definition_key, definition_version, status, current_node_id, variables, " +
            "parent_instance_id, parent_node_id, depth, correlation_id, outcome, fault_message, " +
            "fault_detail, created_at, updated_at, completed_at, row_version " +
            "FROM process_instance WHERE parent_instance_id = @parent");
        AddParameter(command, "@parent", parentInstanceId.ToString());

        var results = new List<ProcessInstance>();
        using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task InsertWaitAsync(ProcessWait wait, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "INSERT INTO process_wait (id, instance_id, node_id, kind, due_at, signal_name, correlation_key, " +
            "interaction_key, assignee, child_instance_id, created_at) " +
            "VALUES (@id, @instance, @node, @kind, @dueAt, @signal, @correlation, @interaction, @assignee, @child, @createdAt)");

        AddParameter(command, "@id", wait.Id.ToString());
        AddParameter(command, "@instance", wait.InstanceId.ToString());
        AddParameter(command, "@node", wait.NodeId);
        AddParameter(command, "@kind", (int)wait.Kind);
        AddParameter(command, "@dueAt", Format(wait.DueAt));
        AddParameter(command, "@signal", wait.SignalName);
        AddParameter(command, "@correlation", wait.CorrelationKey);
        AddParameter(command, "@interaction", wait.InteractionKey);
        AddParameter(command, "@assignee", wait.Assignee);
        AddParameter(command, "@child", wait.ChildInstanceId?.ToString());
        AddParameter(command, "@createdAt", Format(wait.CreatedAt));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<ProcessWait>> GetWaitsAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        DbCommand command = CreateCommand($"{WaitColumns} WHERE instance_id = @instance ORDER BY created_at");
        AddParameter(command, "@instance", instanceId.ToString());
        return ReadWaitsAsync(command, cancellationToken);
    }

    public async Task DeleteWaitsAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand("DELETE FROM process_wait WHERE instance_id = @instance");
        AddParameter(command, "@instance", instanceId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<ProcessWait>> FindSignalWaitsAsync(
        string signalName,
        string? correlationKey,
        int maxCount,
        CancellationToken cancellationToken)
    {
        string sql = correlationKey is null
            ? $"{WaitColumns} WHERE kind = @kind AND signal_name = @signal ORDER BY created_at LIMIT @max"
            : $"{WaitColumns} WHERE kind = @kind AND signal_name = @signal AND correlation_key = @correlation ORDER BY created_at LIMIT @max";

        DbCommand command = CreateCommand(sql);
        AddParameter(command, "@kind", (int)WaitKind.Signal);
        AddParameter(command, "@signal", signalName);
        AddParameter(command, "@max", maxCount);
        if (correlationKey is not null)
        {
            AddParameter(command, "@correlation", correlationKey);
        }

        return ReadWaitsAsync(command, cancellationToken);
    }

    public Task<IReadOnlyList<ProcessWait>> FindDueTimerWaitsAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken)
    {
        DbCommand command = CreateCommand(
            $"{WaitColumns} WHERE kind = @kind AND due_at IS NOT NULL AND due_at <= @now ORDER BY due_at LIMIT @max");
        AddParameter(command, "@kind", (int)WaitKind.Timer);
        AddParameter(command, "@now", Format(now));
        AddParameter(command, "@max", maxCount);
        return ReadWaitsAsync(command, cancellationToken);
    }

    public async Task AppendHistoryAsync(ProcessHistoryEntry entry, CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(
            "INSERT INTO process_history (id, instance_id, event_type, node_id, node_kind, outcome, detail, occurred_at) " +
            "VALUES (@id, @instance, @eventType, @node, @nodeKind, @outcome, @detail, @occurredAt)");

        AddParameter(command, "@id", entry.Id.ToString());
        AddParameter(command, "@instance", entry.InstanceId.ToString());
        AddParameter(command, "@eventType", (int)entry.EventType);
        AddParameter(command, "@node", entry.NodeId);
        AddParameter(command, "@nodeKind", entry.NodeKind is null ? null : (int?)entry.NodeKind.Value);
        AddParameter(command, "@outcome", entry.Outcome);
        AddParameter(command, "@detail", entry.Detail);
        AddParameter(command, "@occurredAt", Format(entry.OccurredAt));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private const string WaitColumns =
        "SELECT id, instance_id, node_id, kind, due_at, signal_name, correlation_key, interaction_key, " +
        "assignee, child_instance_id, created_at FROM process_wait";

    private static async Task<IReadOnlyList<ProcessWait>> ReadWaitsAsync(DbCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<ProcessWait>();
            using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var wait = new ProcessWait(
                    Guid.Parse(reader.GetString(0)),
                    Guid.Parse(reader.GetString(1)),
                    reader.GetString(2),
                    (WaitKind)reader.GetInt32(3),
                    ParseRequired(reader.GetString(10)))
                {
                    DueAt = Parse(reader, 4),
                    SignalName = GetNullableString(reader, 5),
                    CorrelationKey = GetNullableString(reader, 6),
                    InteractionKey = GetNullableString(reader, 7),
                    Assignee = GetNullableString(reader, 8),
                    ChildInstanceId = reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9)),
                };
                results.Add(wait);
            }

            return results;
        }
        finally
        {
            command.Dispose();
        }
    }

    private static ProcessInstance Map(DbDataReader reader)
        => ProcessInstance.Restore(
            id: Guid.Parse(reader.GetString(0)),
            definitionKey: reader.GetString(1),
            definitionVersion: reader.GetInt32(2),
            status: (ProcessStatus)reader.GetInt32(3),
            currentNodeId: reader.GetString(4),
            variablesJson: GetNullableString(reader, 5),
            createdAt: ParseRequired(reader.GetString(13)),
            updatedAt: ParseRequired(reader.GetString(14)),
            parentInstanceId: reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6)),
            parentNodeId: GetNullableString(reader, 7),
            depth: reader.GetInt32(8),
            correlationId: GetNullableString(reader, 9),
            outcome: GetNullableString(reader, 10),
            faultMessage: GetNullableString(reader, 11),
            faultDetail: GetNullableString(reader, 12),
            completedAt: Parse(reader, 15),
            concurrencyToken: reader.GetInt64(16));

    private static void BindInstance(DbCommand command, ProcessInstance instance)
    {
        AddParameter(command, "@id", instance.Id.ToString());
        AddParameter(command, "@key", instance.DefinitionKey);
        AddParameter(command, "@version", instance.DefinitionVersion);
        AddParameter(command, "@status", (int)instance.Status);
        AddParameter(command, "@node", instance.CurrentNodeId);
        AddParameter(command, "@variables", instance.VariablesJson);
        AddParameter(command, "@parent", instance.ParentInstanceId?.ToString());
        AddParameter(command, "@parentNode", instance.ParentNodeId);
        AddParameter(command, "@depth", instance.Depth);
        AddParameter(command, "@correlation", instance.CorrelationId);
        AddParameter(command, "@outcome", instance.Outcome);
        AddParameter(command, "@faultMessage", instance.FaultMessage);
        AddParameter(command, "@faultDetail", instance.FaultDetail);
        AddParameter(command, "@createdAt", Format(instance.CreatedAt));
        AddParameter(command, "@updatedAt", Format(instance.UpdatedAt));
        AddParameter(command, "@completedAt", Format(instance.CompletedAt));
    }

    private DbCommand CreateCommand(string sql)
    {
        IDbSession session = Session;
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

    private static string? GetNullableString(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static string? Format(DateTimeOffset? value)
        => value?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static string Format(DateTimeOffset value)
        => value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static DateTimeOffset? Parse(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : ParseRequired(reader.GetString(ordinal));

    private static DateTimeOffset ParseRequired(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
