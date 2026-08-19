namespace MachineEtatPlus.Definition;

/// <summary>Resultats normalises produits par le moteur lui-meme.</summary>
public static class NodeOutcomes
{
    /// <summary>Une attente s'est debloquee sur son echeance.</summary>
    public const string Timer = "timer";

    /// <summary>Une attente s'est debloquee sur reception de son signal.</summary>
    public const string Signal = "signal";

    /// <summary>Un sous-processus s'est termine normalement sans resultat explicite.</summary>
    public const string Completed = "completed";

    /// <summary>Un sous-processus s'est termine en erreur.</summary>
    public const string Faulted = "faulted";

    /// <summary>Un sous-processus a ete annule.</summary>
    public const string Cancelled = "cancelled";
}
