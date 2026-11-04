using Autofac;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Configuration;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Tests;

/// <summary>Comportement du catalogue tel que configuré par le module, avec un vrai conteneur.</summary>
public sealed class CatalogueProcessusTests : IDisposable
{
    private readonly IContainer _conteneur;
    private readonly ICatalogueProcessus _catalogue;

    public CatalogueProcessusTests()
    {
        var b = new ContainerBuilder();
        b.RegisterInstance(Fakes.Parametres());
        b.RegisterInstance(Fakes.Horloge()).As<TimeProvider>();
        b.RegisterModule<CatalogueProcessusModule>();
        _conteneur = b.Build();
        _catalogue = _conteneur.Resolve<ICatalogueProcessus>();
    }

    public void Dispose() => _conteneur.Dispose();

    [Theory]
    [InlineData(CategorieProcessus.Relance, TypeProcessus.Relance)]
    [InlineData(CategorieProcessus.Recotisation, TypeProcessus.Recotisation)]
    public void Resolve_sans_parametre(CategorieProcessus categorie, TypeProcessus attendu)
        => Assert.Equal(attendu, _catalogue.Resolve(categorie));

    [Theory]
    [InlineData(CategorieProcessus.Amender, TypeFormulaire.Initial, TypeProcessus.AmendementInitial)]
    [InlineData(CategorieProcessus.Amender, TypeFormulaire.Renouvellement, TypeProcessus.AmendementRenouvellement)]
    [InlineData(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale)]
    [InlineData(CategorieProcessus.Reception, TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement)]
    public void Resolve_par_TypeFormulaire(CategorieProcessus categorie, TypeFormulaire tf, TypeProcessus attendu)
        => Assert.Equal(attendu, _catalogue.Resolve(categorie, tf));

    [Theory]
    [InlineData(TypeDossier.Individuel, TypeProcessus.PlanificationIndividuelle)]
    [InlineData(TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise)]
    public void Resolve_par_TypeDossier(TypeDossier td, TypeProcessus attendu)
        => Assert.Equal(attendu, _catalogue.Resolve(CategorieProcessus.PlanificationDossier, td));

    [Fact]
    public void Resolve_avec_un_contexte_complet_sert_toutes_les_categories()
    {
        // Cas « catégorie issue des données » : l'appelant passe tout ce qu'il sait.
        var contexte = new ContexteProcessus(TypeFormulaire.Initial, TypeDossier.Entreprise);

        Assert.Equal(TypeProcessus.ReceptionInitiale, _catalogue.Resolve(CategorieProcessus.Reception, contexte));
        Assert.Equal(TypeProcessus.PlanificationEntreprise, _catalogue.Resolve(CategorieProcessus.PlanificationDossier, contexte));
        Assert.Equal(TypeProcessus.Relance, _catalogue.Resolve(CategorieProcessus.Relance, contexte));
    }

    [Fact]
    public void Categorie_inconnue_leve_une_exception_explicite()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _catalogue.Resolve((CategorieProcessus)999));
        Assert.Equal("Aucun résolveur pour la catégorie '999'.", ex.Message);
    }

    [Fact]
    public void Information_requise_absente_indique_la_categorie_et_le_champ()
    {
        var ex = Assert.Throws<ContexteIncompletException>(
            () => _catalogue.Resolve(CategorieProcessus.Reception, TypeDossier.Entreprise));

        Assert.Equal("TypeFormulaire", ex.Champ);
        Assert.Equal("La catégorie 'Reception' requiert 'TypeFormulaire' dans le contexte.", ex.Message);
    }

    [Fact]
    public void Resolve_sans_parametre_sur_categorie_parametree_indique_le_champ_manquant()
    {
        var ex = Assert.Throws<ContexteIncompletException>(
            () => _catalogue.Resolve(CategorieProcessus.PlanificationDossier));

        Assert.Equal("La catégorie 'PlanificationDossier' requiert 'TypeDossier' dans le contexte.", ex.Message);
    }

    [Fact]
    public void Valeur_absente_de_la_table_leve_une_exception_explicite()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => _catalogue.Resolve(CategorieProcessus.PlanificationDossier, (TypeDossier)42));

        Assert.Equal("Aucun TypeProcessus pour TypeDossier.42.", ex.Message);
    }
}
