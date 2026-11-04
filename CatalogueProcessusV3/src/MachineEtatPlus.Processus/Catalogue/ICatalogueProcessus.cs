using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Retourne le <see cref="TypeProcessus"/> à utiliser pour une catégorie,
/// éventuellement précisée par une valeur d'enum (ex. <see cref="TypeFormulaire"/>, <see cref="TypeDossier"/>).
/// </summary>
public interface ICatalogueProcessus
{
    /// <summary>Catégorie à processus unique. <c>null</c> si aucun processus ne correspond.</summary>
    TypeProcessus? Obtenir(CategorieProcessus categorie);

    /// <summary>Catégorie aiguillée par un critère. <c>null</c> si aucun processus ne correspond.</summary>
    TypeProcessus? Obtenir<TCritere>(CategorieProcessus categorie, TCritere critere)
        where TCritere : struct, Enum;
}
