namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Paramètres métier utilisés par les résolveurs dédiés (fournis par l'application hôte).</summary>
public interface IParametresProcessus
{
    DateOnly DateLimiteRenouvellement { get; }
}
