namespace SelectionProcessus.Categories.ReceptionDeclaration;

public sealed class DeterminateurReceptionDeclaration : DeterminateurParRegles<CriteresReceptionDeclaration>
{
    public override CategorieProcessus Categorie => CategorieProcessus.ReceptionDeclaration;

    protected override void DefinirRegles(ConstructeurRegles regles) => regles
        .Si(c => c.TypeFormulaire is TypeFormulaire.FormulaireIndividuel or TypeFormulaire.FormulaireSimplifie,
            TypeProcessus.DeclarationOriginale)
        .Si(c => c.TypeFormulaire == TypeFormulaire.FormulaireLot,
            TypeProcessus.DeclarationOriginaleMassive);
}
