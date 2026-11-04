using System.Runtime.CompilerServices;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus;

/// <summary>
/// Donne le <see cref="TypeProcessus"/> à utiliser pour une catégorie.
/// <code>
/// CatalogueProcessus.Obtenir(Categories.Relance);
/// CatalogueProcessus.Obtenir(Categories.Reception, TypeFormulaire.Initial);
/// CatalogueProcessus.Obtenir(Categories.PlanificationDossier, TypeDossier.Entreprise);
/// </code>
/// </summary>
public static class CatalogueProcessus
{
    public static TypeProcessus Obtenir(Categorie categorie)
    {
        ArgumentNullException.ThrowIfNull(categorie);
        return categorie.Decider();
    }

    public static TypeProcessus Obtenir<TInfo>(Categorie<TInfo> categorie, TInfo info)
    {
        ArgumentNullException.ThrowIfNull(categorie);
        try
        {
            return categorie.Decider(info);
        }
        catch (SwitchExpressionException)
        {
            // Seulement pour une valeur hors enum (ex. cast d'un entier invalide).
            throw new ArgumentOutOfRangeException(nameof(info), info,
                $"Valeur '{info}' non prise en charge pour la catégorie '{categorie}'.");
        }
    }
}
