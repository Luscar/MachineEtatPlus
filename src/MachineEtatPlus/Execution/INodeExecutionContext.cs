using System;
using System.Threading;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Execution;

/// <summary>
/// Contexte remis aux traitements de l'application cliente lors de l'execution d'un noeud.
/// </summary>
public interface INodeExecutionContext
{
    /// <summary>Definition en cours d'execution.</summary>
    ProcessDefinition Definition { get; }

    /// <summary>Noeud en cours d'execution.</summary>
    NodeDefinition Node { get; }

    /// <summary>Instance en cours d'execution.</summary>
    ProcessInstance Instance { get; }

    /// <summary>Variables de l'instance.</summary>
    ProcessVariables Variables { get; }

    /// <summary>
    /// Connexion et transaction fournies par l'application cliente : tout acces base effectue
    /// par un traitement metier doit passer par elles pour rester atomique avec l'avancement.
    /// </summary>
    IDbSession Session { get; }

    /// <summary>Fournisseur de services, si le moteur a ete construit avec un conteneur.</summary>
    IServiceProvider? Services { get; }

    /// <summary>Instant courant, tel que vu par l'horloge du moteur.</summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Charge utile ayant relance l'instance (signal ou interaction). Disponible pour le premier
    /// noeud execute apres la reprise, <c>null</c> ensuite.
    /// </summary>
    object? Input { get; }

    /// <summary>Jeton d'annulation du pas d'execution.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Lit une variable de l'instance.</summary>
    /// <typeparam name="T">Type attendu.</typeparam>
    /// <param name="name">Nom de la variable.</param>
    /// <returns>La valeur, ou <c>default</c> si la variable est absente.</returns>
    T? GetVariable<T>(string name);

    /// <summary>Ecrit une variable de l'instance.</summary>
    /// <typeparam name="T">Type de la valeur.</typeparam>
    /// <param name="name">Nom de la variable.</param>
    /// <param name="value">Valeur.</param>
    void SetVariable<T>(string name, T value);
}
