using MachineEtatPlus.Processus.Resolveurs;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace MachineEtatPlus.Processus.Tests;

internal static class Fakes
{
    public static readonly DateOnly DateLimite = new(2026, 6, 30);

    public static DateTimeOffset Le(int annee, int mois, int jour, int heure = 12) =>
        new(annee, mois, jour, heure, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Horloge(DateTimeOffset? maintenant = null)
    {
        var horloge = new FakeTimeProvider(maintenant ?? Le(2026, 1, 15));
        horloge.SetLocalTimeZone(TimeZoneInfo.Utc);
        return horloge;
    }

    public static IParametresProcessus Parametres()
    {
        var parametres = Substitute.For<IParametresProcessus>();
        parametres.DateLimiteRenouvellement.Returns(DateLimite);
        return parametres;
    }
}
