using System;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Execution;

/// <summary>Demande de demarrage d'un processus.</summary>
public sealed class ProcessStartRequest
{
    /// <summary>Cree une demande de demarrage.</summary>
    /// <param name="definitionKey">Cle de la definition a demarrer.</param>
    public ProcessStartRequest(string definitionKey)
    {
        if (string.IsNullOrWhiteSpace(definitionKey))
        {
            throw new ArgumentException("La cle de definition est obligatoire.", nameof(definitionKey));
        }

        DefinitionKey = definitionKey;
    }

    /// <summary>Cle de la definition a demarrer.</summary>
    public string DefinitionKey { get; }

    /// <summary>Version demandee, ou <c>null</c> pour la plus recente.</summary>
    public int? Version { get; set; }

    /// <summary>
    /// Identifiant a attribuer a l'instance. Fournissez-le pour rendre le demarrage idempotent
    /// cote appelant ; sinon un identifiant est genere.
    /// </summary>
    public Guid? InstanceId { get; set; }

    /// <summary>Cle de correlation metier.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Variables initiales de l'instance.</summary>
    public ProcessVariables? Variables { get; set; }

    /// <summary>Alimente une variable initiale.</summary>
    /// <typeparam name="T">Type de la valeur.</typeparam>
    /// <param name="name">Nom de la variable.</param>
    /// <param name="value">Valeur.</param>
    /// <returns>Cette demande, pour chainage.</returns>
    public ProcessStartRequest With<T>(string name, T value)
    {
        Variables ??= new ProcessVariables();
        Variables.Set(name, value);
        return this;
    }
}

/// <summary>Emission d'un signal vers une ou plusieurs instances en attente.</summary>
public sealed class SignalRequest
{
    /// <summary>Cree une demande d'emission de signal.</summary>
    /// <param name="signalName">Nom du signal.</param>
    public SignalRequest(string signalName)
    {
        if (string.IsNullOrWhiteSpace(signalName))
        {
            throw new ArgumentException("Le nom du signal est obligatoire.", nameof(signalName));
        }

        SignalName = signalName;
    }

    /// <summary>Nom du signal.</summary>
    public string SignalName { get; }

    /// <summary>Instance visee, ou <c>null</c> pour cibler par cle de correlation.</summary>
    public Guid? InstanceId { get; set; }

    /// <summary>Cle de correlation permettant de retrouver les instances concernees.</summary>
    public string? CorrelationKey { get; set; }

    /// <summary>Charge utile transmise a l'instance.</summary>
    public object? Payload { get; set; }

    /// <summary>Nombre maximum d'instances reveillees par cette emission. Valeur par defaut : 50.</summary>
    public int MaxInstances { get; set; } = 50;
}

/// <summary>Fin d'une interaction humaine attendue par une instance.</summary>
public sealed class InteractionCompletionRequest
{
    /// <summary>Cree une demande de cloture d'interaction.</summary>
    /// <param name="instanceId">Instance concernee.</param>
    /// <param name="outcome">Resultat choisi par l'utilisateur, qui selectionne la transition.</param>
    public InteractionCompletionRequest(Guid instanceId, string? outcome = null)
    {
        InstanceId = instanceId;
        Outcome = outcome;
    }

    /// <summary>Instance concernee.</summary>
    public Guid InstanceId { get; }

    /// <summary>Resultat choisi par l'utilisateur.</summary>
    public string? Outcome { get; }

    /// <summary>Noeud vise, utile si plusieurs interactions sont possibles.</summary>
    public string? NodeId { get; set; }

    /// <summary>Charge utile saisie par l'utilisateur.</summary>
    public object? Payload { get; set; }
}
