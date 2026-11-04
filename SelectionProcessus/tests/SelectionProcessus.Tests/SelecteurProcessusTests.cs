using SelectionProcessus.Categories.ReceptionCorrection;
using SelectionProcessus.Categories.ReceptionDeclaration;
using SelectionProcessus.Categories.ReceptionPaiement;

namespace SelectionProcessus.Tests;

public class SelecteurProcessusTests
{
    private readonly SelecteurProcessus _selecteur = new(
    [
        new DeterminateurReceptionDeclaration(),
        new DeterminateurReceptionCorrection(),
        new DeterminateurReceptionPaiement(),
    ]);

    [Theory]
    [InlineData(TypeFormulaire.FormulaireIndividuel, TypeProcessus.DeclarationOriginale)]
    [InlineData(TypeFormulaire.FormulaireSimplifie, TypeProcessus.DeclarationOriginale)]
    [InlineData(TypeFormulaire.FormulaireLot, TypeProcessus.DeclarationOriginaleMassive)]
    public void ReceptionDeclaration_SelonTypeFormulaire(TypeFormulaire formulaire, TypeProcessus attendu)
    {
        var resultat = _selecteur.Determiner(
            CategorieProcessus.ReceptionDeclaration,
            new CriteresReceptionDeclaration(formulaire));

        Assert.Equal(attendu, resultat);
    }

    [Theory]
    [InlineData(1, true, TypeProcessus.CorrectionUnitaire)]
    [InlineData(5, true, TypeProcessus.CorrectionMassive)]
    [InlineData(5, false, TypeProcessus.CorrectionManuelle)]
    public void ReceptionCorrection_SelonPlusieursCriteres(int nombre, bool electronique, TypeProcessus attendu)
    {
        var resultat = _selecteur.Determiner(
            CategorieProcessus.ReceptionCorrection,
            new CriteresReceptionCorrection(nombre, electronique));

        Assert.Equal(attendu, resultat);
    }

    [Fact]
    public void ReceptionPaiement_SansCritere_RetourneToujoursLeMemeProcessus()
    {
        Assert.Equal(TypeProcessus.TraitementPaiement,
            _selecteur.Determiner(CategorieProcessus.ReceptionPaiement));
    }

    [Fact]
    public void ReceptionPaiement_IgnoreLesCriteresRecus()
    {
        Assert.Equal(TypeProcessus.TraitementPaiement, _selecteur.Determiner(
            CategorieProcessus.ReceptionPaiement,
            new CriteresReceptionDeclaration(TypeFormulaire.FormulaireLot)));
    }

    [Fact]
    public void CategorieAvecCriteres_AppeleeSansCritere_Leve()
    {
        Assert.Throws<ArgumentException>(() =>
            _selecteur.Determiner(CategorieProcessus.ReceptionDeclaration));
    }

    [Fact]
    public void CriteresIncompatiblesAvecLaCategorie_Leve()
    {
        var ex = Assert.Throws<ArgumentException>(() => _selecteur.Determiner(
            CategorieProcessus.ReceptionDeclaration,
            new CriteresReceptionCorrection(1, true)));

        Assert.Contains(nameof(CriteresReceptionDeclaration), ex.Message);
    }

    [Fact]
    public void CategorieSansDeterminateur_Leve()
    {
        var selecteur = new SelecteurProcessus([new DeterminateurReceptionDeclaration()]);

        Assert.Throws<CategorieNonSupporteeException>(() => selecteur.Determiner(
            CategorieProcessus.ReceptionCorrection,
            new CriteresReceptionCorrection(1, true)));
    }

    [Fact]
    public void AucuneRegleSatisfaite_Leve_OuEssayerRetourneFaux()
    {
        var criteres = new CriteresReceptionDeclaration((TypeFormulaire)999);

        Assert.Throws<ProcessusIntrouvableException>(() =>
            _selecteur.Determiner(CategorieProcessus.ReceptionDeclaration, criteres));
        Assert.False(_selecteur.EssayerDeterminer(CategorieProcessus.ReceptionDeclaration, criteres, out _));
    }

    [Fact]
    public void DeuxDeterminateursPourLaMemeCategorie_Leve()
    {
        Assert.Throws<InvalidOperationException>(() => new SelecteurProcessus(
        [
            new DeterminateurReceptionDeclaration(),
            new DeterminateurReceptionDeclaration(),
        ]));
    }
}
