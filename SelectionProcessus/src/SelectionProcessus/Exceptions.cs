namespace SelectionProcessus;

public sealed class CategorieNonSupporteeException(CategorieProcessus categorie)
    : InvalidOperationException($"Aucun déterminateur n'est enregistré pour la catégorie {categorie}.")
{
    public CategorieProcessus Categorie { get; } = categorie;
}

public sealed class ProcessusIntrouvableException(CategorieProcessus categorie, ICriteresProcessus criteres)
    : InvalidOperationException($"Aucun processus ne correspond aux critères {criteres} pour la catégorie {categorie}.")
{
    public CategorieProcessus Categorie { get; } = categorie;
    public ICriteresProcessus Criteres { get; } = criteres;
}
