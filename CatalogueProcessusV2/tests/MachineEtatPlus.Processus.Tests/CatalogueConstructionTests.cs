using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Tests;

/// <summary>Le catalogue se teste sans conteneur : il ne dépend que de sa table.</summary>
public sealed class CatalogueConstructionTests
{
    private static Dictionary<CategorieProcessus, IResolveurTypeProcessus> TableComplete() =>
        Enum.GetValues<CategorieProcessus>()
            .ToDictionary(c => c, IResolveurTypeProcessus (_) => new ResolveurUnique(TypeProcessus.Relance));

    [Fact]
    public void Table_complete_est_acceptee()
        => Assert.Equal(TypeProcessus.Relance,
            new CatalogueProcessus(TableComplete()).Resolve(CategorieProcessus.Amender, ContexteProcessus.Vide));

    [Fact]
    public void Categories_manquantes_sont_toutes_listees()
    {
        var table = TableComplete();
        table.Remove(CategorieProcessus.Amender);
        table.Remove(CategorieProcessus.Recotisation);

        var ex = Assert.Throws<InvalidOperationException>(() => new CatalogueProcessus(table));

        Assert.StartsWith("Aucun résolveur pour les catégories : Amender, Recotisation.", ex.Message);
    }

    [Fact]
    public void Categorie_exclue_n_est_pas_exigee_mais_ne_se_resout_pas()
    {
        var table = TableComplete();
        table.Remove(CategorieProcessus.Amender);

        var catalogue = new CatalogueProcessus(table, [CategorieProcessus.Amender]);

        var ex = Assert.Throws<InvalidOperationException>(() => catalogue.Resolve(CategorieProcessus.Amender));
        Assert.Equal("Aucun résolveur pour la catégorie 'Amender'.", ex.Message);
    }

    [Fact]
    public void La_table_est_copiee_a_la_construction()
    {
        var table = TableComplete();
        var catalogue = new CatalogueProcessus(table);

        table[CategorieProcessus.Relance] = new ResolveurUnique(TypeProcessus.Recotisation);

        Assert.Equal(TypeProcessus.Relance, catalogue.Resolve(CategorieProcessus.Relance));
    }
}
