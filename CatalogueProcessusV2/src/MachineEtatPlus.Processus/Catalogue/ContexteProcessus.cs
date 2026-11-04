using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>
/// Informations connues par l'appelant. Chaque résolveur lit ce dont il a besoin ;
/// l'appelant n'a pas à savoir quel paramètre attend quelle catégorie.
/// </summary>
public sealed record ContexteProcessus(
    TypeFormulaire? TypeFormulaire = null,
    TypeDossier? TypeDossier = null)
{
    public static ContexteProcessus Vide { get; } = new();

    public TypeFormulaire ExigerTypeFormulaire() =>
        TypeFormulaire ?? throw new ContexteIncompletException(nameof(TypeFormulaire));

    public TypeDossier ExigerTypeDossier() =>
        TypeDossier ?? throw new ContexteIncompletException(nameof(TypeDossier));
}
