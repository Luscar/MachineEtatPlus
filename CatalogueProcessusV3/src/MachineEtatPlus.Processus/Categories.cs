using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus;

/// <summary>
/// La table complète : une entrée par catégorie, avec l'information qu'elle exige et sa règle.
/// Les règles sont des switch sans branche par défaut : ajouter une valeur à TypeFormulaire ou
/// TypeDossier sans la traiter ici est une erreur de compilation (CS8509).
/// </summary>
public static class Categories
{
    public static readonly Categorie<TypeFormulaire> Reception = new(CategorieProcessus.Reception,
        typeFormulaire => typeFormulaire switch
        {
            TypeFormulaire.Initial => TypeProcessus.ReceptionInitiale,
            TypeFormulaire.Renouvellement => TypeProcessus.ReceptionRenouvellement,
        });

    public static readonly Categorie Relance = new(CategorieProcessus.Relance,
        () => TypeProcessus.Relance);

    public static readonly Categorie<TypeDossier> PlanificationDossier = new(CategorieProcessus.PlanificationDossier,
        typeDossier => typeDossier switch
        {
            TypeDossier.Individuel => TypeProcessus.PlanificationIndividuelle,
            TypeDossier.Entreprise => TypeProcessus.PlanificationEntreprise,
        });

    public static readonly Categorie<TypeFormulaire> Amender = new(CategorieProcessus.Amender,
        typeFormulaire => typeFormulaire switch
        {
            TypeFormulaire.Initial => TypeProcessus.AmendementInitial,
            TypeFormulaire.Renouvellement => TypeProcessus.AmendementRenouvellement,
        });

    public static readonly Categorie Recotisation = new(CategorieProcessus.Recotisation,
        () => TypeProcessus.Recotisation);
}
