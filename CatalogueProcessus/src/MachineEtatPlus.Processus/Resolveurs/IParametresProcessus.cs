namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Paramètres métier utilisés par les résolveurs dédiés.</summary>
public interface IParametresProcessus
{
    DateOnly DateLimiteRenouvellement { get; }
}
