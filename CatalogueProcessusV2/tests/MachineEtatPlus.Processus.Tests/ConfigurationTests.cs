using Autofac;
using Autofac.Core;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Configuration;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Tests;

public sealed class ConfigurationTests
{
    /// <summary>Toutes les catégories sauf celles indiquées, en résolveur unique.</summary>
    private static Action<DefinitionCatalogueProcessus> ToutesSauf(params CategorieProcessus[] sauf) => d =>
    {
        foreach (var c in Enum.GetValues<CategorieProcessus>().Except(sauf))
            d.Unique(c, TypeProcessus.Relance);
    };

    private static Exception Racine(Exception e)
    {
        while (e.InnerException is not null) e = e.InnerException;
        return e;
    }

    [Fact]
    public void Le_module_couvre_toutes_les_categories_et_le_conteneur_demarre()
    {
        // Le catalogue est auto-activé : Build() échouerait si une catégorie manquait.
        var b = new ContainerBuilder();
        b.RegisterInstance(Fakes.Parametres());
        b.RegisterModule<CatalogueProcessusModule>();

        using var conteneur = b.Build();

        Assert.NotNull(conteneur.Resolve<ICatalogueProcessus>());
    }

    [Fact]
    public void Dependance_manquante_d_un_resolveur_dedie_fait_echouer_le_demarrage()
    {
        var b = new ContainerBuilder();
        b.RegisterModule<CatalogueProcessusModule>(); // IParametresProcessus non enregistré

        var ex = Assert.ThrowsAny<DependencyResolutionException>(() => b.Build());
        Assert.Contains(nameof(IParametresProcessus), ex.ToString());
    }

    [Fact]
    public void Categorie_manquante_fait_echouer_le_demarrage()
    {
        var b = new ContainerBuilder();
        b.AddCatalogueProcessus(ToutesSauf(CategorieProcessus.Recotisation));

        var ex = Assert.ThrowsAny<Exception>(() => b.Build());

        Assert.StartsWith("Aucun résolveur pour les catégories : Recotisation.", Racine(ex).Message);
    }

    [Fact]
    public void Categorie_explicitement_exclue_n_empeche_pas_le_demarrage()
    {
        var b = new ContainerBuilder();
        b.AddCatalogueProcessus(d => ToutesSauf(CategorieProcessus.Recotisation)(d.Exclure(CategorieProcessus.Recotisation)));

        using var conteneur = b.Build();

        Assert.Throws<InvalidOperationException>(
            () => conteneur.Resolve<ICatalogueProcessus>().Resolve(CategorieProcessus.Recotisation));
    }

    [Fact]
    public void Doublon_est_rejete_a_la_declaration()
    {
        var b = new ContainerBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() => b.AddCatalogueProcessus(d => d
            .Unique(CategorieProcessus.Relance, TypeProcessus.Relance)
            .ParTypeFormulaire(CategorieProcessus.Relance,
                new Dictionary<TypeFormulaire, TypeProcessus> { [TypeFormulaire.Initial] = TypeProcessus.Relance })));

        Assert.Equal("La catégorie 'Relance' est déjà déclarée dans le catalogue.", ex.Message);
    }

    [Fact]
    public void Doublon_entre_declaration_et_exclusion_est_rejete()
        => Assert.Throws<InvalidOperationException>(() => new ContainerBuilder().AddCatalogueProcessus(d => d
            .Dedie<ResolveurReception>(CategorieProcessus.Reception)
            .Exclure(CategorieProcessus.Reception)));

    [Fact]
    public void Par_defaut_le_catalogue_est_un_singleton()
    {
        var b = new ContainerBuilder();
        b.AddCatalogueProcessus(ToutesSauf());
        using var conteneur = b.Build();
        using var scope = conteneur.BeginLifetimeScope();

        Assert.Same(conteneur.Resolve<ICatalogueProcessus>(), scope.Resolve<ICatalogueProcessus>());
    }

    [Fact]
    public void Duree_de_vie_par_scope_donne_un_catalogue_par_scope()
    {
        var b = new ContainerBuilder();
        b.AddCatalogueProcessus(d => ToutesSauf()(d.DureeDeVieParScope()));
        using var conteneur = b.Build();
        using var scope1 = conteneur.BeginLifetimeScope();
        using var scope2 = conteneur.BeginLifetimeScope();

        var c1 = scope1.Resolve<ICatalogueProcessus>();
        Assert.Same(c1, scope1.Resolve<ICatalogueProcessus>());
        Assert.NotSame(c1, scope2.Resolve<ICatalogueProcessus>());
    }
}
