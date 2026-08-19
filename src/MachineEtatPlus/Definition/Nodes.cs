using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Definition;

/// <summary>Point d'entree du processus.</summary>
public sealed class StartNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud de depart.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="transitions">Arcs sortants.</param>
    public StartNodeDefinition(string id, string? name, IReadOnlyList<Transition> transitions)
        : base(id, name, transitions)
    {
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.Start;
}

/// <summary>Point de sortie du processus.</summary>
public sealed class EndNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud de fin.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="outcome">Resultat final publie par l'instance, ou <c>null</c>.</param>
    /// <param name="isFailure">
    /// <c>true</c> pour terminer l'instance en echec plutot qu'en succes.
    /// </param>
    public EndNodeDefinition(string id, string? name, string? outcome, bool isFailure)
        : base(id, name, Array.Empty<Transition>())
    {
        Outcome = outcome;
        IsFailure = isFailure;
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.End;

    /// <summary>Resultat final publie par l'instance.</summary>
    public string? Outcome { get; }

    /// <summary>Indique que ce noeud termine l'instance en echec.</summary>
    public bool IsFailure { get; }
}

/// <summary>
/// Noeud metier : execute une commande de l'application cliente, soit par un delegue en ligne,
/// soit par un <see cref="Abstractions.ICommandHandler{TCommand}"/> resolu depuis le conteneur.
/// </summary>
public sealed class BusinessNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud metier.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="transitions">Arcs sortants.</param>
    /// <param name="commandFactory">Fabrique de la commande a dispatcher.</param>
    /// <param name="handler">Traitement en ligne, alternative a la fabrique.</param>
    public BusinessNodeDefinition(
        string id,
        string? name,
        IReadOnlyList<Transition> transitions,
        Func<INodeExecutionContext, object>? commandFactory,
        Func<INodeExecutionContext, CancellationToken, Task<string?>>? handler)
        : base(id, name, transitions)
    {
        if (commandFactory is null && handler is null)
        {
            throw new ArgumentException($"Le noeud metier '{id}' doit definir une commande ou un traitement en ligne.", nameof(commandFactory));
        }

        CommandFactory = commandFactory;
        Handler = handler;
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.Business;

    /// <summary>Fabrique de la commande a dispatcher.</summary>
    public Func<INodeExecutionContext, object>? CommandFactory { get; }

    /// <summary>Traitement en ligne.</summary>
    public Func<INodeExecutionContext, CancellationToken, Task<string?>>? Handler { get; }
}

/// <summary>
/// Noeud decision : execute une requete de l'application cliente et aiguille selon son resultat.
/// </summary>
public sealed class DecisionNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud de decision.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="transitions">Arcs sortants.</param>
    /// <param name="queryFactory">Fabrique de la requete a dispatcher.</param>
    /// <param name="handler">Evaluation en ligne, alternative a la fabrique.</param>
    public DecisionNodeDefinition(
        string id,
        string? name,
        IReadOnlyList<Transition> transitions,
        Func<INodeExecutionContext, object>? queryFactory,
        Func<INodeExecutionContext, CancellationToken, Task<string>>? handler)
        : base(id, name, transitions)
    {
        if (queryFactory is null && handler is null)
        {
            throw new ArgumentException($"Le noeud decision '{id}' doit definir une requete ou une evaluation en ligne.", nameof(queryFactory));
        }

        QueryFactory = queryFactory;
        Handler = handler;
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.Decision;

    /// <summary>Fabrique de la requete a dispatcher.</summary>
    public Func<INodeExecutionContext, object>? QueryFactory { get; }

    /// <summary>Evaluation en ligne.</summary>
    public Func<INodeExecutionContext, CancellationToken, Task<string>>? Handler { get; }
}

/// <summary>
/// Noeud d'attente : suspend l'instance jusqu'a une echeance, un signal, ou le premier des deux.
/// </summary>
public sealed class WaitNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud d'attente.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="transitions">Arcs sortants.</param>
    /// <param name="dueAt">Calcul de l'echeance, ou <c>null</c>.</param>
    /// <param name="signalName">Nom du signal attendu, ou <c>null</c>.</param>
    /// <param name="correlationKey">Calcul de la cle de correlation du signal.</param>
    /// <param name="payloadVariable">Variable dans laquelle deposer la charge utile recue.</param>
    public WaitNodeDefinition(
        string id,
        string? name,
        IReadOnlyList<Transition> transitions,
        Func<INodeExecutionContext, DateTimeOffset?>? dueAt,
        string? signalName,
        Func<INodeExecutionContext, string?>? correlationKey,
        string? payloadVariable)
        : base(id, name, transitions)
    {
        if (dueAt is null && signalName is null)
        {
            throw new ArgumentException($"Le noeud d'attente '{id}' doit definir une echeance ou un signal.", nameof(dueAt));
        }

        DueAt = dueAt;
        SignalName = signalName;
        CorrelationKey = correlationKey;
        PayloadVariable = payloadVariable;
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.Wait;

    /// <inheritdoc />
    public override bool IsBlocking => true;

    /// <summary>Calcul de l'echeance.</summary>
    public Func<INodeExecutionContext, DateTimeOffset?>? DueAt { get; }

    /// <summary>Nom du signal attendu.</summary>
    public string? SignalName { get; }

    /// <summary>Calcul de la cle de correlation du signal.</summary>
    public Func<INodeExecutionContext, string?>? CorrelationKey { get; }

    /// <summary>Variable dans laquelle deposer la charge utile recue.</summary>
    public string? PayloadVariable { get; }
}

