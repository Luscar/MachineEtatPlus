using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Execution;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Definition;

/// <summary>Base commune des constructeurs de noeuds : libelle et arcs sortants.</summary>
/// <typeparam name="TSelf">Type concret du constructeur, pour le chainage fluide.</typeparam>
public abstract class NodeBuilder<TSelf>
    where TSelf : NodeBuilder<TSelf>
{
    /// <summary>Arcs sortants declares.</summary>
    private protected readonly List<Transition> TransitionList = new List<Transition>();

    /// <summary>Libelle lisible du noeud.</summary>
    private protected string? NodeName;

    /// <summary>Retourne l'instance concrete, pour le chainage.</summary>
    private protected TSelf Self => (TSelf)this;

    /// <summary>Donne un libelle lisible au noeud.</summary>
    /// <param name="name">Libelle.</param>
    /// <returns>Ce constructeur.</returns>
    public TSelf Named(string name)
    {
        NodeName = name;
        return Self;
    }

    /// <summary>Declare la transition par defaut, empruntee quand aucun resultat ne correspond.</summary>
    /// <param name="target">Noeud cible.</param>
    /// <param name="condition">Garde optionnelle.</param>
    /// <returns>Ce constructeur.</returns>
    public TSelf To(string target, Func<INodeExecutionContext, bool>? condition = null)
    {
        TransitionList.Add(new Transition(target, null, condition));
        return Self;
    }

    /// <summary>Declare une transition conditionnee par un resultat.</summary>
    /// <param name="outcome">Resultat declencheur.</param>
    /// <param name="target">Noeud cible.</param>
    /// <param name="condition">Garde optionnelle.</param>
    /// <returns>Ce constructeur.</returns>
    public TSelf When(string outcome, string target, Func<INodeExecutionContext, bool>? condition = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Le resultat declencheur est obligatoire.", nameof(outcome));
        }

        TransitionList.Add(new Transition(target, outcome, condition));
        return Self;
    }

    /// <summary>Construit le noeud.</summary>
    /// <param name="id">Identifiant du noeud.</param>
    /// <returns>Le noeud construit.</returns>
    private protected abstract NodeDefinition Build(string id);

    internal NodeDefinition BuildNode(string id) => Build(id);
}

/// <summary>Constructeur d'un noeud metier.</summary>
public sealed class BusinessNodeBuilder : NodeBuilder<BusinessNodeBuilder>
{
    private Func<INodeExecutionContext, object>? _commandFactory;
    private Func<INodeExecutionContext, CancellationToken, Task<string?>>? _handler;

