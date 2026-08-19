using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;
using MachineEtatPlus.Tests.Infrastructure;
using Xunit;

namespace MachineEtatPlus.Tests;

public class WaitNodeTests
{
    private static ProcessDefinition AttenteSignal() => ProcessDefinition.Create("paiement")
        .Start("debut", to: "attente")
        .Wait("attente", w => w
            .ForSignal("paiement-recu", ctx => ctx.GetVariable<string>("reference"))
            .StorePayloadIn("paiement")
            .OnSignal("encaisse"))
        .Business("encaisse", (_, _) => Task.CompletedTask, to: "fin")
        .End("fin", outcome: "paye")
        .Build();

    private static ProcessDefinition AttenteEcheance() => ProcessDefinition.Create("relance")
        .Start("debut", to: "attente")
        .Wait("attente", w => w
            .After(TimeSpan.FromHours(48))
            .OnTimer("relancer"))
        .Business("relancer", (_, _) => Task.CompletedTask, to: "fin")
        .End("fin", outcome: "relancee")
        .Build();

    [Fact]
    public async Task L_attente_de_signal_bloque_l_instance_et_enregistre_l_attente()
    {
        using var host = new ProcessTestHost(new[] { AttenteSignal() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("paiement").With("reference", "CMD-42")));

        Assert.Equal(ProcessStatus.Waiting, result.Status);
        Assert.Equal(WaitKind.Signal, result.WaitKind);
        Assert.Equal("attente", result.CurrentNodeId);

        ProcessWait wait = Assert.Single(await host.GetWaitsAsync(result.InstanceId));
        Assert.Equal(WaitKind.Signal, wait.Kind);
        Assert.Equal("paiement-recu", wait.SignalName);
        Assert.Equal("CMD-42", wait.CorrelationKey);
        Assert.Null(wait.DueAt);

        Assert.Empty(await host.GetPendingJobsAsync());
    }

