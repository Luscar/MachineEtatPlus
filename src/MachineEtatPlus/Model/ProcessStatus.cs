namespace MachineEtatPlus.Model;

/// <summary>Etat de vie d'une instance de processus.</summary>
public enum ProcessStatus
{
    /// <summary>L'instance peut avancer : elle sera traitee par le moteur.</summary>
    Running = 0,

    /// <summary>L'instance est bloquee sur une attente (date/heure, signal, interaction ou sous-processus).</summary>
    Waiting = 1,

    /// <summary>L'instance a atteint un noeud de fin.</summary>
    Completed = 2,

    /// <summary>L'instance s'est arretee sur une erreur.</summary>
    Faulted = 3,

    /// <summary>L'instance a ete annulee explicitement.</summary>
    Cancelled = 4,
}
