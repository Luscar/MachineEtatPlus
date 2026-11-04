namespace SelectionProcessus;

/// <summary>Détermine le type de processus pour UNE catégorie donnée.</summary>
public interface IDeterminateurProcessus
{
    CategorieProcessus Categorie { get; }

    /// <summary>Type de critères attendu par ce déterminateur.</summary>
    Type TypeCriteres { get; }

    TypeProcessus Determiner(ICriteresProcessus criteres);
}
