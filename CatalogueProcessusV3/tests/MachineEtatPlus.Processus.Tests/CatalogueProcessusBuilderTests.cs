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
    public void Deux_types_par_defaut_sont_refuses()
    {
        var b = new CatalogueProcessusBuilder().Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance);
        Assert.Throws<InvalidOperationException>(() =>
            b.Ajouter(CategorieProcessus.Relance, parDefaut: TypeProcessus.Recotisation,
                (TypeFormulaire.Initial, TypeProcessus.Relance)));
    }

    [Fact]
    public void Ajout_refuse_ne_modifie_pas_la_table()
    {
        var b = new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale);
        Assert.Throws<InvalidOperationException>(() =>
            b.Ajouter(CategorieProcessus.Reception, TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise));
        Assert.Null(b.Construire().Obtenir(CategorieProcessus.Reception, TypeDossier.Entreprise));
    }

    [Fact]
    public void Defaut_et_exceptions()
    {
        var catalogue = new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception, parDefaut: TypeProcessus.ReceptionInitiale,
                (TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement))
            .Construire();

        Assert.Equal(TypeProcessus.ReceptionInitiale, catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Initial));
        Assert.Equal(TypeProcessus.ReceptionRenouvellement, catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Renouvellement));
        Assert.Equal(TypeProcessus.ReceptionInitiale, catalogue.Obtenir(CategorieProcessus.Reception));
    }

    [Fact]
    public void Defaut_avec_un_critere_d_un_autre_enum_retourne_null()
    {
        var catalogue = new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception, parDefaut: TypeProcessus.ReceptionInitiale,
                (TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement))
            .Construire();

        Assert.Null(catalogue.Obtenir(CategorieProcessus.Reception, TypeDossier.Entreprise));
    }

    [Fact]
    public void Defaut_sans_exception_fixe_quand_meme_l_enum_du_critere()
    {
        var catalogue = new CatalogueProcessusBuilder()
            .Ajouter<TypeFormulaire>(CategorieProcessus.Reception, parDefaut: TypeProcessus.ReceptionInitiale)
            .Construire();

        Assert.Equal(TypeProcessus.ReceptionInitiale, catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Renouvellement));
        Assert.Null(catalogue.Obtenir(CategorieProcessus.Reception, TypeDossier.Entreprise));
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

    [Fact]
    public void Bloc_de_correspondances_equivaut_aux_lignes_individuelles()
    {
        var catalogue = new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception,
                (TypeFormulaire.Initial,        TypeProcessus.ReceptionInitiale),
                (TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement))
            .Construire();

        Assert.Equal(TypeProcessus.ReceptionInitiale, catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Initial));
        Assert.Equal(TypeProcessus.ReceptionRenouvellement, catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Renouvellement));
    }

    [Fact]
    public void Doublon_dans_un_bloc_est_refuse()
        => Assert.Throws<InvalidOperationException>(() => new CatalogueProcessusBuilder()
            .Ajouter(CategorieProcessus.Reception,
                (TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale),
                (TypeFormulaire.Initial, TypeProcessus.ReceptionRenouvellement)));

    [Fact]
    public void Bloc_vide_est_refuse()
        => Assert.Throws<ArgumentException>(() => new CatalogueProcessusBuilder()
            .Ajouter<TypeFormulaire>(CategorieProcessus.Reception));
}
