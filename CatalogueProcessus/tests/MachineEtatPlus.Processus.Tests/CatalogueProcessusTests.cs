using Autofac;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Tests;

public sealed class CatalogueProcessusTests : IDisposable
{
    private readonly IContainer _conteneur = ConteneurDeTest.Construire();
    private ICatalogueProcessus Catalogue => _conteneur.Resolve<ICatalogueProcessus>();

    public void Dispose() => _conteneur.Dispose();

    [Theory]
    [InlineData(CategorieProcessus.Relance, TypeProcessus.Relance)]
    [InlineData(CategorieProcessus.Recotisation, TypeProcessus.Recotisation)]
    public void Resolve_sans_parametre_retourne_le_type_unique(CategorieProcessus categorie, TypeProcessus attendu)
        => Assert.Equal(attendu, Catalogue.Resolve(categorie));

    [Theory]
    [InlineData(CategorieProcessus.Amender, TypeFormulaire.Initial, TypeProcessus.AmendementInitial)]
    [InlineData(CategorieProcessus.Amender, TypeFormulaire.Renouvellement, TypeProcessus.AmendementRenouvellement)]
    [InlineData(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale)]
    [InlineData(CategorieProcessus.Reception, TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement)]
    public void Resolve_par_TypeFormulaire(CategorieProcessus categorie, TypeFormulaire typeFormulaire, TypeProcessus attendu)
        => Assert.Equal(attendu, Catalogue.Resolve(categorie, typeFormulaire));

    [Theory]
    [InlineData(TypeDossier.Individuel, TypeProcessus.PlanificationIndividuelle)]
    [InlineData(TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise)]
    public void Resolve_par_TypeDossier(TypeDossier typeDossier, TypeProcessus attendu)
        => Assert.Equal(attendu, Catalogue.Resolve(CategorieProcessus.PlanificationDossier, typeDossier));

    [Fact]
    public void Resolve_categorie_non_enregistree_leve_une_exception_explicite()
    {
        var categorieInconnue = (CategorieProcessus)999;

        var ex = Assert.Throws<InvalidOperationException>(() => Catalogue.Resolve(categorieInconnue));

        Assert.Contains("Aucun résolveur", ex.Message);
        Assert.Contains("'999'", ex.Message);
    }

    [Fact]
    public void Resolve_avec_TypeDossier_sur_une_categorie_TypeFormulaire_indique_attendu_et_recu()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Catalogue.Resolve(CategorieProcessus.Reception, TypeDossier.Entreprise));

        Assert.Equal("La catégorie 'Reception' attend le paramètre 'TypeFormulaire', reçu 'TypeDossier'.", ex.Message);
    }

    [Fact]
    public void Resolve_avec_parametre_sur_une_categorie_sans_parametre_indique_attendu_et_recu()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Catalogue.Resolve(CategorieProcessus.Relance, TypeFormulaire.Initial));

        Assert.Equal("La catégorie 'Relance' attend le paramètre 'aucun', reçu 'TypeFormulaire'.", ex.Message);
    }

    [Fact]
    public void Resolve_sans_parametre_sur_une_categorie_parametree_indique_attendu_et_recu()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Catalogue.Resolve(CategorieProcessus.Reception));

        Assert.Equal("La catégorie 'Reception' attend le paramètre 'TypeFormulaire', reçu 'aucun'.", ex.Message);
    }

    [Fact]
    public void Resolve_valeur_absente_de_la_table_leve_une_exception_explicite()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Catalogue.Resolve(CategorieProcessus.PlanificationDossier, (TypeDossier)42));

        Assert.Equal("Aucun TypeProcessus pour TypeDossier.42.", ex.Message);
    }
}
