using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Table de correspondance (catégorie, critère?) → type de processus.
/// Le critère est stocké comme <see cref="Enum"/> : deux valeurs d'enums différents
/// ne sont jamais égales, même avec la même valeur numérique.
/// Construire via <see cref="CatalogueProcessusBuilder"/>.
/// </summary>
public sealed class CatalogueProcessus : ICatalogueProcessus
{
    private readonly IReadOnlyDictionary<(CategorieProcessus, Enum?), TypeProcessus> _types;

    internal CatalogueProcessus(IReadOnlyDictionary<(CategorieProcessus, Enum?), TypeProcessus> types)
        => _types = types;

    public TypeProcessus? Obtenir(CategorieProcessus categorie) => Chercher(categorie, null);

    public TypeProcessus? Obtenir<TCritere>(CategorieProcessus categorie, TCritere critere)
        where TCritere : struct, Enum
        => Chercher(categorie, critere);

    private TypeProcessus? Chercher(CategorieProcessus categorie, Enum? critere)
        => _types.TryGetValue((categorie, critere), out var type) ? type : null;
}
