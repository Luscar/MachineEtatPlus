using Autofac;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Configuration;

/// <summary>Enregistre le catalogue (singleton) et sa table de correspondance.</summary>
public sealed class CatalogueProcessusModule : Module
{
    protected override void Load(ContainerBuilder builder)
        => builder.Register(_ => Definir(new CatalogueProcessusBuilder()).Construire())
                  .As<ICatalogueProcessus>()
                  .SingleInstance();

    /// <summary>La table du catalogue. Ajouter une ligne par (catégorie, critère?).</summary>
    public static CatalogueProcessusBuilder Definir(CatalogueProcessusBuilder b) => b
        // Un type par défaut + seulement les formulaires atypiques
        .Ajouter(CategorieProcessus.Reception, parDefaut: TypeProcessus.ReceptionInitiale,
            (TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement))

        .Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance)

        .Ajouter(CategorieProcessus.PlanificationDossier,
            (TypeDossier.Individuel, TypeProcessus.PlanificationIndividuelle),
            (TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise))

        // Toutes les correspondances, sans défaut : un formulaire non listé → null
        .Ajouter(CategorieProcessus.Amender,
            (TypeFormulaire.Initial,        TypeProcessus.AmendementInitial),
            (TypeFormulaire.Renouvellement, TypeProcessus.AmendementRenouvellement))

        .Ajouter(CategorieProcessus.Recotisation, TypeProcessus.Recotisation);
}
