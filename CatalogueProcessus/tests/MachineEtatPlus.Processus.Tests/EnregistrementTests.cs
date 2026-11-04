using Autofac;
using Autofac.Features.Indexed;
using MachineEtatPlus.Processus.Configuration;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Tests;

public sealed class EnregistrementTests
{
    /// <summary>Catégories volontairement sans résolveur (aucune à ce jour).</summary>
    private static readonly HashSet<CategorieProcessus> CategoriesExclues = [];

    [Fact]
    public void Deux_resolveurs_pour_la_meme_categorie_levent_une_exception_a_l_enregistrement()
    {
        var b = new ContainerBuilder();
        b.AddResolveurUnique(CategorieProcessus.Relance, TypeProcessus.Relance);

        var ex = Assert.Throws<InvalidOperationException>(
            () => b.AddResolveurParTypeFormulaire(CategorieProcessus.Relance,
                new Dictionary<TypeFormulaire, TypeProcessus> { [TypeFormulaire.Initial] = TypeProcessus.Relance }));

        Assert.Equal("Un résolveur est déjà enregistré pour 'Relance'.", ex.Message);
    }

    [Fact]
    public void Doublon_entre_resolveur_dedie_et_resolveur_generique_est_detecte()
    {
        var b = new ContainerBuilder();
        b.AddResolveur<ResolveurReception>(CategorieProcessus.Reception);

        Assert.Throws<InvalidOperationException>(
            () => b.AddResolveurUnique(CategorieProcessus.Reception, TypeProcessus.ReceptionInitiale));
    }

    [Fact]
    public void Module_enregistre_deux_fois_leve_une_exception()
    {
        var b = new ContainerBuilder();
        b.RegisterModule<CatalogueProcessusModule>();
        b.RegisterModule(new CatalogueProcessusModule());

        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Toutes_les_categories_ont_un_resolveur()
    {
        using var conteneur = ConteneurDeTest.Construire();
        var index = conteneur.Resolve<IIndex<CategorieProcessus, IResolveurTypeProcessus>>();

        var manquantes = Enum.GetValues<CategorieProcessus>()
            .Where(c => !CategoriesExclues.Contains(c))
            .Where(c => !index.TryGetValue(c, out _))
            .ToList();

        Assert.Empty(manquantes);
    }
}
