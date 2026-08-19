using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.DependencyInjection;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;
using MachineEtatPlus.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MachineEtatPlus.Tests;

public class DependencyInjectionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public DependencyInjectionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteSchema.Create(_connection);

        var collection = new ServiceCollection();
        collection.AddMachineEtatPlus(options => options.MaxStepsPerTransaction = 50)
            .AddDefinition(Definition())
            .UseStore<SqliteProcessStore>()
            .UseJobScheduler<SqliteJobScheduler>()
            .AddDecisionHandler<ClientSolvableQuery, ClientSolvableHandler>()
            .AddCommandHandler<ReserverStockCommand, ReserverStockHandler>();

        collection.AddSingleton(new JournalMetier());
        _services = collection.BuildServiceProvider(validateScopes: true);
    }

    private static ProcessDefinition Definition() => ProcessDefinition.Create("commande")
        .Start("debut", to: "solvabilite")
        .Decision("solvabilite", d => d
            .Query(ctx => new ClientSolvableQuery(ctx.GetVariable<int>("clientId")))
            .When("oui", "reserver")
            .When("non", "refus"))
        .Business("reserver", b => b
            .Command(ctx => new ReserverStockCommand(ctx.GetVariable<int>("commandeId")))
            .When("rupture", "refus")
            .To("fin"))
        .End("fin", outcome: "acceptee")
        .End("refus", outcome: "refusee")
        .Build();

    [Fact]
    public async Task Les_handlers_sont_resolus_depuis_le_conteneur()
    {
        ProcessExecutionResult result = await RunAsync(new ProcessStartRequest("commande")
            .With("clientId", 1)
            .With("commandeId", 1001));

        Assert.Equal(ProcessStatus.Completed, result.Status);
        Assert.Equal("acceptee", result.Outcome);

        JournalMetier journal = _services.GetRequiredService<JournalMetier>();
        Assert.Equal(new[] { "solvabilite:1", "reservation:1001" }, journal.Evenements);
    }

    [Fact]
    public async Task La_query_de_decision_aiguille_vers_le_refus()
    {
        ProcessExecutionResult result = await RunAsync(new ProcessStartRequest("commande")
            .With("clientId", 666)
            .With("commandeId", 1002));

        Assert.Equal("refusee", result.Outcome);
    }

    [Fact]
    public async Task Le_resultat_de_la_commande_peut_aiguiller()
    {
        ProcessExecutionResult result = await RunAsync(new ProcessStartRequest("commande")
            .With("clientId", 1)
            .With("commandeId", -1));

        Assert.Equal("refusee", result.Outcome);
    }

    [Fact]
    public async Task Une_commande_sans_handler_est_signalee_clairement()
    {
        var collection = new ServiceCollection();
        collection.AddMachineEtatPlus()
            .AddDefinition(Definition())
            .UseStore<SqliteProcessStore>()
            .UseJobScheduler<SqliteJobScheduler>();

        using ServiceProvider services = collection.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IDbSessionScopeFactory>();
        var engine = scope.ServiceProvider.GetRequiredService<IProcessEngine>();

        using DbTransaction transaction = _connection.BeginTransaction();
        try
        {
            using (sessions.BeginSession(new DbSession(_connection, transaction)))
            {
                ProcessHandlerNotFoundException error = await Assert.ThrowsAsync<ProcessHandlerNotFoundException>(() =>
                    engine.StartAsync(new ProcessStartRequest("commande").With("clientId", 1)));

                Assert.Equal(typeof(ClientSolvableQuery), error.MessageType);
            }
        }
        finally
        {
            transaction.Rollback();
        }
    }

    private async Task<ProcessExecutionResult> RunAsync(ProcessStartRequest request)
    {
        using IServiceScope scope = _services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IDbSessionScopeFactory>();
        var engine = scope.ServiceProvider.GetRequiredService<IProcessEngine>();

        using DbTransaction transaction = _connection.BeginTransaction();
        ProcessExecutionResult result;
        using (sessions.BeginSession(new DbSession(_connection, transaction)))
        {
            result = await engine.StartAsync(request);
        }

        transaction.Commit();
        return result;
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        GC.SuppressFinalize(this);
    }

    private sealed record ClientSolvableQuery(int ClientId);

    private sealed record ReserverStockCommand(int CommandeId);

    private sealed class JournalMetier
    {
        private readonly System.Collections.Generic.List<string> _evenements = new();

        public System.Collections.Generic.IReadOnlyList<string> Evenements => _evenements;

        public void Ajouter(string evenement) => _evenements.Add(evenement);
    }

    private sealed class ClientSolvableHandler : IDecisionHandler<ClientSolvableQuery>
    {
        private readonly JournalMetier _journal;

        public ClientSolvableHandler(JournalMetier journal) => _journal = journal;

        public Task<string> EvaluateAsync(ClientSolvableQuery query, INodeExecutionContext context, CancellationToken cancellationToken)
        {
            _journal.Ajouter($"solvabilite:{query.ClientId}");
            Assert.NotNull(context.Session.Transaction);
            return Task.FromResult(query.ClientId == 666 ? "non" : "oui");
        }
    }

    private sealed class ReserverStockHandler : ICommandHandler<ReserverStockCommand>
    {
        private readonly JournalMetier _journal;

        public ReserverStockHandler(JournalMetier journal) => _journal = journal;

        public Task<string?> HandleAsync(ReserverStockCommand command, INodeExecutionContext context, CancellationToken cancellationToken)
        {
            if (command.CommandeId < 0)
            {
                return Task.FromResult<string?>("rupture");
            }

            _journal.Ajouter($"reservation:{command.CommandeId}");
            context.SetVariable("reserve", true);
            return Task.FromResult<string?>(null);
        }
    }
}
