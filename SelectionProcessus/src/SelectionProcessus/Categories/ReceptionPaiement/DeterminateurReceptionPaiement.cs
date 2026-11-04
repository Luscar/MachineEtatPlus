namespace SelectionProcessus.Categories.ReceptionPaiement;

/// <summary>La réception d'un paiement n'a qu'un seul processus possible.</summary>
public sealed class DeterminateurReceptionPaiement : DeterminateurProcessusUnique
{
    public override CategorieProcessus Categorie => CategorieProcessus.ReceptionPaiement;

    public override TypeProcessus TypeProcessus => TypeProcessus.TraitementPaiement;
}
