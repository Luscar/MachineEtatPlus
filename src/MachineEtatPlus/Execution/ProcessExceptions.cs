using System;

namespace MachineEtatPlus.Execution;

/// <summary>Erreur remontee par le moteur de processus.</summary>
public class ProcessEngineException : Exception
{
    /// <summary>Cree l'exception avec un message.</summary>
    /// <param name="message">Message d'erreur.</param>
    public ProcessEngineException(string message)
        : base(message)
    {
    }

    /// <summary>Cree l'exception avec un message et une cause.</summary>
    /// <param name="message">Message d'erreur.</param>
    /// <param name="innerException">Cause.</param>
    public ProcessEngineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Aucune session (connexion + transaction) n'a ete fournie par l'application cliente.</summary>
public sealed class ProcessSessionMissingException : ProcessEngineException
{
    /// <summary>Cree l'exception avec le message par defaut.</summary>
    public ProcessSessionMissingException()
        : base("Aucune session base de donnees n'est active. L'application cliente doit fournir sa connexion et sa transaction (voir IDbSessionAccessor) avant d'appeler le moteur.")
    {
    }
}

/// <summary>L'instance visee est introuvable.</summary>
public sealed class ProcessInstanceNotFoundException : ProcessEngineException
{
    /// <summary>Cree l'exception pour une instance donnee.</summary>
    /// <param name="instanceId">Identifiant recherche.</param>
    public ProcessInstanceNotFoundException(Guid instanceId)
        : base($"L'instance de processus '{instanceId}' est introuvable.")
        => InstanceId = instanceId;

    /// <summary>Identifiant recherche.</summary>
    public Guid InstanceId { get; }
}

/// <summary>Aucune transition ne permet de quitter un noeud pour le resultat produit.</summary>
public sealed class ProcessTransitionException : ProcessEngineException
{
    /// <summary>Cree l'exception pour un noeud et un resultat donnes.</summary>
    /// <param name="definitionKey">Cle de la definition.</param>
    /// <param name="nodeId">Noeud concerne.</param>
    /// <param name="outcome">Resultat produit.</param>
    public ProcessTransitionException(string definitionKey, string nodeId, string? outcome)
        : base($"Le noeud '{nodeId}' du processus '{definitionKey}' n'a aucune transition pour le resultat '{outcome ?? "(par defaut)"}'.")
    {
        DefinitionKey = definitionKey;
        NodeId = nodeId;
        Outcome = outcome;
    }

    /// <summary>Cle de la definition.</summary>
    public string DefinitionKey { get; }

    /// <summary>Noeud concerne.</summary>
    public string NodeId { get; }

    /// <summary>Resultat produit.</summary>
    public string? Outcome { get; }
}

/// <summary>Aucun handler n'est enregistre pour une commande ou une requete.</summary>
public sealed class ProcessHandlerNotFoundException : ProcessEngineException
{
    /// <summary>Cree l'exception pour un type de message donne.</summary>
    /// <param name="messageType">Type de la commande ou de la requete.</param>
    /// <param name="handlerType">Type de handler recherche.</param>
    public ProcessHandlerNotFoundException(Type messageType, Type handlerType)
        : base($"Aucun '{handlerType}' n'est enregistre pour '{messageType}'.")
    {
        MessageType = messageType;
        HandlerType = handlerType;
    }

    /// <summary>Type de la commande ou de la requete.</summary>
    public Type MessageType { get; }

    /// <summary>Type de handler recherche.</summary>
    public Type HandlerType { get; }
}
