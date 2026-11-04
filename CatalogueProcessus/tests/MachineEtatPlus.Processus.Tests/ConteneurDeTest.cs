using Autofac;
using MachineEtatPlus.Processus.Configuration;
using MachineEtatPlus.Processus.Resolveurs;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace MachineEtatPlus.Processus.Tests;

/// <summary>Construit un vrai conteneur Autofac avec le module du catalogue.</summary>
internal static class ConteneurDeTest
{
    public static readonly DateOnly DateLimite = new(2026, 6, 30);

    public static IContainer Construire(DateTimeOffset? maintenant = null)
    {
        var parametres = Substitute.For<IParametresProcessus>();
        parametres.DateLimiteRenouvellement.Returns(DateLimite);

        var horloge = new FakeTimeProvider(maintenant ?? new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        horloge.SetLocalTimeZone(TimeZoneInfo.Utc);

        var b = new ContainerBuilder();
        b.RegisterInstance(parametres).As<IParametresProcessus>();
        b.RegisterInstance(horloge).As<TimeProvider>();
        b.RegisterModule<CatalogueProcessusModule>();
        return b.Build();
    }
}
