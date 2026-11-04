using Autofac;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Configuration;

/// <summary>
/// Déclaration de la table catégorie → résolveur. Une catégorie ne peut être déclarée
/// (ou exclue) qu'une seule fois : le doublon est rejeté immédiatement.
/// </summary>
public sealed class DefinitionCatalogueProcessus
{
    private readonly Dictionary<CategorieProcessus, Func<IComponentContext, IResolveurTypeProcessus>> _fabriques = [];
    private readonly HashSet<CategorieProcessus> _exclues = [];
    private readonly HashSet<Type> _typesDedies = [];

    internal IReadOnlyCollection<Type> TypesDedies => _typesDedies;
    internal IReadOnlyCollection<CategorieProcessus> Exclues => _exclues;
    internal bool ParScope { get; private set; }

    public DefinitionCatalogueProcessus Unique(CategorieProcessus categorie, TypeProcessus typeProcessus)
        => Instance(categorie, new ResolveurUnique(typeProcessus));

    public DefinitionCatalogueProcessus ParTypeFormulaire(CategorieProcessus categorie,
        IReadOnlyDictionary<TypeFormulaire, TypeProcessus> correspondances)
        => Instance(categorie, ResolveurParCle.ParTypeFormulaire(correspondances));

    public DefinitionCatalogueProcessus ParTypeDossier(CategorieProcessus categorie,
        IReadOnlyDictionary<TypeDossier, TypeProcessus> correspondances)
        => Instance(categorie, ResolveurParCle.ParTypeDossier(correspondances));

    /// <summary>Résolveur dédié, construit par le conteneur (dépendances injectées).</summary>
    public DefinitionCatalogueProcessus Dedie<TResolveur>(CategorieProcessus categorie)
        where TResolveur : class, IResolveurTypeProcessus
    {
        Reserver(categorie);
        _typesDedies.Add(typeof(TResolveur));
        _fabriques[categorie] = ctx => ctx.Resolve<TResolveur>();
        return this;
    }

    /// <summary>Catégorie volontairement sans résolveur (sinon le démarrage échoue).</summary>
    public DefinitionCatalogueProcessus Exclure(CategorieProcessus categorie)
    {
        Reserver(categorie);
        _exclues.Add(categorie);
        return this;
    }

    /// <summary>
    /// Catalogue et résolveurs dédiés en <c>InstancePerLifetimeScope</c>, à utiliser si un
    /// résolveur dédié dépend d'un service scoped. Par défaut : <c>SingleInstance</c>.
    /// </summary>
    public DefinitionCatalogueProcessus DureeDeVieParScope()
    {
        ParScope = true;
        return this;
    }

    internal Dictionary<CategorieProcessus, IResolveurTypeProcessus> Construire(IComponentContext ctx) =>
        _fabriques.ToDictionary(kv => kv.Key, kv => kv.Value(ctx));

    private DefinitionCatalogueProcessus Instance(CategorieProcessus categorie, IResolveurTypeProcessus resolveur)
    {
        Reserver(categorie);
        _fabriques[categorie] = _ => resolveur;
        return this;
    }

    private void Reserver(CategorieProcessus categorie)
    {
        if (_fabriques.ContainsKey(categorie) || _exclues.Contains(categorie))
            throw new InvalidOperationException($"La catégorie '{categorie}' est déjà déclarée dans le catalogue.");
    }
}
