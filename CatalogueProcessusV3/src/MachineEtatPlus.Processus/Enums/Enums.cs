namespace MachineEtatPlus.Processus.Enums;

public enum CategorieProcessus
{
    Reception,
    Relance,
    PlanificationDossier,
    Amender,
    Recotisation,
}

public enum TypeProcessus
{
    ReceptionInitiale,
    ReceptionRenouvellement,
    Relance,
    PlanificationIndividuelle,
    PlanificationEntreprise,
    AmendementInitial,
    AmendementRenouvellement,
    Recotisation,
}

public enum TypeFormulaire
{
    Initial,
    Renouvellement,
}

public enum TypeDossier
{
    Individuel,
    Entreprise,
}
