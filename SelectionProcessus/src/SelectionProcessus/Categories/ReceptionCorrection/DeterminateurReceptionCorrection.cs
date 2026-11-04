namespace SelectionProcessus.Categories.ReceptionCorrection;

public sealed class DeterminateurReceptionCorrection : DeterminateurParRegles<CriteresReceptionCorrection>
{
    public const int SeuilMassif = 2;

    public override CategorieProcessus Categorie => CategorieProcessus.ReceptionCorrection;

    protected override void DefinirRegles(ConstructeurRegles regles) => regles
        .Si(c => !c.EstTransmiseElectroniquement, TypeProcessus.CorrectionManuelle)
        .Si(c => c.NombreDeclarations >= SeuilMassif, TypeProcessus.CorrectionMassive)
        .SinonParDefaut(TypeProcessus.CorrectionUnitaire);
}
