namespace SelectionProcessus;

/// <summary>Type de processus concret à exécuter.</summary>
public enum TypeProcessus
{
    // Réception d'une déclaration
    DeclarationOriginale,
    DeclarationOriginaleMassive,

    // Réception d'une correction
    CorrectionUnitaire,
    CorrectionMassive,
    CorrectionManuelle,
}
