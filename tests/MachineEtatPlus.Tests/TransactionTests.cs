using System;
using System.Data.Common;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;
using MachineEtatPlus.Tests.Infrastructure;
using Xunit;

namespace MachineEtatPlus.Tests;

/// <summary>
/// Verifie le contrat central : le moteur travaille dans la transaction du client et n'en
/// prend jamais le controle.
/// </summary>
public class TransactionTests
{
    private static ProcessDefinition Definition() => ProcessDefinition.Create("commande")
        .Start("debut", to: "reserver")
        .Business("reserver", (ctx, ct) => ReserverAsync(ctx, ct), to: "attente")
        .Wait("attente", w => w.After(TimeSpan.FromHours(1)).OnTimer("fin"))
        .End("fin", outcome: "ok")
        .Build();

    /// <summary>Ecriture metier passant par la connexion et la transaction du client.</summary>
    private static async Task ReserverAsync(INodeExecutionContext context, System.Threading.CancellationToken cancellationToken)
    {
        IDbSession session = context.Session;
        using DbCommand command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = "INSERT INTO reservation (reference) VALUES (@reference)";

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@reference";
        parameter.Value = context.GetVariable<string>("reference") ?? "?";
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ProcessTestHost CreateHost()
    {
        var host = new ProcessTestHost(new[] { Definition() });
        host.ExecuteRaw("CREATE TABLE reservation (reference TEXT NOT NULL)");
        return host;
    }

    [Fact]
    public async Task Un_rollback_du_client_efface_l_avancement_et_les_ecritures_metier()
    {
        using ProcessTestHost host = CreateHost();

        ProcessExecutionResult result = await host.InRolledBackTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("reference", "CMD-1")));

        Assert.Equal(ProcessStatus.Waiting, result.Status);

        // Rien n'a survecu : ni l'instance, ni l'attente, ni la tache, ni l'ecriture metier.
        Assert.Equal(0, await host.CountAsync("process_instance"));
        Assert.Equal(0, await host.CountAsync("process_wait"));
        Assert.Equal(0, await host.CountAsync("process_job"));
        Assert.Equal(0, await host.CountAsync("process_history"));
        Assert.Equal(0, await host.CountAsync("reservation"));
    }

    [Fact]
    public async Task Un_commit_du_client_publie_l_avancement_et_les_ecritures_metier_ensemble()
    {
        using ProcessTestHost host = CreateHost();

        await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("reference", "CMD-1")));

        Assert.Equal(1, await host.CountAsync("process_instance"));
        Assert.Equal(1, await host.CountAsync("process_wait"));
        Assert.Equal(1, await host.CountAsync("process_job"));
        Assert.Equal(1, await host.CountAsync("reservation"));
    }

    [Fact]
    public async Task Le_moteur_fonctionne_aussi_sans_transaction_explicite()
    {
        using ProcessTestHost host = CreateHost();

        ProcessExecutionResult result;
        using (host.Sessions.BeginSession(new DbSession(host.Connection, null)))
        {
            result = await host.Engine.StartAsync(new ProcessStartRequest("commande").With("reference", "CMD-2"));
        }

        Assert.Equal(ProcessStatus.Waiting, result.Status);
        Assert.Equal(1, await host.CountAsync("process_instance"));
    }

    [Fact]
    public async Task La_session_est_exposee_aux_traitements_metier()
    {
        ProcessDefinition definition = ProcessDefinition.Create("inspection")
            .Start("debut", to: "verifier")
            .Business("verifier", (ctx, _) =>
            {
                Assert.NotNull(ctx.Session.Connection);
                Assert.NotNull(ctx.Session.Transaction);
                Assert.Same(ctx.Session.Connection, ctx.Session.Transaction!.Connection);
                return Task.CompletedTask;
            }, to: "fin")
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("inspection")));

        Assert.Equal(ProcessStatus.Completed, result.Status);
    }
}
