using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>Point d'entrée unique pour résoudre le <see cref="TypeProcessus"/> d'une catégorie.</summary>
public interface ICatalogueProcessus
{
    TypeProcessus Resolve(CategorieProcessus categorie);

    TypeProcessus Resolve<TParam>(CategorieProcessus categorie, TParam parametre);
}
