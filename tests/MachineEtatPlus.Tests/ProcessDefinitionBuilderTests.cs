using System;
using System.Threading.Tasks;
using MachineEtatPlus.Definition;
using Xunit;

namespace MachineEtatPlus.Tests;

public class ProcessDefinitionBuilderTests
{
    [Fact]
    public void Une_definition_valide_expose_ses_noeuds()
    {
        ProcessDefinition definition = ProcessDefinition.Create("simple", version: 3)
            .Named("Processus simple")
            .Start("debut", to: "travail")
            .Business("travail", (_, _) => Task.CompletedTask, to: "fin")
            .End("fin", outcome: "ok")
            .Build();

        Assert.Equal("simple", definition.Key);
        Assert.Equal(3, definition.Version);
        Assert.Equal("debut", definition.StartNodeId);
        Assert.Equal(3, definition.Nodes.Count);
        Assert.Equal(NodeKind.Business, definition.GetNode("travail").Kind);
        Assert.Null(definition.FindNode("inconnu"));
    }

    [Fact]
    public void Une_cible_inexistante_est_refusee()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "travail")
                .Business("travail", (_, _) => Task.CompletedTask, to: "nulle-part")
                .End("fin")
                .Build());

        Assert.Contains("nulle-part", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void L_absence_de_noeud_de_depart_est_refusee()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse").End("fin").Build());

        Assert.Contains("noeud de depart", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deux_noeuds_de_depart_sont_refuses()
    {
        Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "fin")
                .Start("debut2", to: "fin")
                .End("fin")
                .Build());
    }

    [Fact]
    public void Un_identifiant_duplique_est_refuse()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "fin")
                .End("fin")
                .End("fin")
                .Build());

        Assert.Contains("deux fois", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_noeud_inatteignable_est_refuse()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "fin")
                .Business("orphelin", (_, _) => Task.CompletedTask, to: "fin")
                .End("fin")
                .Build());

        Assert.Contains("orphelin", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_noeud_sans_transition_sortante_est_refuse()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "impasse")
                .Business("impasse", b => b.Execute((_, _) => Task.CompletedTask))
                .End("fin")
                .Build());

        Assert.Contains("aucune transition sortante", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Une_attente_de_signal_sans_branche_de_signal_est_refusee()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "attente")
                .Wait("attente", w => w.ForSignal("truc").OnTimer("fin"))
                .End("fin")
                .Build());

        Assert.Contains("signal", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Une_attente_sans_echeance_ni_signal_est_refusee()
    {
        Assert.Throws<ArgumentException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "attente")
                .Wait("attente", w => w.To("fin"))
                .End("fin")
                .Build());
    }

    [Fact]
    public void Deux_transitions_inconditionnelles_pour_le_meme_resultat_sont_refusees()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "choix")
                .Decision("choix", d => d
                    .Evaluate(_ => "oui")
                    .When("oui", "fin")
                    .When("oui", "autre"))
                .End("fin")
                .End("autre")
                .Build());

        Assert.Contains("plusieurs transitions", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void L_absence_de_noeud_de_fin_est_refusee()
    {
        ProcessDefinitionException error = Assert.Throws<ProcessDefinitionException>(() =>
            ProcessDefinition.Create("casse")
                .Start("debut", to: "boucle")
                .Business("boucle", (_, _) => Task.CompletedTask, to: "debut")
                .Build());

        Assert.Contains("aucun noeud de fin", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Le_catalogue_retourne_la_version_la_plus_recente_par_defaut()
    {
        ProcessDefinition v1 = Simple(1);
        ProcessDefinition v2 = Simple(2);
        var registry = new ProcessDefinitionRegistry(new[] { v1, v2 });

        Assert.Equal(2, registry.Get("simple").Version);
        Assert.Equal(1, registry.Get("simple", 1).Version);
        Assert.Null(registry.Find("simple", 7));
        Assert.Throws<ProcessDefinitionException>(() => registry.Get("inconnu"));
        Assert.Equal(2, registry.All().Count);
    }

    [Fact]
    public void Le_catalogue_refuse_deux_fois_la_meme_version()
    {
        var registry = new ProcessDefinitionRegistry();
        registry.Add(Simple(1));

        Assert.Throws<ProcessDefinitionException>(() => registry.Add(Simple(1)));
    }

    private static ProcessDefinition Simple(int version) => ProcessDefinition.Create("simple", version)
        .Start("debut", to: "fin")
        .End("fin")
        .Build();
}
