using Autofac;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Configuration;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Tests;

/// <summary>Le catalogue tel que configuré par le module Autofac.</summary>
public sealed class CatalogueProcessusTests : IDisposable
{
    private readonly IContainer _conteneur;
    private readonly ICatalogueProcessus _catalogue;

    public CatalogueProcessusTests()
    {
        var b = new ContainerBuilder();
        b.RegisterModule<CatalogueProcessusModule>();
        _conteneur = b.Build();
        _catalogue = _conteneur.Resolve<ICatalogueProcessus>();
    }

    public void Dispose() => _conteneur.Dispose();

    [Theory]
    [InlineData(CategorieProcessus.Relance, TypeProcessus.Relance)]
    [InlineData(CategorieProcessus.Recotisation, TypeProcessus.Recotisation)]
    public void Categorie_seule(CategorieProcessus categorie, TypeProcessus attendu)
        => Assert.Equal(attendu, _catalogue.Obtenir(categorie));

    [Theory]
    [InlineData(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale)]
    [InlineData(CategorieProcessus.Reception, TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement)]
    [InlineData(CategorieProcessus.Amender, TypeFormulaire.Initial, TypeProcessus.AmendementInitial)]
    [InlineData(CategorieProcessus.Amender, TypeFormulaire.Renouvellement, TypeProcessus.AmendementRenouvellement)]
    public void Categorie_et_type_formulaire(CategorieProcessus categorie, TypeFormulaire formulaire, TypeProcessus attendu)
        => Assert.Equal(attendu, _catalogue.Obtenir(categorie, formulaire));

    [Theory]
    [InlineData(TypeDossier.Individuel, TypeProcessus.PlanificationIndividuelle)]
    [InlineData(TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise)]
    public void Categorie_et_type_dossier(TypeDossier dossier, TypeProcessus attendu)
        => Assert.Equal(attendu, _catalogue.Obtenir(CategorieProcessus.PlanificationDossier, dossier));

    [Fact]
    public void Critere_manquant_sans_defaut_retourne_null()
        => Assert.Null(_catalogue.Obtenir(CategorieProcessus.Amender));

    [Fact]
    public void Critere_manquant_avec_defaut_retourne_le_defaut()
        => Assert.Equal(TypeProcessus.ReceptionInitiale, _catalogue.Obtenir(CategorieProcessus.Reception));

    [Fact]
    public void Critere_superflu_est_ignore_pour_une_categorie_a_processus_unique()
        => Assert.Equal(TypeProcessus.Relance, _catalogue.Obtenir(CategorieProcessus.Relance, TypeFormulaire.Initial));

    [Fact]
    public void Mauvais_genre_de_critere_retourne_null()
        // TypeDossier.Individuel et TypeFormulaire.Initial valent tous deux 0 : ils ne doivent pas se confondre.
        => Assert.Null(_catalogue.Obtenir(CategorieProcessus.Reception, TypeDossier.Individuel));

    [Fact]
    public void Valeur_non_declaree_sans_defaut_retourne_null()
        => Assert.Null(_catalogue.Obtenir(CategorieProcessus.Amender, (TypeFormulaire)99));

    [Fact]
    public void Valeur_non_declaree_avec_defaut_retourne_le_defaut()
        => Assert.Equal(TypeProcessus.ReceptionInitiale, _catalogue.Obtenir(CategorieProcessus.Reception, (TypeFormulaire)99));

    [Fact]
    public void Catalogue_est_un_singleton()
        => Assert.Same(_catalogue, _conteneur.Resolve<ICatalogueProcessus>());

    [Fact]
    public void Chaque_categorie_est_declaree()
    {
        foreach (var categorie in Enum.GetValues<CategorieProcessus>())
        {
            var trouve = _catalogue.Obtenir(categorie) is not null
                || Enum.GetValues<TypeFormulaire>().Any(f => _catalogue.Obtenir(categorie, f) is not null)
                || Enum.GetValues<TypeDossier>().Any(d => _catalogue.Obtenir(categorie, d) is not null);
            Assert.True(trouve, $"Aucun processus déclaré pour {categorie}.");
        }
    }
}
