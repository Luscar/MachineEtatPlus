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

public class EngineFlowTests
{
    private static ProcessDefinition Lineaire() => ProcessDefinition.Create("lineaire")
        .Named("Trois etapes metier")
        .Start("debut", to: "etape1")
        .Business("etape1", (ctx, _) => Increment(ctx), to: "etape2")
        .Business("etape2", (ctx, _) => Increment(ctx), to: "etape3")
        .Business("etape3", (ctx, _) => Increment(ctx), to: "fin")
        .End("fin", outcome: "termine")
        .Build();

    private static Task Increment(INodeExecutionContext context)
    {
        context.SetVariable("compteur", context.GetVariable<int>("compteur") + 1);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Le_processus_avance_jusqu_a_la_fin_dans_une_seule_transaction()
    {
        using var host = new ProcessTestHost(new[] { Lineaire() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("lineaire")));

        Assert.Equal(ProcessStatus.Completed, result.Status);
        Assert.Equal("termine", result.Outcome);
        Assert.Equal("fin", result.CurrentNodeId);
        Assert.Equal(5, result.StepsExecuted);
        Assert.False(result.Deferred);
        Assert.Null(result.WaitKind);

        ProcessInstance persisted = await host.GetRequiredInstanceAsync(result.InstanceId);
        Assert.Equal(ProcessStatus.Completed, persisted.Status);
        Assert.Equal(3, persisted.Variables.Get<int>("compteur"));
        Assert.NotNull(persisted.CompletedAt);
        Assert.Equal(0, await host.CountAsync("process_wait"));
    }

    [Fact]
    public async Task Les_variables_initiales_sont_disponibles_pour_les_noeuds()
    {
        ProcessDefinition definition = ProcessDefinition.Create("echo")
            .Start("debut", to: "copier")
            .Business("copier", (ctx, _) =>
            {
                ctx.SetVariable("sortie", $"bonjour {ctx.GetVariable<string>("nom")}");
                return Task.CompletedTask;
            }, to: "fin")
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("echo").With("nom", "Camille")));

