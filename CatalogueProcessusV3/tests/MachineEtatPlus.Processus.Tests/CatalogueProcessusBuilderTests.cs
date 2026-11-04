using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Tests;

/// <summary>Validation de la table à la construction.</summary>
public sealed class CatalogueProcessusBuilderTests
{
    [Fact]
    public void Doublon_sans_critere_est_refuse()
    {
        var b = new CatalogueProcessusBuilder().Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance);
        Assert.Throws<InvalidOperationException>(() => b.Ajouter(CategorieProcessus.Relance, TypeProcessus.Recotisation));
    }

    [Fact]
    public void Doublon_avec_critere_est_refuse()
    {
        var b = new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale);
        Assert.Throws<InvalidOperationException>(() =>
            b.Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionRenouvellement));
    }

    [Fact]
    public void Melange_de_genres_de_critere_est_refuse()
    {
        var b = new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale);
        Assert.Throws<InvalidOperationException>(() =>
            b.Ajouter(CategorieProcessus.Reception, TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise));
    }

    [Fact]
    public void Melange_avec_et_sans_critere_est_refuse()
    {
        var b = new CatalogueProcessusBuilder().Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance);
        Assert.Throws<InvalidOperationException>(() =>
            b.Ajouter(CategorieProcessus.Relance, TypeFormulaire.Initial, TypeProcessus.Relance));
    }

    [Fact]
    public void Ajout_refuse_ne_modifie_pas_la_table()
    {
        var b = new CatalogueProcessusBuilder().Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance);
        Assert.Throws<InvalidOperationException>(() =>
            b.Ajouter(CategorieProcessus.Relance, TypeFormulaire.Initial, TypeProcessus.Recotisation));
        Assert.Null(b.Construire().Obtenir(CategorieProcessus.Relance, TypeFormulaire.Initial));
    }

    [Fact]
    public void Catalogue_construit_est_independant_du_builder()
    {
        var b = new CatalogueProcessusBuilder();
        var catalogue = b.Construire();
        b.Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance);
        Assert.Null(catalogue.Obtenir(CategorieProcessus.Relance));
    }

    [Fact]
    public void Catalogue_vide_retourne_null()
        => Assert.Null(new CatalogueProcessusBuilder().Construire().Obtenir(CategorieProcessus.Relance));
}
