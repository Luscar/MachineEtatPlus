using System.Reflection;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Tests;

public sealed class CatalogueProcessusTests
{
    [Fact]
    public void Categories_sans_information()
    {
        Assert.Equal(TypeProcessus.Relance, CatalogueProcessus.Obtenir(Categories.Relance));
        Assert.Equal(TypeProcessus.Recotisation, CatalogueProcessus.Obtenir(Categories.Recotisation));
    }

    [Theory]
    [InlineData(TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale)]
    [InlineData(TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement)]
    public void Reception_selon_TypeFormulaire(TypeFormulaire info, TypeProcessus attendu)
        => Assert.Equal(attendu, CatalogueProcessus.Obtenir(Categories.Reception, info));

    [Theory]
    [InlineData(TypeFormulaire.Initial, TypeProcessus.AmendementInitial)]
    [InlineData(TypeFormulaire.Renouvellement, TypeProcessus.AmendementRenouvellement)]
    public void Amender_selon_TypeFormulaire(TypeFormulaire info, TypeProcessus attendu)
        => Assert.Equal(attendu, CatalogueProcessus.Obtenir(Categories.Amender, info));

    [Theory]
    [InlineData(TypeDossier.Individuel, TypeProcessus.PlanificationIndividuelle)]
    [InlineData(TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise)]
    public void PlanificationDossier_selon_TypeDossier(TypeDossier info, TypeProcessus attendu)
        => Assert.Equal(attendu, CatalogueProcessus.Obtenir(Categories.PlanificationDossier, info));

    [Fact]
    public void Valeur_hors_enum_leve_une_exception_explicite()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CatalogueProcessus.Obtenir(Categories.PlanificationDossier, (TypeDossier)42));

        Assert.Equal("info", ex.ParamName);
        Assert.StartsWith("Valeur '42' non prise en charge pour la catégorie 'PlanificationDossier'.", ex.Message);
    }

    [Fact]
    public void Chaque_CategorieProcessus_a_exactement_une_entree_dans_Categories()
    {
        var valeurs = typeof(Categories)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (CategorieProcessus)f.FieldType.GetProperty("Valeur")!.GetValue(f.GetValue(null))!)
            .ToList();

        Assert.Equal(valeurs.Count, valeurs.Distinct().Count());
        Assert.Equal(Enum.GetValues<CategorieProcessus>().OrderBy(c => c), valeurs.OrderBy(c => c));
    }

    [Fact]
    public void Le_nom_de_chaque_entree_correspond_a_sa_categorie()
    {
        foreach (var f in typeof(Categories).GetFields(BindingFlags.Public | BindingFlags.Static))
            Assert.Equal(f.Name, f.GetValue(null)!.ToString());
    }
}