        ProcessInstance persisted = await host.GetRequiredInstanceAsync(result.InstanceId);
        Assert.Equal("bonjour Camille", persisted.Variables.Get<string>("sortie"));
    }

    [Fact]
    public async Task Le_noeud_decision_aiguille_selon_le_resultat_de_la_query()
    {
        ProcessDefinition definition = ProcessDefinition.Create("aiguillage")
            .Start("debut", to: "montant")
            .Decision("montant", d => d
                .Evaluate(ctx => ctx.GetVariable<decimal>("montant") > 1000m ? "eleve" : "normal")
                .When("eleve", "validation")
                .When("normal", "auto"))
            .Business("validation", (_, _) => Task.CompletedTask, to: "finValidation")
            .Business("auto", (_, _) => Task.CompletedTask, to: "finAuto")
            .End("finValidation", outcome: "a-valider")
            .End("finAuto", outcome: "automatique")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult eleve = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("aiguillage").With("montant", 5000m)));
        ProcessExecutionResult normal = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("aiguillage").With("montant", 12m)));

        Assert.Equal("a-valider", eleve.Outcome);
        Assert.Equal("automatique", normal.Outcome);
    }

    [Fact]
    public async Task Une_transition_conditionnelle_est_evaluee_avant_d_etre_retenue()
    {
        ProcessDefinition definition = ProcessDefinition.Create("garde")
            .Start("debut", to: "porte")
            .Business("porte", b => b
                .Execute((_, _) => Task.CompletedTask)
                .To("vip", ctx => ctx.GetVariable<bool>("vip"))
                .To("standard"))
            .Business("vip", (_, _) => Task.CompletedTask, to: "fin")
            .Business("standard", (_, _) => Task.CompletedTask, to: "fin")
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult vip = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("garde").With("vip", true)));
        ProcessExecutionResult standard = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("garde").With("vip", false)));

        IReadOnlyList<(ProcessEventType Event, string? NodeId, string? Outcome)> historiqueVip =
            await host.GetHistoryAsync(vip.InstanceId);
        IReadOnlyList<(ProcessEventType Event, string? NodeId, string? Outcome)> historiqueStandard =
            await host.GetHistoryAsync(standard.InstanceId);

        Assert.Contains(historiqueVip, e => e.NodeId == "vip");
        Assert.Contains(historiqueStandard, e => e.NodeId == "standard");
    }

    [Fact]
    public async Task Un_noeud_de_fin_en_echec_termine_l_instance_en_erreur()
    {
        ProcessDefinition definition = ProcessDefinition.Create("refus")
            .Start("debut", to: "ko")
            .Fail("ko", outcome: "rejete")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("refus")));

        Assert.Equal(ProcessStatus.Faulted, result.Status);
        Assert.Equal("rejete", result.Outcome);
        Assert.Contains("ko", result.FaultMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Par_defaut_une_exception_metier_remonte_a_l_appelant()
    {
        ProcessDefinition definition = ProcessDefinition.Create("explosif")
            .Start("debut", to: "boum")
            .Business("boum", (_, _) => throw new InvalidOperationException("stock indisponible"), to: "fin")
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(new[] { definition });

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.InRolledBackTransactionAsync(engine => engine.StartAsync(new ProcessStartRequest("explosif"))));

        Assert.Equal("stock indisponible", error.Message);
        Assert.Equal(0, await host.CountAsync("process_instance"));
    }

    [Fact]
    public async Task En_mode_capture_l_exception_metier_marque_l_instance_en_echec()
    {
        ProcessDefinition definition = ProcessDefinition.Create("explosif")
            .Start("debut", to: "boum")
            .Business("boum", (_, _) => throw new InvalidOperationException("stock indisponible"), to: "fin")
            .End("fin")
            .Build();

        using var host = new ProcessTestHost(
            new[] { definition },
            options => options.PropagateHandlerExceptions = false);

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("explosif")));

        Assert.Equal(ProcessStatus.Faulted, result.Status);
        Assert.Equal("stock indisponible", result.FaultMessage);

        ProcessInstance persisted = await host.GetRequiredInstanceAsync(result.InstanceId);
        Assert.Contains("InvalidOperationException", persisted.FaultDetail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Le_budget_de_pas_confie_la_suite_a_l_ordonnanceur_externe()
    {
        ProcessDefinition definition = ProcessDefinition.Create("boucle")
            .Start("debut", to: "increment")
            .Business("increment", (ctx, _) => Increment(ctx), to: "test")
            .Decision("test", d => d
                .Evaluate(ctx => ctx.GetVariable<int>("compteur") >= 10 ? "assez" : "encore")
                .When("encore", "increment")
                .When("assez", "fin"))
            .End("fin", outcome: "boucle-terminee")
            .Build();

        using var host = new ProcessTestHost(
            new[] { definition },
            options => options.MaxStepsPerTransaction = 5);

        ProcessExecutionResult premier = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("boucle")));

        Assert.True(premier.Deferred);
        Assert.Equal(ProcessStatus.Running, premier.Status);
        Assert.Equal(5, premier.StepsExecuted);

        IReadOnlyList<ScheduledJob> jobs = await host.GetPendingJobsAsync();
        ScheduledJob continuation = Assert.Single(jobs);
        Assert.Equal(Abstractions.ProcessJobKind.Continuation, continuation.Kind);
        Assert.Equal(premier.InstanceId, continuation.InstanceId);
        Assert.Null(continuation.RunAt);

        ProcessExecutionResult dernier = premier;
        for (int i = 0; i < 10 && !dernier.IsFinished; i++)
        {
            dernier = await host.InTransactionAsync(engine => engine.ResumeAsync(premier.InstanceId));
        }

        Assert.Equal(ProcessStatus.Completed, dernier.Status);
        Assert.Equal("boucle-terminee", dernier.Outcome);

        ProcessInstance persisted = await host.GetRequiredInstanceAsync(premier.InstanceId);
        Assert.Equal(10, persisted.Variables.Get<int>("compteur"));
    }

    [Fact]
    public async Task Reprendre_une_instance_terminee_est_sans_effet()
    {
        using var host = new ProcessTestHost(new[] { Lineaire() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("lineaire")));

        ProcessExecutionResult reprise = await host.InTransactionAsync(engine =>
            engine.ResumeAsync(result.InstanceId));

        Assert.Equal(ProcessStatus.Completed, reprise.Status);
        Assert.Equal(0, reprise.StepsExecuted);
    }

    [Fact]
    public async Task L_identifiant_d_instance_fourni_par_l_appelant_est_respecte()
    {
        using var host = new ProcessTestHost(new[] { Lineaire() });
        var id = Guid.NewGuid();

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("lineaire") { InstanceId = id, CorrelationId = "CMD-7" }));

        Assert.Equal(id, result.InstanceId);
        ProcessInstance persisted = await host.GetRequiredInstanceAsync(id);
        Assert.Equal("CMD-7", persisted.CorrelationId);
    }

    [Fact]
    public async Task L_historique_retrace_l_avancement()
    {
        using var host = new ProcessTestHost(new[] { Lineaire() });

        ProcessExecutionResult result = await host.InTransactionAsync(engine =>
            engine.StartAsync(new ProcessStartRequest("lineaire")));

        IReadOnlyList<(ProcessEventType Event, string? NodeId, string? Outcome)> history =
            await host.GetHistoryAsync(result.InstanceId);

        Assert.Equal(ProcessEventType.Started, history[0].Event);
        Assert.Equal(ProcessEventType.Completed, history[^1].Event);
        Assert.Equal(
            new[] { "debut", "etape1", "etape2", "etape3", "fin" },
            history.Skip(1).Select(e => e.NodeId).ToArray());
    }

    [Fact]
    public async Task L_historique_peut_etre_desactive()
    {
        using var host = new ProcessTestHost(new[] { Lineaire() }, options => options.RecordHistory = false);

        await host.InTransactionAsync(engine => engine.StartAsync(new ProcessStartRequest("lineaire")));

        Assert.Equal(0, await host.CountAsync("process_history"));
    }

    [Fact]
    public async Task Sans_session_le_moteur_refuse_d_avancer()
    {
        using var host = new ProcessTestHost(new[] { Lineaire() });

        await Assert.ThrowsAsync<ProcessSessionMissingException>(() =>
            host.Engine.StartAsync(new ProcessStartRequest("lineaire")));
    }
}