    /// <summary>Declare la commande a dispatcher vers son <c>ICommandHandler</c>.</summary>
    /// <param name="factory">Fabrique de la commande.</param>
    /// <returns>Ce constructeur.</returns>
    public BusinessNodeBuilder Command(Func<INodeExecutionContext, object> factory)
    {
        _commandFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>Declare un traitement en ligne qui retourne le resultat d'aiguillage.</summary>
    /// <param name="handler">Traitement asynchrone.</param>
    /// <returns>Ce constructeur.</returns>
    public BusinessNodeBuilder Execute(Func<INodeExecutionContext, CancellationToken, Task<string?>> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>Declare un traitement en ligne sans resultat d'aiguillage.</summary>
    /// <param name="handler">Traitement asynchrone.</param>
    /// <returns>Ce constructeur.</returns>
    public BusinessNodeBuilder Execute(Func<INodeExecutionContext, CancellationToken, Task> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _handler = async (context, cancellationToken) =>
        {
            await handler(context, cancellationToken).ConfigureAwait(false);
            return null;
        };
        return this;
    }

    private protected override NodeDefinition Build(string id)
        => new BusinessNodeDefinition(id, NodeName, TransitionList.ToArray(), _commandFactory, _handler);
}

/// <summary>Constructeur d'un noeud de decision.</summary>
public sealed class DecisionNodeBuilder : NodeBuilder<DecisionNodeBuilder>
{
    private Func<INodeExecutionContext, object>? _queryFactory;
    private Func<INodeExecutionContext, CancellationToken, Task<string>>? _handler;

    /// <summary>Declare la requete a dispatcher vers son <c>IDecisionHandler</c>.</summary>
    /// <param name="factory">Fabrique de la requete.</param>
    /// <returns>Ce constructeur.</returns>
    public DecisionNodeBuilder Query(Func<INodeExecutionContext, object> factory)
    {
        _queryFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>Declare une evaluation en ligne.</summary>
    /// <param name="handler">Evaluation asynchrone retournant le resultat d'aiguillage.</param>
    /// <returns>Ce constructeur.</returns>
    public DecisionNodeBuilder Evaluate(Func<INodeExecutionContext, CancellationToken, Task<string>> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>Declare une evaluation synchrone en ligne.</summary>
    /// <param name="handler">Evaluation retournant le resultat d'aiguillage.</param>
    /// <returns>Ce constructeur.</returns>
    public DecisionNodeBuilder Evaluate(Func<INodeExecutionContext, string> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _handler = (context, _) => Task.FromResult(handler(context));
        return this;
    }

    private protected override NodeDefinition Build(string id)
        => new DecisionNodeDefinition(id, NodeName, TransitionList.ToArray(), _queryFactory, _handler);
}

/// <summary>Constructeur d'un noeud d'attente.</summary>
public sealed class WaitNodeBuilder : NodeBuilder<WaitNodeBuilder>
{
    private Func<INodeExecutionContext, DateTimeOffset?>? _dueAt;
    private string? _signalName;
    private Func<INodeExecutionContext, string?>? _correlationKey;
    private string? _payloadVariable;

    /// <summary>Attend jusqu'a une date/heure calculee a l'entree du noeud.</summary>
    /// <param name="dueAt">Calcul de l'echeance ; <c>null</c> desactive l'echeance a l'execution.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder Until(Func<INodeExecutionContext, DateTimeOffset?> dueAt)
    {
        _dueAt = dueAt ?? throw new ArgumentNullException(nameof(dueAt));
        return this;
    }

    /// <summary>Attend jusqu'a une date/heure fixe.</summary>
    /// <param name="dueAt">Echeance.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder Until(DateTimeOffset dueAt) => Until(_ => dueAt);

    /// <summary>Attend pendant une duree comptee a partir de l'entree dans le noeud.</summary>
    /// <param name="delay">Duree d'attente.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder After(TimeSpan delay) => Until(context => context.Now + delay);

    /// <summary>Attend la reception d'un signal.</summary>
    /// <param name="signalName">Nom du signal.</param>
    /// <param name="correlationKey">Calcul de la cle de correlation, pour cibler la bonne instance.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder ForSignal(string signalName, Func<INodeExecutionContext, string?>? correlationKey = null)
    {
        if (string.IsNullOrWhiteSpace(signalName))
        {
            throw new ArgumentException("Le nom du signal est obligatoire.", nameof(signalName));
        }

        _signalName = signalName;
        _correlationKey = correlationKey;
        return this;
    }

    /// <summary>Depose la charge utile recue dans une variable de l'instance.</summary>
    /// <param name="variableName">Nom de la variable.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder StorePayloadIn(string variableName)
    {
        _payloadVariable = variableName;
        return this;
    }

    /// <summary>Transition empruntee quand l'echeance est atteinte.</summary>
    /// <param name="target">Noeud cible.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder OnTimer(string target) => When(NodeOutcomes.Timer, target);

    /// <summary>Transition empruntee quand le signal est recu.</summary>
    /// <param name="target">Noeud cible.</param>
    /// <returns>Ce constructeur.</returns>
    public WaitNodeBuilder OnSignal(string target) => When(NodeOutcomes.Signal, target);

    private protected override NodeDefinition Build(string id)
        => new WaitNodeDefinition(id, NodeName, TransitionList.ToArray(), _dueAt, _signalName, _correlationKey, _payloadVariable);
}

/// <summary>Constructeur d'un noeud interactif.</summary>
public sealed class InteractiveNodeBuilder : NodeBuilder<InteractiveNodeBuilder>
{
    private readonly string _interactionKey;
    private Func<INodeExecutionContext, string?>? _assignee;
    private Func<INodeExecutionContext, DateTimeOffset?>? _dueAt;
    private string? _payloadVariable;

    internal InteractiveNodeBuilder(string interactionKey) => _interactionKey = interactionKey;

    /// <summary>Designe le destinataire de l'interaction (utilisateur, role, file...).</summary>
    /// <param name="assignee">Calcul du destinataire.</param>
    /// <returns>Ce constructeur.</returns>
    public InteractiveNodeBuilder AssignedTo(Func<INodeExecutionContext, string?> assignee)
    {
        _assignee = assignee ?? throw new ArgumentNullException(nameof(assignee));
        return this;
    }

    /// <summary>Designe un destinataire fixe.</summary>
    /// <param name="assignee">Destinataire.</param>
    /// <returns>Ce constructeur.</returns>
    public InteractiveNodeBuilder AssignedTo(string assignee) => AssignedTo(_ => assignee);

    /// <summary>Ajoute une echeance au-dela de laquelle l'attente bascule sur le resultat <see cref="NodeOutcomes.Timer"/>.</summary>
    /// <param name="dueAt">Calcul de l'echeance.</param>
    /// <returns>Ce constructeur.</returns>
    public InteractiveNodeBuilder Deadline(Func<INodeExecutionContext, DateTimeOffset?> dueAt)
    {
        _dueAt = dueAt ?? throw new ArgumentNullException(nameof(dueAt));
        return this;
    }

    /// <summary>Ajoute une echeance relative a l'entree dans le noeud.</summary>
    /// <param name="delay">Delai accorde.</param>
    /// <returns>Ce constructeur.</returns>
    public InteractiveNodeBuilder Deadline(TimeSpan delay) => Deadline(context => context.Now + delay);

    /// <summary>Depose la charge utile fournie par l'utilisateur dans une variable de l'instance.</summary>
    /// <param name="variableName">Nom de la variable.</param>
    /// <returns>Ce constructeur.</returns>
    public InteractiveNodeBuilder StorePayloadIn(string variableName)
    {
        _payloadVariable = variableName;
        return this;
    }

    /// <summary>Transition empruntee quand l'echeance est atteinte.</summary>
    /// <param name="target">Noeud cible.</param>
    /// <returns>Ce constructeur.</returns>
    public InteractiveNodeBuilder OnTimer(string target) => When(NodeOutcomes.Timer, target);

    private protected override NodeDefinition Build(string id)
        => new InteractiveNodeDefinition(id, NodeName, TransitionList.ToArray(), _interactionKey, _assignee, _dueAt, _payloadVariable);
}

/// <summary>Constructeur d'un noeud sous-processus.</summary>
public sealed class SubProcessNodeBuilder : NodeBuilder<SubProcessNodeBuilder>
{
    private readonly string _childDefinitionKey;
    private int? _childVersion;
    private Action<INodeExecutionContext, ProcessVariables>? _inputMapper;
    private Action<INodeExecutionContext, ProcessVariables>? _outputMapper;
    private bool _waitForCompletion = true;

    internal SubProcessNodeBuilder(string childDefinitionKey) => _childDefinitionKey = childDefinitionKey;

    /// <summary>Fige la version de la definition enfant.</summary>
    /// <param name="version">Version demandee.</param>
    /// <returns>Ce constructeur.</returns>
    public SubProcessNodeBuilder UseVersion(int version)
    {
        _childVersion = version;
        return this;
    }

    /// <summary>Alimente les variables de l'instance enfant depuis le contexte du parent.</summary>
    /// <param name="mapper">Alimentation ; le second parametre est le sac de variables de l'enfant.</param>
    /// <returns>Ce constructeur.</returns>
    public SubProcessNodeBuilder Input(Action<INodeExecutionContext, ProcessVariables> mapper)
    {
        _inputMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        return this;
    }

    /// <summary>Recupere des variables de l'instance enfant a sa fin.</summary>
    /// <param name="mapper">Recuperation ; le second parametre est le sac de variables de l'enfant.</param>
    /// <returns>Ce constructeur.</returns>
    public SubProcessNodeBuilder Output(Action<INodeExecutionContext, ProcessVariables> mapper)
    {
        _outputMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        return this;
    }

    /// <summary>Demarre l'enfant sans attendre sa fin : le parent poursuit immediatement.</summary>
    /// <returns>Ce constructeur.</returns>
    public SubProcessNodeBuilder FireAndForget()
    {
        _waitForCompletion = false;
        return this;
    }

    private protected override NodeDefinition Build(string id)
        => new SubProcessNodeDefinition(
            id,
            NodeName,
            TransitionList.ToArray(),
            _childDefinitionKey,
            _childVersion,
            _inputMapper,
            _outputMapper,
            _waitForCompletion);
}
