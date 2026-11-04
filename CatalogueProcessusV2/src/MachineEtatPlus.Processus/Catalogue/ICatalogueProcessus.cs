using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

public interface ICatalogueProcessus
{
    TypeProcessus Resolve(CategorieProcessus categorie, ContexteProcessus contexte);
}

/// <summary>Raccourcis typés : seuls les paramètres connus sont acceptés à la compilation.</summary>
public static class CatalogueProcessusExtensions
{
    public static TypeProcessus Resolve(this ICatalogueProcessus catalogue, CategorieProcessus categorie)
        => catalogue.Resolve(categorie, ContexteProcessus.Vide);

    public static TypeProcessus Resolve(this ICatalogueProcessus catalogue, CategorieProcessus categorie,
        TypeFormulaire typeFormulaire)
        => catalogue.Resolve(categorie, new ContexteProcessus(TypeFormulaire: typeFormulaire));

    public static TypeProcessus Resolve(this ICatalogueProcessus catalogue, CategorieProcessus categorie,
        TypeDossier typeDossier)
        => catalogue.Resolve(categorie, new ContexteProcessus(TypeDossier: typeDossier));
}
