namespace MachineEtatPlus.Model;

/// <summary>Types d'evenements journalises dans l'historique d'une instance.</summary>
public enum ProcessEventType
{
    /// <summary>L'instance vient d'etre creee.</summary>
    Started = 0,

    /// <summary>Un noeud a ete execute.</summary>
    NodeExecuted = 1,

    /// <summary>L'instance s'est mise en attente.</summary>
    Waiting = 2,

    /// <summary>L'instance a ete relancee depuis une attente.</summary>
    Resumed = 3,

    /// <summary>L'instance a atteint un noeud de fin.</summary>
    Completed = 4,

    /// <summary>L'instance est tombee en erreur.</summary>
    Faulted = 5,

    /// <summary>L'instance a ete annulee.</summary>
    Cancelled = 6,

    /// <summary>L'avancement a ete suspendu et confie a une tache externe.</summary>
    Deferred = 7,
}
