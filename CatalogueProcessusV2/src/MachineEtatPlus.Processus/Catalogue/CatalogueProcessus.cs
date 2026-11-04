using System.Collections.Frozen;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Catalogue sans dépendance à un conteneur : il reçoit la table complète et la valide
/// à la construction (catégories manquantes → exception immédiate, donc au démarrage).
/// </summary>
public sealed class CatalogueProcessus : ICatalogueProcessus
{
    private readonly FrozenDictionary<CategorieProcessus, IResolveurTypeProcessus> _resolveurs;

    public CatalogueProcessus(
        IReadOnlyDictionary<CategorieProcessus, IResolveurTypeProcessus> resolveurs,
        IEnumerable<CategorieProcessus>? categoriesExclues = null)
    {
        ArgumentNullException.ThrowIfNull(resolveurs);
        var exclues = categoriesExclues?.ToHashSet() ?? [];

        var manquantes = Enum.GetValues<CategorieProcessus>()
            .Where(c => !resolveurs.ContainsKey(c) && !exclues.Contains(c))
            .ToList();

        if (manquantes.Count > 0)
            throw new InvalidOperationException(
                $"Aucun résolveur pour les catégories : {string.Join(", ", manquantes)}. " +
                "Enregistrez-les ou excluez-les explicitement.");

        _resolveurs = resolveurs.ToFrozenDictionary();
    }

    public TypeProcessus Resolve(CategorieProcessus categorie, ContexteProcessus contexte)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        if (!_resolveurs.TryGetValue(categorie, out var resolveur))
            throw new InvalidOperationException($"Aucun résolveur pour la catégorie '{categorie}'.");

        try
        {
            return resolveur.Resoudre(contexte);
        }
        catch (ContexteIncompletException e)
        {
            throw new ContexteIncompletException(e.Champ,
                $"La catégorie '{categorie}' requiert '{e.Champ}' dans le contexte.", e);
        }
    }
}
