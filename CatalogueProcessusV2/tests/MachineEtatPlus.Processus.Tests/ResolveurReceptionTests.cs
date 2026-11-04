using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;
using static MachineEtatPlus.Processus.Tests.Fakes;

namespace MachineEtatPlus.Processus.Tests;

public sealed class ResolveurReceptionTests
{
    private static TypeProcessus Resoudre(DateTimeOffset maintenant, TypeFormulaire tf) =>
        new ResolveurReception(Horloge(maintenant), Parametres()).Resoudre(new ContexteProcessus(tf));

    [Fact]
    public void Initial_quelle_que_soit_la_date()
    {
        Assert.Equal(TypeProcessus.ReceptionInitiale, Resoudre(Le(2026, 1, 1), TypeFormulaire.Initial));
        Assert.Equal(TypeProcessus.ReceptionInitiale, Resoudre(Le(2026, 12, 31), TypeFormulaire.Initial));
    }

    [Fact]
    public void Renouvellement_avant_la_date_limite()
        => Assert.Equal(TypeProcessus.ReceptionRenouvellement, Resoudre(Le(2026, 6, 29), TypeFormulaire.Renouvellement));

    [Fact]
    public void Renouvellement_le_jour_de_la_date_limite()
        => Assert.Equal(TypeProcessus.ReceptionRenouvellement, Resoudre(Le(2026, 6, 30, 23), TypeFormulaire.Renouvellement));

    [Fact]
    public void Renouvellement_apres_la_date_limite_est_tardif()
        => Assert.Equal(TypeProcessus.ReceptionRenouvellementTardif, Resoudre(Le(2026, 7, 1, 0), TypeFormulaire.Renouvellement));

    [Fact]
    public void Valeur_non_supportee()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Resoudre(Le(2026, 1, 1), (TypeFormulaire)99));

    [Fact]
    public void TypeFormulaire_absent_du_contexte()
    {
        var resolveur = new ResolveurReception(Horloge(), Parametres());

        var ex = Assert.Throws<ContexteIncompletException>(() => resolveur.Resoudre(ContexteProcessus.Vide));
        Assert.Equal("TypeFormulaire", ex.Champ);
    }
}