/// <summary>
/// Noeud interactif : suspend l'instance jusqu'a une action humaine, avec echeance facultative.
/// </summary>
public sealed class InteractiveNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud interactif.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="transitions">Arcs sortants.</param>
    /// <param name="interactionKey">Cle fonctionnelle de l'interaction (formulaire, ecran, action).</param>
    /// <param name="assignee">Calcul du destinataire de l'interaction.</param>
    /// <param name="dueAt">Calcul de l'echeance au-dela de laquelle l'attente bascule sur le resultat <see cref="NodeOutcomes.Timer"/>.</param>
    /// <param name="payloadVariable">Variable dans laquelle deposer la charge utile fournie par l'utilisateur.</param>
    public InteractiveNodeDefinition(
        string id,
        string? name,
        IReadOnlyList<Transition> transitions,
        string interactionKey,
        Func<INodeExecutionContext, string?>? assignee,
        Func<INodeExecutionContext, DateTimeOffset?>? dueAt,
        string? payloadVariable)
        : base(id, name, transitions)
    {
        if (string.IsNullOrWhiteSpace(interactionKey))
        {
            throw new ArgumentException($"Le noeud interactif '{id}' doit definir une cle d'interaction.", nameof(interactionKey));
        }

        InteractionKey = interactionKey;
        Assignee = assignee;
        DueAt = dueAt;
        PayloadVariable = payloadVariable;
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.Interactive;

    /// <inheritdoc />
    public override bool IsBlocking => true;

    /// <summary>Cle fonctionnelle de l'interaction.</summary>
    public string InteractionKey { get; }

    /// <summary>Calcul du destinataire.</summary>
    public Func<INodeExecutionContext, string?>? Assignee { get; }

    /// <summary>Calcul de l'echeance.</summary>
    public Func<INodeExecutionContext, DateTimeOffset?>? DueAt { get; }

    /// <summary>Variable dans laquelle deposer la charge utile fournie.</summary>
    public string? PayloadVariable { get; }
}

/// <summary>Noeud sous-processus : demarre une instance enfant.</summary>
public sealed class SubProcessNodeDefinition : NodeDefinition
{
    /// <summary>Cree un noeud sous-processus.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <param name="name">Libelle lisible.</param>
    /// <param name="transitions">Arcs sortants.</param>
    /// <param name="childDefinitionKey">Cle de la definition enfant.</param>
    /// <param name="childVersion">Version de la definition enfant, ou <c>null</c> pour la plus recente.</param>
    /// <param name="inputMapper">Alimentation des variables de l'enfant.</param>
    /// <param name="outputMapper">Recuperation des variables de l'enfant a sa fin.</param>
    /// <param name="waitForCompletion">
    /// <c>true</c> pour attendre la fin de l'enfant, <c>false</c> pour poursuivre immediatement.
    /// </param>
    public SubProcessNodeDefinition(
        string id,
        string? name,
        IReadOnlyList<Transition> transitions,
        string childDefinitionKey,
        int? childVersion,
        Action<INodeExecutionContext, ProcessVariables>? inputMapper,
        Action<INodeExecutionContext, ProcessVariables>? outputMapper,
        bool waitForCompletion)
        : base(id, name, transitions)
    {
        if (string.IsNullOrWhiteSpace(childDefinitionKey))
        {
            throw new ArgumentException($"Le noeud sous-processus '{id}' doit designer une definition enfant.", nameof(childDefinitionKey));
        }

        ChildDefinitionKey = childDefinitionKey;
        ChildVersion = childVersion;
        InputMapper = inputMapper;
        OutputMapper = outputMapper;
        WaitForCompletion = waitForCompletion;
    }

    /// <inheritdoc />
    public override NodeKind Kind => NodeKind.SubProcess;

    /// <inheritdoc />
    public override bool IsBlocking => WaitForCompletion;

    /// <summary>Cle de la definition enfant.</summary>
    public string ChildDefinitionKey { get; }

    /// <summary>Version de la definition enfant.</summary>
    public int? ChildVersion { get; }

    /// <summary>Alimentation des variables de l'enfant.</summary>
    public Action<INodeExecutionContext, ProcessVariables>? InputMapper { get; }

    /// <summary>Recuperation des variables de l'enfant.</summary>
    public Action<INodeExecutionContext, ProcessVariables>? OutputMapper { get; }

    /// <summary>Indique si le parent attend la fin de l'enfant.</summary>
    public bool WaitForCompletion { get; }
}
