namespace SelectionProcessus;

/// <summary>
/// Point d'entrée : reçoit une catégorie et ses critères, et délègue au déterminateur
/// enregistré pour cette catégorie.
/// </summary>
public interface ISelecteurProcessus
{
    TypeProcessus Determiner(CategorieProcessus categorie, ICriteresProcessus criteres);

    /// <summary>Pour une catégorie qui n'a besoin d'aucun critère (ex. un seul processus possible).</summary>
    TypeProcessus Determiner(CategorieProcessus categorie);

    bool EssayerDeterminer(CategorieProcessus categorie, ICriteresProcessus criteres, out TypeProcessus typeProcessus);
}

public sealed class SelecteurProcessus : ISelecteurProcessus
{
    private readonly IReadOnlyDictionary<CategorieProcessus, IDeterminateurProcessus> _determinateurs;

    public SelecteurProcessus(IEnumerable<IDeterminateurProcessus> determinateurs)
    {
        ArgumentNullException.ThrowIfNull(determinateurs);

        var parCategorie = new Dictionary<CategorieProcessus, IDeterminateurProcessus>();
        foreach (var determinateur in determinateurs)
        {
            if (!parCategorie.TryAdd(determinateur.Categorie, determinateur))
            {
                throw new InvalidOperationException(
                    $"Plus d'un déterminateur est enregistré pour la catégorie {determinateur.Categorie}.");
            }
        }

        _determinateurs = parCategorie;
    }

    public TypeProcessus Determiner(CategorieProcessus categorie, ICriteresProcessus criteres)
    {
        if (!_determinateurs.TryGetValue(categorie, out var determinateur))
        {
            throw new CategorieNonSupporteeException(categorie);
        }

        return determinateur.Determiner(criteres);
    }

    public TypeProcessus Determiner(CategorieProcessus categorie) =>
        Determiner(categorie, AucunCritere.Instance);

    public bool EssayerDeterminer(CategorieProcessus categorie, ICriteresProcessus criteres, out TypeProcessus typeProcessus)
    {
        typeProcessus = default;

        if (!_determinateurs.TryGetValue(categorie, out var determinateur))
        {
            return false;
        }

        try
        {
            typeProcessus = determinateur.Determiner(criteres);
            return true;
        }
        catch (ProcessusIntrouvableException)
        {
            return false;
        }
    }
}
