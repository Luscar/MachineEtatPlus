using Autofac;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Configuration;

/// <summary>
/// Enregistre le catalogue et un résolveur par catégorie.
/// <see cref="IParametresProcessus"/> doit être fourni par l'application hôte.
/// </summary>
public sealed class CatalogueProcessusModule : Module
{
    protected override void Load(ContainerBuilder b)
    {
        b.AddResolveur<ResolveurReception>(CategorieProcessus.Reception)
         .AddResolveurUnique(CategorieProcessus.Relance, TypeProcessus.Relance)
         .AddResolveurParTypeDossier(CategorieProcessus.PlanificationDossier, new Dictionary<TypeDossier, TypeProcessus>
         {
             [TypeDossier.Individuel] = TypeProcessus.PlanificationIndividuelle,
             [TypeDossier.Entreprise] = TypeProcessus.PlanificationEntreprise,
         })
         .AddResolveurParTypeFormulaire(CategorieProcessus.Amender, new Dictionary<TypeFormulaire, TypeProcessus>
         {
             [TypeFormulaire.Initial]        = TypeProcessus.AmendementInitial,
             [TypeFormulaire.Renouvellement] = TypeProcessus.AmendementRenouvellement,
         })
         .AddResolveurUnique(CategorieProcessus.Recotisation, TypeProcessus.Recotisation);

        // Horloge système par défaut ; un TimeProvider enregistré par l'hôte reste prioritaire.
        b.RegisterInstance(TimeProvider.System).As<TimeProvider>().PreserveExistingDefaults();

        b.RegisterType<CatalogueProcessus>().As<ICatalogueProcessus>().SingleInstance();
    }
}
