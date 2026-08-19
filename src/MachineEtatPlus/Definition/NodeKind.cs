namespace MachineEtatPlus.Definition;

/// <summary>Types de noeuds qu'un processus peut contenir.</summary>
public enum NodeKind
{
    /// <summary>Point d'entree du processus.</summary>
    Start = 0,

    /// <summary>Noeud metier : execute une commande de l'application cliente.</summary>
    Business = 1,

    /// <summary>Noeud decision : execute une requete de l'application cliente et aiguille selon son resultat.</summary>
    Decision = 2,

    /// <summary>Noeud d'attente : suspend l'instance jusqu'a une date/heure ou un signal.</summary>
    Wait = 3,

    /// <summary>Noeud interactif : suspend l'instance jusqu'a une action humaine.</summary>
    Interactive = 4,

    /// <summary>Noeud sous-processus : demarre un processus enfant.</summary>
    SubProcess = 5,

    /// <summary>Point de sortie du processus.</summary>
    End = 6,
}
