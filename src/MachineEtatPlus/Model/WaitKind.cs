namespace MachineEtatPlus.Model;

/// <summary>Nature de l'attente qui bloque une instance.</summary>
public enum WaitKind
{
    /// <summary>Attente d'une echeance (date/heure).</summary>
    Timer = 0,

    /// <summary>Attente d'un signal externe.</summary>
    Signal = 1,

    /// <summary>Attente d'une interaction humaine.</summary>
    Interaction = 2,

    /// <summary>Attente de la fin d'un sous-processus.</summary>
    SubProcess = 3,
}
