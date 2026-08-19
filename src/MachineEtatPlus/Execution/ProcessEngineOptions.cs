using System;

namespace MachineEtatPlus.Execution;

/// <summary>Reglages du moteur.</summary>
public sealed class ProcessEngineOptions
{
    private int _maxStepsPerTransaction = 100;
    private int _maxSubProcessDepth = 10;

    /// <summary>
    /// Nombre maximum de noeuds franchis dans une meme transaction. Au-dela, le moteur s'arrete
    /// proprement et confie la suite a l'ordonnanceur externe, ce qui protege des boucles
    /// infinies et des transactions interminables. Valeur par defaut : 100.
    /// </summary>
    public int MaxStepsPerTransaction
    {
        get => _maxStepsPerTransaction;
        set => _maxStepsPerTransaction = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Le budget de pas doit etre strictement positif.");
    }

    /// <summary>Profondeur maximale d'imbrication des sous-processus. Valeur par defaut : 10.</summary>
    public int MaxSubProcessDepth
    {
        get => _maxSubProcessDepth;
        set => _maxSubProcessDepth = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "La profondeur maximale doit etre strictement positive.");
    }

    /// <summary>Journalise chaque evenement d'avancement via <c>IProcessStore.AppendHistoryAsync</c>. Actif par defaut.</summary>
    public bool RecordHistory { get; set; } = true;

    /// <summary>
    /// Demande un verrou d'ecriture au stockage avant de modifier une instance. Actif par defaut.
    /// </summary>
    public bool LockInstancesForUpdate { get; set; } = true;

    /// <summary>
    /// Comportement en cas d'exception levee par un traitement de l'application cliente.
    /// Par defaut l'exception est propagee : la transaction du client etant vraisemblablement
    /// compromise, c'est a lui de decider du <c>Rollback</c>. Passez a <c>false</c> uniquement si
    /// vous savez enregistrer l'echec (point de sauvegarde, transaction dediee...).
    /// </summary>
    public bool PropagateHandlerExceptions { get; set; } = true;
}
