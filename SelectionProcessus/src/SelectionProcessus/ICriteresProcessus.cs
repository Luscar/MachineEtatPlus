namespace SelectionProcessus;

/// <summary>
/// Marqueur pour les informations complémentaires propres à une catégorie.
/// Chaque catégorie définit son propre type de critères (ex. le type de formulaire
/// pour la réception d'une déclaration).
/// </summary>
public interface ICriteresProcessus;

/// <summary>Critères vides, pour une catégorie qui n'a besoin d'aucune information supplémentaire.</summary>
public sealed record AucunCritere : ICriteresProcessus
{
    public static readonly AucunCritere Instance = new();
    private AucunCritere() { }
}
