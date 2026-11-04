using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace MachineEtatPlus.Processus.Tests;

public sealed class ResolveurReceptionTests
{
    private static readonly DateOnly DateLimite = new(2026, 6, 30);

    private static ResolveurReception Creer(DateTimeOffset maintenant)
    {
        var horloge = new FakeTimeProvider(maintenant);
        horloge.SetLocalTimeZone(TimeZoneInfo.Utc);

        var parametres = Substitute.For<IParametresProcessus>();
        parametres.DateLimiteRenouvellement.Returns(DateLimite);

        return new ResolveurReception(horloge, parametres);
    }

    private static DateTimeOffset Le(int annee, int mois, int jour, int heure = 12) =>
        new(annee, mois, jour, heure, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Initial_donne_ReceptionInitiale_quelle_que_soit_la_date()
    {
        Assert.Equal(TypeProcessus.ReceptionInitiale, Creer(Le(2026, 1, 1)).Resoudre(TypeFormulaire.Initial));
        Assert.Equal(TypeProcessus.ReceptionInitiale, Creer(Le(2026, 12, 31)).Resoudre(TypeFormulaire.Initial));
    }

    [Fact]
    public void Renouvellement_avant_la_date_limite_donne_ReceptionRenouvellement()
        => Assert.Equal(TypeProcessus.ReceptionRenouvellement,
            Creer(Le(2026, 6, 29)).Resoudre(TypeFormulaire.Renouvellement));

    [Fact]
    public void Renouvellement_le_jour_de_la_date_limite_donne_ReceptionRenouvellement()
        => Assert.Equal(TypeProcessus.ReceptionRenouvellement,
            Creer(Le(2026, 6, 30, heure: 23)).Resoudre(TypeFormulaire.Renouvellement));

    [Fact]
    public void Renouvellement_apres_la_date_limite_donne_ReceptionRenouvellementTardif()
        => Assert.Equal(TypeProcessus.ReceptionRenouvellementTardif,
            Creer(Le(2026, 7, 1, heure: 0)).Resoudre(TypeFormulaire.Renouvellement));

    [Fact]
    public void Valeur_non_supportee_leve_ArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Creer(Le(2026, 1, 1)).Resoudre((TypeFormulaire)99));

        Assert.Equal("typeFormulaire", ex.ParamName);
    }
}
