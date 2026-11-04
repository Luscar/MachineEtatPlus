namespace SelectionProcessus.Categories.ReceptionCorrection;

/// <summary>
/// Exemple d'une catégorie qui a besoin d'informations différentes (et plus nombreuses)
/// que la réception d'une déclaration.
/// </summary>
public sealed record CriteresReceptionCorrection(int NombreDeclarations, bool EstTransmiseElectroniquement)
    : ICriteresProcessus;
