using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Table de correspondance (catégorie, critère?) → type de processus.
/// <para>
/// Recherche : d'abord (catégorie, critère), sinon le type par défaut de la catégorie (catégorie, null).
/// Un critère d'un autre enum que celui déclaré pour la catégorie retourne <c>null</c>
/// (ex. un <see cref="TypeDossier"/> passé à une catégorie aiguillée par <see cref="TypeFormulaire"/>).
/// </para>
/// Construire via <see cref="CatalogueProcessusBuilder"/>.
/// </summary>
public sealed class CatalogueProcessus : ICatalogueProcessus
{
    private readonly IReadOnlyDictionary<(CategorieProcessus, Enum?), TypeProcessus> _types;
    private readonly IReadOnlyDictionary<CategorieProcessus, Type> _typeCritereParCategorie;

    internal CatalogueProcessus(
        IReadOnlyDictionary<(CategorieProcessus, Enum?), TypeProcessus> types,
        IReadOnlyDictionary<CategorieProcessus, Type> typeCritereParCategorie)
    {
        _types = types;
        _typeCritereParCategorie = typeCritereParCategorie;
    }

    public TypeProcessus? Obtenir(CategorieProcessus categorie) => ParDefaut(categorie);

    public TypeProcessus? Obtenir<TCritere>(CategorieProcessus categorie, TCritere critere)
        where TCritere : struct, Enum
    {
        if (_typeCritereParCategorie.TryGetValue(categorie, out var attendu) && attendu != typeof(TCritere))
            return null;

        return _types.TryGetValue((categorie, critere), out var type) ? type : ParDefaut(categorie);
    }

    private TypeProcessus? ParDefaut(CategorieProcessus categorie)
        => _types.TryGetValue((categorie, null), out var type) ? type : null;
}
