using System;

namespace MachineEtatPlus.Abstractions;

/// <summary>Source de temps du moteur, remplacable dans les tests.</summary>
public interface IProcessClock
{
    /// <summary>Instant courant.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Horloge systeme.</summary>
public sealed class SystemProcessClock : IProcessClock
{
    /// <summary>Instance partagee.</summary>
    public static readonly SystemProcessClock Instance = new SystemProcessClock();

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
