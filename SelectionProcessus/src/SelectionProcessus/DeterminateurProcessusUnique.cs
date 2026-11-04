namespace SelectionProcessus;

/// <summary>
/// Classe de base pour une catégorie qui ne contient qu'un seul type de processus :
/// le même type est toujours retourné, quels que soient les critères reçus.
/// </summary>
public abstract class DeterminateurProcessusUnique : IDeterminateurProcessus
{
    public abstract CategorieProcessus Categorie { get; }

    /// <summary>L'unique type de processus de la catégorie.</summary>
    public abstract TypeProcessus TypeProcessus { get; }

    /// <summary>Aucun critère particulier n'est requis : tout <see cref="ICriteresProcessus"/> est accepté.</summary>
    public Type TypeCriteres => typeof(ICriteresProcessus);

    public TypeProcessus Determiner(ICriteresProcessus criteres) => TypeProcessus;
}
