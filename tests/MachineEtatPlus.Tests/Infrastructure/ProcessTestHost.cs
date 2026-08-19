using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;
using Microsoft.Data.Sqlite;

namespace MachineEtatPlus.Tests.Infrastructure;

internal sealed class TestClock : IProcessClock
{
    public TestClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }

    public void Advance(TimeSpan delta) => UtcNow += delta;
}

/// <summary>
/// Monte une base SQLite en memoire, le stockage et l'ordonnanceur clients, et le moteur.
/// La connexion appartient a l'hote : c'est lui qui ouvre et referme les transactions,
/// exactement comme le ferait l'application cliente.
/// </summary>
internal sealed class ProcessTestHost : IDisposable
{
    private readonly SqliteConnection _connection;

    public ProcessTestHost(
        IEnumerable<ProcessDefinition> definitions,
        Action<ProcessEngineOptions>? configureOptions = null,
        ICommandDispatcher? commandDispatcher = null,
        IDecisionDispatcher? decisionDispatcher = null,
        DateTimeOffset? now = null)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteSchema.Create(_connection);

        Clock = new TestClock(now ?? new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));
        Sessions = new AmbientDbSessionAccessor();
        Store = new SqliteProcessStore(Sessions);
        Scheduler = new SqliteJobScheduler(Sessions);
        Definitions = new ProcessDefinitionRegistry(definitions);

        Options = new ProcessEngineOptions();
        configureOptions?.Invoke(Options);

        Engine = new ProcessEngine(
            Definitions,
            Store,
            Scheduler,
            Sessions,
            commandDispatcher,
            decisionDispatcher,
            Clock,
            Options);
    }

    public TestClock Clock { get; }

    public DbConnection Connection => _connection;

    public AmbientDbSessionAccessor Sessions { get; }

    public SqliteProcessStore Store { get; }

    public SqliteJobScheduler Scheduler { get; }

    public ProcessDefinitionRegistry Definitions { get; }

    public ProcessEngineOptions Options { get; }

    public ProcessEngine Engine { get; }

    /// <summary>Execute une action dans une transaction validee, comme le ferait le client.</summary>
    public async Task<T> InTransactionAsync<T>(Func<IProcessEngine, Task<T>> action)
    {
        using DbTransaction transaction = _connection.BeginTransaction();
        T result;
        using (Sessions.BeginSession(new DbSession(_connection, transaction)))
        {
            result = await action(Engine).ConfigureAwait(false);
        }

        transaction.Commit();
        return result;
    }

    /// <summary>Execute une action dans une transaction annulee : rien ne doit subsister.</summary>
    public async Task<T> InRolledBackTransactionAsync<T>(Func<IProcessEngine, Task<T>> action)
    {
        using DbTransaction transaction = _connection.BeginTransaction();
        try
        {
            using (Sessions.BeginSession(new DbSession(_connection, transaction)))
            {
                return await action(Engine).ConfigureAwait(false);
            }
        }
        finally
        {
            transaction.Rollback();
        }
    }

    /// <summary>Lecture hors transaction, pour verifier ce qui a reellement ete valide.</summary>
    public async Task<T> ReadAsync<T>(Func<Task<T>> action)
    {
        using (Sessions.BeginSession(new DbSession(_connection, null)))
        {
            return await action().ConfigureAwait(false);
        }
    }

    public Task<ProcessInstance?> GetInstanceAsync(Guid instanceId)
        => ReadAsync(() => Store.GetInstanceAsync(instanceId, false, CancellationToken.None));

    public async Task<ProcessInstance> GetRequiredInstanceAsync(Guid instanceId)
        => await GetInstanceAsync(instanceId).ConfigureAwait(false)
           ?? throw new InvalidOperationException($"Instance '{instanceId}' absente.");

    public Task<IReadOnlyList<ProcessWait>> GetWaitsAsync(Guid instanceId)
        => ReadAsync(() => Store.GetWaitsAsync(instanceId, CancellationToken.None));

    public Task<IReadOnlyList<ScheduledJob>> GetPendingJobsAsync()
        => ReadAsync(() => Scheduler.GetPendingAsync());

    public async Task<IReadOnlyList<ProcessInstance>> GetChildrenAsync(Guid parentId)
        => await ReadAsync(() => Store.GetChildInstancesAsync(parentId, CancellationToken.None)).ConfigureAwait(false);

    /// <summary>Execute du DDL de test (tables metier de l'application cliente).</summary>
    public void ExecuteRaw(string sql)
    {
        using DbCommand command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public async Task<long> CountAsync(string table)
    {
        using DbCommand command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        object? scalar = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return Convert.ToInt64(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<(ProcessEventType Event, string? NodeId, string? Outcome)>> GetHistoryAsync(Guid instanceId)
    {
        using DbCommand command = _connection.CreateCommand();
        command.CommandText =
            "SELECT event_type, node_id, outcome FROM process_history WHERE instance_id = @id ORDER BY rowid";
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = instanceId.ToString();
        command.Parameters.Add(parameter);

        var results = new List<(ProcessEventType, string?, string?)>();
        using DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            results.Add((
                (ProcessEventType)reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return results;
    }

    public void Dispose()
    {
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
    }
}
