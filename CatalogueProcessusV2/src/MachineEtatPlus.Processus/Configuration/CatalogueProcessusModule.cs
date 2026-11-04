using Autofac;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Configuration;

/// <summary>
/// Table des catégories. Ajouter une catégorie simple = une ligne.
/// <see cref="IParametresProcessus"/> doit être enregistré par l'application hôte.
/// </summary>
public sealed class CatalogueProcessusModule : Module
{
    protected override void Load(ContainerBuilder b)
    {
        // Horloge système par défaut ; un TimeProvider enregistré par l'hôte reste prioritaire.
        b.RegisterInstance(TimeProvider.System).As<TimeProvider>().PreserveExistingDefaults();

        b.AddCatalogueProcessus(c => c
            .Dedie<ResolveurReception>(CategorieProcessus.Reception)
            .Unique(CategorieProcessus.Relance, TypeProcessus.Relance)
            .ParTypeDossier(CategorieProcessus.PlanificationDossier, new Dictionary<TypeDossier, TypeProcessus>
            {
                [TypeDossier.Individuel] = TypeProcessus.PlanificationIndividuelle,
                [TypeDossier.Entreprise] = TypeProcessus.PlanificationEntreprise,
            })
            .ParTypeFormulaire(CategorieProcessus.Amender, new Dictionary<TypeFormulaire, TypeProcessus>
            {
                [TypeFormulaire.Initial]        = TypeProcessus.AmendementInitial,
                [TypeFormulaire.Renouvellement] = TypeProcessus.AmendementRenouvellement,
            })
            .Unique(CategorieProcessus.Recotisation, TypeProcessus.Recotisation));
    }
}
