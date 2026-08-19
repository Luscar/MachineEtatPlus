using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;
using MachineEtatPlus.Tests.Infrastructure;
using Xunit;

namespace MachineEtatPlus.Tests;

public class SubProcessTests
{
    private static ProcessDefinition Expedition() => ProcessDefinition.Create("expedition")
        .Start("debut", to: "etiqueter")
        .Business("etiqueter", (ctx, _) =>
        {
            ctx.SetVariable("numeroSuivi", $"SUIVI-{ctx.GetVariable<int>("commandeId")}");
            return Task.CompletedTask;
        }, to: "fin")
        .End("fin", outcome: "expediee")
        .Build();

    private static ProcessDefinition ExpeditionAvecAttente() => ProcessDefinition.Create("expedition")
        .Start("debut", to: "attenteTransporteur")
        .Wait("attenteTransporteur", w => w.ForSignal("colis-remis").OnSignal("etiqueter"))
        .Business("etiqueter", (ctx, _) =>
        {
            ctx.SetVariable("numeroSuivi", $"SUIVI-{ctx.GetVariable<int>("commandeId")}");
            return Task.CompletedTask;
        }, to: "fin")
        .End("fin", outcome: "expediee")
        .Build();

    private static ProcessDefinition Commande(string childKey = "expedition") => ProcessDefinition.Create("commande")
        .Start("debut", to: "expedier")
        .SubProcess("expedier", childKey, s => s
            .Input((ctx, vars) => vars.Set("commandeId", ctx.GetVariable<int>("commandeId")))
            .Output((ctx, childVars) => ctx.SetVariable("suivi", childVars.Get<string>("numeroSuivi")))
            .When("expediee", "fin")
            .When("cancelled", "annulee"))
        .End("fin", outcome: "livree")
        .End("annulee", outcome: "annulee")
        .Build();

    [Fact]
    public async Task Un_sous_processus_synchrone_se_deroule_dans_la_meme_transaction()
    {
        using var host = new ProcessTestHost(new[] { Commande(), Expedition() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("commandeId", 1001)));

        Assert.Equal(ProcessStatus.Completed, result.Status);
        Assert.Equal("livree", result.Outcome);

        ProcessInstance parent = await host.GetRequiredInstanceAsync(result.InstanceId);
        Assert.Equal("SUIVI-1001", parent.Variables.Get<string>("suivi"));

        ProcessInstance enfant = Assert.Single(await host.GetChildrenAsync(result.InstanceId));
        Assert.Equal(ProcessStatus.Completed, enfant.Status);
        Assert.Equal("expediee", enfant.Outcome);
        Assert.Equal(1, enfant.Depth);
        Assert.Equal("expedier", enfant.ParentNodeId);
    }

