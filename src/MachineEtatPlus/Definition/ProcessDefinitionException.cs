using System;

namespace MachineEtatPlus.Definition;

/// <summary>Signale une definition de processus invalide ou introuvable.</summary>
public class ProcessDefinitionException : Exception
{
    /// <summary>Cree l'exception avec un message.</summary>
    /// <param name="message">Message d'erreur.</param>
    public ProcessDefinitionException(string message)
        : base(message)
    {
    }

    /// <summary>Cree l'exception avec un message et une cause.</summary>
    /// <param name="message">Message d'erreur.</param>
    /// <param name="innerException">Cause.</param>
    public ProcessDefinitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
