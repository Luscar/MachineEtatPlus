using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Déclare la table du catalogue et valide sa cohérence :
/// <list type="bullet">
/// <item>pas de doublon (catégorie, critère);</item>
/// <item>une catégorie utilise un seul genre de critère, ou aucun.</item>
/// </list>
/// </summary>
public sealed class CatalogueProcessusBuilder
{
    private readonly Dictionary<(CategorieProcessus, Enum?), TypeProcessus> _types = new();
    private readonly Dictionary<CategorieProcessus, Type?> _critereParCategorie = new();

    /// <summary>Catégorie à processus unique.</summary>
    public CatalogueProcessusBuilder Ajouter(CategorieProcessus categorie, TypeProcessus type)
        => AjouterInterne(categorie, null, type);

    /// <summary>Catégorie aiguillée par une valeur d'enum.</summary>
    public CatalogueProcessusBuilder Ajouter<TCritere>(
        CategorieProcessus categorie, TCritere critere, TypeProcessus type)
        where TCritere : struct, Enum
        => AjouterInterne(categorie, critere, type);

    public CatalogueProcessus Construire() => new(new Dictionary<(CategorieProcessus, Enum?), TypeProcessus>(_types));

    private CatalogueProcessusBuilder AjouterInterne(CategorieProcessus categorie, Enum? critere, TypeProcessus type)
    {
        var typeCritere = critere?.GetType();

        if (_critereParCategorie.TryGetValue(categorie, out var existant) && existant != typeCritere)
            throw new InvalidOperationException(
                $"La catégorie {categorie} mélange les critères " +
                $"'{existant?.Name ?? "aucun"}' et '{typeCritere?.Name ?? "aucun"}'.");

        if (!_types.TryAdd((categorie, critere), type))
            throw new InvalidOperationException(
                $"Doublon : {categorie}{(critere is null ? "" : $" / {critere}")} est déjà associé à {_types[(categorie, critere)]}.");

        _critereParCategorie[categorie] = typeCritere;
        return this;
    }
}