    [Fact]
    public async Task Le_signal_reveille_l_instance_et_depose_la_charge_utile()
    {
        using var host = new ProcessTestHost(new[] { AttenteSignal() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("paiement").With("reference", "CMD-42")));

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("paiement-recu")
            {
                CorrelationKey = "CMD-42",
                Payload = new Paiement("CB", 249.90m),
            }));

        ProcessExecutionResult reveil = Assert.Single(reveils);
        Assert.Equal(depart.InstanceId, reveil.InstanceId);
        Assert.Equal(ProcessStatus.Completed, reveil.Status);
        Assert.Equal("paye", reveil.Outcome);

        ProcessInstance persisted = await host.GetRequiredInstanceAsync(depart.InstanceId);
        Paiement? paiement = persisted.Variables.Get<Paiement>("paiement");
        Assert.NotNull(paiement);
        Assert.Equal("CB", paiement!.Moyen);
        Assert.Equal(249.90m, paiement.Montant);
        Assert.Equal(0, await host.CountAsync("process_wait"));
    }

    [Fact]
    public async Task Un_signal_dont_la_correlation_ne_correspond_pas_ne_reveille_rien()
    {
        using var host = new ProcessTestHost(new[] { AttenteSignal() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("paiement").With("reference", "CMD-42")));

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("paiement-recu") { CorrelationKey = "CMD-99" }));

        Assert.Empty(reveils);
        ProcessInstance persisted = await host.GetRequiredInstanceAsync(depart.InstanceId);
        Assert.Equal(ProcessStatus.Waiting, persisted.Status);
    }

    [Fact]
    public async Task Un_signal_peut_viser_une_instance_precise()
    {
        using var host = new ProcessTestHost(new[] { AttenteSignal() });

        ProcessExecutionResult premier = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("paiement").With("reference", "CMD-1")));
        ProcessExecutionResult second = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("paiement").With("reference", "CMD-2")));

        await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("paiement-recu") { InstanceId = second.InstanceId }));

        Assert.Equal(ProcessStatus.Waiting, (await host.GetRequiredInstanceAsync(premier.InstanceId)).Status);
        Assert.Equal(ProcessStatus.Completed, (await host.GetRequiredInstanceAsync(second.InstanceId)).Status);
    }

    [Fact]
    public async Task L_attente_d_echeance_programme_une_tache_datee()
    {
        using var host = new ProcessTestHost(new[] { AttenteEcheance() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("relance")));

        Assert.Equal(ProcessStatus.Waiting, result.Status);
        Assert.Equal(WaitKind.Timer, result.WaitKind);

        ProcessWait wait = Assert.Single(await host.GetWaitsAsync(result.InstanceId));
        Assert.Equal(WaitKind.Timer, wait.Kind);
        Assert.Equal(host.Clock.UtcNow.AddHours(48), wait.DueAt);

        ScheduledJob job = Assert.Single(await host.GetPendingJobsAsync());
        Assert.Equal(ProcessJobKind.Timer, job.Kind);
        Assert.Equal(host.Clock.UtcNow.AddHours(48), job.RunAt);
        Assert.Equal(wait.Id, job.WaitId);
    }

    [Fact]
    public async Task Les_echeances_arrivees_a_terme_relancent_les_instances()
    {
        using var host = new ProcessTestHost(new[] { AttenteEcheance() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("relance")));

        IReadOnlyList<ProcessExecutionResult> troptot = await host.InTransactionAsync(engine =>
            engine.FireDueTimersAsync());
        Assert.Empty(troptot);

        host.Clock.Advance(TimeSpan.FromHours(49));

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.FireDueTimersAsync());

        ProcessExecutionResult reveil = Assert.Single(reveils);
        Assert.Equal(depart.InstanceId, reveil.InstanceId);
        Assert.Equal(ProcessStatus.Completed, reveil.Status);
        Assert.Equal("relancee", reveil.Outcome);
    }

    [Fact]
    public async Task Une_tache_datee_peut_etre_declenchee_par_son_identifiant()
    {
        using var host = new ProcessTestHost(new[] { AttenteEcheance() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("relance")));
        ScheduledJob job = Assert.Single(await host.GetPendingJobsAsync());

        ProcessExecutionResult reveil = await host.InTransactionAsync(engine =>
            engine.FireTimerAsync(depart.InstanceId, job.WaitId));

        Assert.Equal(ProcessStatus.Completed, reveil.Status);
    }

    [Fact]
    public async Task Une_echeance_deja_passee_ne_bloque_pas_l_instance()
    {
        ProcessDefinition definition = ProcessDefinition.Create("passe")
            .Start("debut", to: "attente")
            .Wait("attente", w => w
                .Until(ctx => ctx.Now.AddHours(-1))
                .OnTimer("fin"))
            .End("fin", outcome: "immediat")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("passe")));

        Assert.Equal(ProcessStatus.Completed, result.Status);
        Assert.Equal("immediat", result.Outcome);
        Assert.Empty(await host.GetPendingJobsAsync());
    }

    [Fact]
    public async Task Une_attente_mixte_retient_le_signal_et_annule_l_echeance()
    {
        ProcessDefinition definition = ProcessDefinition.Create("mixte")
            .Start("debut", to: "attente")
            .Wait("attente", w => w
                .ForSignal("reponse")
                .After(TimeSpan.FromDays(3))
                .OnSignal("repondu")
                .OnTimer("abandon"))
            .End("repondu", outcome: "reponse-recue")
            .End("abandon", outcome: "sans-reponse")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("mixte")));

        IReadOnlyList<ProcessWait> waits = await host.GetWaitsAsync(depart.InstanceId);
        Assert.Equal(2, waits.Count);
        Assert.Contains(waits, w => w.Kind == WaitKind.Signal);
        Assert.Contains(waits, w => w.Kind == WaitKind.Timer);
        Assert.Single(await host.GetPendingJobsAsync());

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("reponse")));

        Assert.Equal("reponse-recue", Assert.Single(reveils).Outcome);
        Assert.Empty(await host.GetPendingJobsAsync());
        Assert.Equal(0, await host.CountAsync("process_wait"));
    }

    [Fact]
    public async Task Une_attente_mixte_bascule_sur_l_echeance_si_le_signal_n_arrive_pas()
    {
        ProcessDefinition definition = ProcessDefinition.Create("mixte")
            .Start("debut", to: "attente")
            .Wait("attente", w => w
                .ForSignal("reponse")
                .After(TimeSpan.FromDays(3))
                .OnSignal("repondu")
                .OnTimer("abandon"))
            .End("repondu", outcome: "reponse-recue")
            .End("abandon", outcome: "sans-reponse")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        await host.InTransactionAsync(engine => engine.StartAsync(new ProcessStartRequest("mixte")));
        host.Clock.Advance(TimeSpan.FromDays(4));

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.FireDueTimersAsync());

        Assert.Equal("sans-reponse", Assert.Single(reveils).Outcome);
        Assert.Equal(0, await host.CountAsync("process_wait"));
    }

    [Fact]
    public async Task Un_signal_recu_deux_fois_ne_fait_avancer_l_instance_qu_une_fois()
    {
        using var host = new ProcessTestHost(new[] { AttenteSignal() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("paiement").With("reference", "CMD-42")));

        await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("paiement-recu") { CorrelationKey = "CMD-42" }));

        IReadOnlyList<ProcessExecutionResult> second = await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("paiement-recu") { CorrelationKey = "CMD-42" }));

        Assert.Empty(second);
        ProcessInstance persisted = await host.GetRequiredInstanceAsync(depart.InstanceId);
        Assert.Equal(ProcessStatus.Completed, persisted.Status);
    }

    [Fact]
    public async Task Un_signal_diffuse_reveille_toutes_les_instances_concernees()
    {
        ProcessDefinition definition = ProcessDefinition.Create("diffusion")
            .Start("debut", to: "attente")
            .Wait("attente", w => w.ForSignal("cloture").OnSignal("fin"))
            .End("fin", outcome: "cloturee")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        for (int i = 0; i < 3; i++)
        {
            await host.InTransactionAsync(engine => engine.StartAsync(new ProcessStartRequest("diffusion")));
        }

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.SignalAsync(new SignalRequest("cloture")));

        Assert.Equal(3, reveils.Count);
        Assert.All(reveils, r => Assert.Equal(ProcessStatus.Completed, r.Status));
    }

    private sealed record Paiement(string Moyen, decimal Montant);
}