    [Fact]
    public async Task Un_sous_processus_bloque_met_le_parent_en_attente_puis_le_relance()
    {
        using var host = new ProcessTestHost(new[] { Commande(), ExpeditionAvecAttente() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("commandeId", 2002)));

        Assert.Equal(ProcessStatus.Waiting, depart.Status);
        Assert.Equal(WaitKind.SubProcess, depart.WaitKind);

        ProcessInstance enfant = Assert.Single(await host.GetChildrenAsync(depart.InstanceId));
        Assert.Equal(ProcessStatus.Waiting, enfant.Status);

        ProcessWait attenteParent = Assert.Single(await host.GetWaitsAsync(depart.InstanceId));
        Assert.Equal(WaitKind.SubProcess, attenteParent.Kind);
        Assert.Equal(enfant.Id, attenteParent.ChildInstanceId);

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("colis-remis")));

        Assert.Equal(enfant.Id, Assert.Single(reveils).InstanceId);

        ProcessInstance parent = await host.GetRequiredInstanceAsync(depart.InstanceId);
        Assert.Equal(ProcessStatus.Completed, parent.Status);
        Assert.Equal("livree", parent.Outcome);
        Assert.Equal("SUIVI-2002", parent.Variables.Get<string>("suivi"));
        Assert.Equal(0, await host.CountAsync("process_wait"));
    }

    [Fact]
    public async Task Un_sous_processus_detache_laisse_le_parent_poursuivre()
    {
        ProcessDefinition parentDefinition = ProcessDefinition.Create("commande")
            .Start("debut", to: "notifier")
            .SubProcess("notifier", "expedition", s => s
                .FireAndForget()
                .Input((ctx, vars) => vars.Set("commandeId", ctx.GetVariable<int>("commandeId")))
                .To("fin"))
            .End("fin", outcome: "enregistree")
            .Build();

        using var host = new ProcessTestHost(new[] { parentDefinition, ExpeditionAvecAttente() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("commandeId", 3003)));

        Assert.Equal(ProcessStatus.Completed, result.Status);
        Assert.Equal("enregistree", result.Outcome);

        Assert.Empty(await host.GetChildrenAsync(result.InstanceId));
        Assert.Equal(2, await host.CountAsync("process_instance"));
    }

    [Fact]
    public async Task L_annulation_d_un_enfant_remonte_au_parent()
    {
        using var host = new ProcessTestHost(new[] { Commande(), ExpeditionAvecAttente() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("commandeId", 4004)));
        ProcessInstance enfant = Assert.Single(await host.GetChildrenAsync(depart.InstanceId));

        await host.InTransactionAsync(engine => engine.CancelAsync(enfant.Id, "transporteur indisponible"));

        ProcessInstance parent = await host.GetRequiredInstanceAsync(depart.InstanceId);
        Assert.Equal(ProcessStatus.Completed, parent.Status);
        Assert.Equal("annulee", parent.Outcome);
    }

    [Fact]
    public async Task L_annulation_du_parent_annule_aussi_les_enfants()
    {
        using var host = new ProcessTestHost(new[] { Commande(), ExpeditionAvecAttente() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("commandeId", 5005)));
        ProcessInstance enfant = Assert.Single(await host.GetChildrenAsync(depart.InstanceId));

        ProcessExecutionResult annulation = await host.InTransactionAsync(engine =>
            engine.CancelAsync(depart.InstanceId, "commande annulee par le client"));

        Assert.Equal(ProcessStatus.Cancelled, annulation.Status);
        Assert.Equal(ProcessStatus.Cancelled, (await host.GetRequiredInstanceAsync(enfant.Id)).Status);
        Assert.Equal(0, await host.CountAsync("process_wait"));
        Assert.Empty(await host.GetPendingJobsAsync());
    }

    [Fact]
    public async Task Un_echec_d_enfant_non_prevu_fait_echouer_le_parent()
    {
        ProcessDefinition enfant = ProcessDefinition.Create("expedition")
            .Start("debut", to: "ko")
            .Fail("ko", outcome: "impossible")
            .Build();

        ProcessDefinition parent = ProcessDefinition.Create("commande")
            .Start("debut", to: "expedier")
            .SubProcess("expedier", "expedition", s => s.When("expediee", "fin"))
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(new[] { parent, enfant });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande")));

        Assert.Equal(ProcessStatus.Faulted, result.Status);
        Assert.Contains("faulted", result.FaultMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Un_echec_d_enfant_prevu_est_rattrape_par_le_parent()
    {
        ProcessDefinition enfant = ProcessDefinition.Create("expedition")
            .Start("debut", to: "ko")
            .Fail("ko", outcome: "impossible")
            .Build();

        ProcessDefinition parent = ProcessDefinition.Create("commande")
            .Start("debut", to: "expedier")
            .SubProcess("expedier", "expedition", s => s
                .When("expediee", "fin")
                .When("faulted", "repli"))
            .Business("repli", (_, _) => Task.CompletedTask, to: "fin")
            .End("fin", outcome: "traitee")
            .Build();

        using var host = new ProcessTestHost(new[] { parent, enfant });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande")));

        Assert.Equal(ProcessStatus.Completed, result.Status);
        Assert.Equal("traitee", result.Outcome);
    }

    [Fact]
    public async Task La_profondeur_d_imbrication_est_bornee()
    {
        ProcessDefinition recursif = ProcessDefinition.Create("recursif")
            .Start("debut", to: "encore")
            .SubProcess("encore", "recursif", s => s.To("fin"))
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(
            new[] { recursif },
            options => options.MaxSubProcessDepth = 3);

        ProcessEngineException error = await Assert.ThrowsAsync<ProcessEngineException>(() =>
            host.InRolledBackTransactionAsync(engine => engine.StartAsync(new ProcessStartRequest("recursif"))));

        Assert.Contains("profondeur maximale", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Les_pas_de_l_enfant_sont_comptes_dans_le_budget_du_parent()
    {
        using var host = new ProcessTestHost(new[] { Commande(), Expedition() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("commande").With("commandeId", 6006)));

        // parent : debut, expedier, fin ; enfant : debut, etiqueter, fin
        Assert.Equal(6, result.StepsExecuted);
    }
}
