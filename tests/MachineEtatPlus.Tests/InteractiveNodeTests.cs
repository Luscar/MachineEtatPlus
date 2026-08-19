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

public class InteractiveNodeTests
{
    private static ProcessDefinition Validation() => ProcessDefinition.Create("conge")
        .Start("debut", to: "validation")
        .Interactive("validation", "validation-manager", i => i
            .Named("Validation par le manager")
            .AssignedTo(ctx => $"role:manager:{ctx.GetVariable<string>("service")}")
            .Deadline(TimeSpan.FromDays(5))
            .StorePayloadIn("avis")
            .When("approuve", "planifier")
            .When("refuse", "refus")
            .OnTimer("escalade"))
        .Business("planifier", (_, _) => Task.CompletedTask, to: "accepte")
        .End("accepte", outcome: "accepte")
        .End("refus", outcome: "refuse")
        .End("escalade", outcome: "escalade")
        .Build();

    [Fact]
    public async Task Le_noeud_interactif_bloque_et_expose_le_destinataire()
    {
        using var host = new ProcessTestHost(new[] { Validation() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("conge").With("service", "compta")));

        Assert.Equal(ProcessStatus.Waiting, result.Status);
        Assert.Equal(WaitKind.Interaction, result.WaitKind);

        IReadOnlyList<ProcessWait> waits = await host.GetWaitsAsync(result.InstanceId);
        ProcessWait interaction = Assert.Single(waits, w => w.Kind == WaitKind.Interaction);
        Assert.Equal("validation-manager", interaction.InteractionKey);
        Assert.Equal("role:manager:compta", interaction.Assignee);

        ProcessWait echeance = Assert.Single(waits, w => w.Kind == WaitKind.Timer);
        Assert.Equal(host.Clock.UtcNow.AddDays(5), echeance.DueAt);
    }

    [Fact]
    public async Task La_cloture_de_l_interaction_aiguille_selon_le_resultat_choisi()
    {
        using var host = new ProcessTestHost(new[] { Validation() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("conge").With("service", "compta")));

        ProcessExecutionResult fin = await host.InTransactionAsync(engine =>
            engine.CompleteInteractionAsync(new InteractionCompletionRequest(depart.InstanceId, "approuve")
            {
                Payload = new Avis("marie", "OK pour moi"),
            }));

        Assert.Equal(ProcessStatus.Completed, fin.Status);
        Assert.Equal("accepte", fin.Outcome);

        ProcessInstance persisted = await host.GetRequiredInstanceAsync(depart.InstanceId);
        Avis? avis = persisted.Variables.Get<Avis>("avis");
        Assert.NotNull(avis);
        Assert.Equal("marie", avis!.Auteur);
        Assert.Equal(0, await host.CountAsync("process_wait"));
        Assert.Empty(await host.GetPendingJobsAsync());
    }

    [Fact]
    public async Task Un_refus_emprunte_l_autre_branche()
    {
        using var host = new ProcessTestHost(new[] { Validation() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("conge").With("service", "compta")));

        ProcessExecutionResult fin = await host.InTransactionAsync(engine =>
            engine.CompleteInteractionAsync(new InteractionCompletionRequest(depart.InstanceId, "refuse")));

        Assert.Equal("refuse", fin.Outcome);
    }

    [Fact]
    public async Task L_echeance_de_l_interaction_declenche_l_escalade()
    {
        using var host = new ProcessTestHost(new[] { Validation() });

        await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("conge").With("service", "compta")));

        host.Clock.Advance(TimeSpan.FromDays(6));

        IReadOnlyList<ProcessExecutionResult> reveils = await host.InTransactionAsync(engine =>
            engine.FireDueTimersAsync());

        Assert.Equal("escalade", Assert.Single(reveils).Outcome);
    }

    [Fact]
    public async Task Cloturer_une_interaction_inexistante_est_signale_clairement()
    {
        using var host = new ProcessTestHost(new[] { Validation() });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("conge").With("service", "compta")));

        await host.InTransactionAsync(engine =>
            engine.CompleteInteractionAsync(new InteractionCompletionRequest(depart.InstanceId, "approuve")));

        ProcessEngineException error = await Assert.ThrowsAsync<ProcessEngineException>(() =>
            host.InRolledBackTransactionAsync(engine =>
                engine.CompleteInteractionAsync(new InteractionCompletionRequest(depart.InstanceId, "approuve"))));

        Assert.Contains("n'attend aucune interaction", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_resultat_inconnu_emprunte_la_transition_par_defaut()
    {
        ProcessDefinition definition = ProcessDefinition.Create("libre")
            .Start("debut", to: "saisie")
            .Interactive("saisie", "formulaire", i => i
                .When("valide", "ok")
                .To("brouillon"))
            .End("ok", outcome: "valide")
            .End("brouillon", outcome: "brouillon")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult depart = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("libre")));

        ProcessExecutionResult fin = await host.InTransactionAsync(engine =>
            engine.CompleteInteractionAsync(new InteractionCompletionRequest(depart.InstanceId, "autre-chose")));

        Assert.Equal("brouillon", fin.Outcome);
    }

    private sealed record Avis(string Auteur, string Commentaire);
}
