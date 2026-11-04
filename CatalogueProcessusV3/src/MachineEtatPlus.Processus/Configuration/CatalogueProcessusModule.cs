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
        .Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial,        TypeProcessus.ReceptionInitiale)
        .Ajouter(CategorieProcessus.Reception, TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement)

        .Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance)

        .Ajouter(CategorieProcessus.PlanificationDossier, TypeDossier.Individuel, TypeProcessus.PlanificationIndividuelle)
        .Ajouter(CategorieProcessus.PlanificationDossier, TypeDossier.Entreprise, TypeProcessus.PlanificationEntreprise)

        .Ajouter(CategorieProcessus.Amender, TypeFormulaire.Initial,        TypeProcessus.AmendementInitial)
        .Ajouter(CategorieProcessus.Amender, TypeFormulaire.Renouvellement, TypeProcessus.AmendementRenouvellement)

        .Ajouter(CategorieProcessus.Recotisation, TypeProcessus.Recotisation);
}
