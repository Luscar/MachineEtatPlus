using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Déclare la table du catalogue et valide sa cohérence :
/// <list type="bullet">
/// <item>pas de doublon (catégorie, critère), ni deux types par défaut pour une catégorie;</item>
/// <item>une catégorie est aiguillée par un seul enum (ex. pas <see cref="TypeFormulaire"/> et <see cref="TypeDossier"/>).</item>
/// </list>
/// </summary>
public sealed class CatalogueProcessusBuilder
{
    private readonly Dictionary<(CategorieProcessus, Enum?), TypeProcessus> _types = new();
    private readonly Dictionary<CategorieProcessus, Type> _typeCritereParCategorie = new();

    /// <summary>
    /// Type de processus de la catégorie : le seul, ou celui par défaut si des exceptions sont déclarées.
    /// </summary>
    public CatalogueProcessusBuilder Ajouter(CategorieProcessus categorie, TypeProcessus type)
        => AjouterInterne(categorie, null, type);

    /// <summary>Une correspondance critère → type pour la catégorie.</summary>
    public CatalogueProcessusBuilder Ajouter<TCritere>(
        CategorieProcessus categorie, TCritere critere, TypeProcessus type)
        where TCritere : struct, Enum
        => AjouterInterne(categorie, critere, type);

    /// <summary>
    /// Plusieurs correspondances critère → type, sans type par défaut :
    /// un critère non listé retourne <c>null</c>.
    /// <code>
    /// .Ajouter(CategorieProcessus.Reception,
    ///     (TypeFormulaire.A, TypeProcessus.DeclaOriginale),
    ///     (TypeFormulaire.B, TypeProcessus.DeclaOriginaleSimple))
    /// </code>
    /// </summary>
    public CatalogueProcessusBuilder Ajouter<TCritere>(
        CategorieProcessus categorie, params (TCritere Critere, TypeProcessus Type)[] correspondances)
        where TCritere : struct, Enum
    {
        if (correspondances.Length == 0)
            throw new ArgumentException($"Aucune correspondance fournie pour {categorie}.", nameof(correspondances));

        foreach (var (critere, type) in correspondances)
            AjouterInterne(categorie, critere, type);
        return this;
    }

    /// <summary>
    /// Un type par défaut, et seulement les critères atypiques :
    /// <code>
    /// .Ajouter(CategorieProcessus.Reception, parDefaut: TypeProcessus.DeclaOriginale,
    ///     (TypeFormulaire.B, TypeProcessus.DeclaOriginaleSimple))
    /// </code>
    /// </summary>
    public CatalogueProcessusBuilder Ajouter<TCritere>(
        CategorieProcessus categorie, TypeProcessus parDefaut,
        params (TCritere Critere, TypeProcessus Type)[] exceptions)
        where TCritere : struct, Enum
    {
        AjouterInterne(categorie, null, parDefaut);
        foreach (var (critere, type) in exceptions)
            AjouterInterne(categorie, critere, type);
        if (exceptions.Length == 0)
            AssocierTypeCritere(categorie, typeof(TCritere));
        return this;
    }

    public CatalogueProcessus Construire() => new(
        new Dictionary<(CategorieProcessus, Enum?), TypeProcessus>(_types),
        new Dictionary<CategorieProcessus, Type>(_typeCritereParCategorie));

    private CatalogueProcessusBuilder AjouterInterne(CategorieProcessus categorie, Enum? critere, TypeProcessus type)
    {
        if (critere is not null)
            AssocierTypeCritere(categorie, critere.GetType());

        if (!_types.TryAdd((categorie, critere), type))
            throw new InvalidOperationException(critere is null
                ? $"{categorie} a déjà un type par défaut : {_types[(categorie, null)]}."
                : $"Doublon : {categorie} / {critere} est déjà associé à {_types[(categorie, critere)]}.");

        return this;
    }

    private void AssocierTypeCritere(CategorieProcessus categorie, Type typeCritere)
    {
        if (_typeCritereParCategorie.TryGetValue(categorie, out var existant) && existant != typeCritere)
            throw new InvalidOperationException(
                $"La catégorie {categorie} mélange les critères '{existant.Name}' et '{typeCritere.Name}'.");
        _typeCritereParCategorie[categorie] = typeCritere;
    }
}
