using System;

namespace MachineEtatPlus.Model;

/// <summary>
/// Etat persistant d'une execution de processus. C'est l'agregat que l'implementation
/// <c>IProcessStore</c> cliente doit savoir inserer, relire et mettre a jour.
/// </summary>
public sealed class ProcessInstance
{
    private ProcessInstance(
        Guid id,
        string definitionKey,
        int definitionVersion,
        string currentNodeId,
        ProcessVariables variables,
        DateTimeOffset createdAt)
    {
        Id = id;
        DefinitionKey = definitionKey;
        DefinitionVersion = definitionVersion;
        CurrentNodeId = currentNodeId;
        Variables = variables;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>Identifiant de l'instance.</summary>
    public Guid Id { get; private set; }

    /// <summary>Cle de la definition de processus executee.</summary>
    public string DefinitionKey { get; private set; }

    /// <summary>Version de la definition executee.</summary>
    public int DefinitionVersion { get; private set; }

    /// <summary>Etat de vie de l'instance.</summary>
    public ProcessStatus Status { get; internal set; } = ProcessStatus.Running;

    /// <summary>Noeud sur lequel l'instance est positionnee.</summary>
    public string CurrentNodeId { get; internal set; }

    /// <summary>Variables de l'instance.</summary>
    public ProcessVariables Variables { get; private set; }

    /// <summary>Instance parente, si cette instance est un sous-processus attendu par une autre.</summary>
    public Guid? ParentInstanceId { get; internal set; }

    /// <summary>Noeud sous-processus du parent qui attend cette instance.</summary>
    public string? ParentNodeId { get; internal set; }

    /// <summary>Profondeur d'imbrication des sous-processus (0 pour une instance racine).</summary>
    public int Depth { get; internal set; }

    /// <summary>Cle de correlation metier libre (numero de commande, identifiant client...).</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Resultat final produit par le noeud de fin atteint.</summary>
    public string? Outcome { get; internal set; }

    /// <summary>Message d'erreur si l'instance est en echec.</summary>
    public string? FaultMessage { get; internal set; }

    /// <summary>Detail technique de l'erreur si l'instance est en echec.</summary>
    public string? FaultDetail { get; internal set; }

    /// <summary>Date de creation.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Date de derniere modification.</summary>
    public DateTimeOffset UpdatedAt { get; internal set; }

    /// <summary>Date de fin (succes, echec ou annulation).</summary>
    public DateTimeOffset? CompletedAt { get; internal set; }

    /// <summary>
    /// Jeton de concurrence libre a la main du stockage client (numero de version, <c>rowversion</c>, horodatage...).
    /// </summary>
    public object? ConcurrencyToken { get; set; }

    /// <summary>Variables serialisees, forme pratique pour une colonne texte.</summary>
    public string VariablesJson => Variables.ToJson();

    /// <summary>Indique si l'instance est terminee, quelle qu'en soit l'issue.</summary>
    public bool IsFinished
        => Status == ProcessStatus.Completed || Status == ProcessStatus.Faulted || Status == ProcessStatus.Cancelled;

    /// <summary>Cree une nouvelle instance a demarrer.</summary>
    /// <param name="id">Identifiant a attribuer.</param>
    /// <param name="definitionKey">Cle de la definition.</param>
    /// <param name="definitionVersion">Version de la definition.</param>
    /// <param name="startNodeId">Noeud de depart.</param>
    /// <param name="createdAt">Horodatage de creation.</param>
    /// <param name="variables">Variables initiales, ou <c>null</c>.</param>
    /// <returns>L'instance creee.</returns>
    public static ProcessInstance Create(
        Guid id,
        string definitionKey,
        int definitionVersion,
        string startNodeId,
        DateTimeOffset createdAt,
        ProcessVariables? variables = null)
        => new ProcessInstance(id, definitionKey, definitionVersion, startNodeId, variables ?? new ProcessVariables(), createdAt);

    /// <summary>
    /// Reconstruit une instance depuis le stockage. A utiliser par les implementations de <c>IProcessStore</c>.
    /// </summary>
    /// <param name="id">Identifiant.</param>
    /// <param name="definitionKey">Cle de la definition.</param>
    /// <param name="definitionVersion">Version de la definition.</param>
    /// <param name="status">Etat de vie.</param>
    /// <param name="currentNodeId">Noeud courant.</param>
    /// <param name="variablesJson">Variables serialisees.</param>
    /// <param name="createdAt">Date de creation.</param>
    /// <param name="updatedAt">Date de derniere modification.</param>
    /// <param name="parentInstanceId">Instance parente.</param>
    /// <param name="parentNodeId">Noeud parent en attente.</param>
    /// <param name="depth">Profondeur d'imbrication.</param>
    /// <param name="correlationId">Cle de correlation metier.</param>
    /// <param name="outcome">Resultat final.</param>
    /// <param name="faultMessage">Message d'erreur.</param>
    /// <param name="faultDetail">Detail d'erreur.</param>
    /// <param name="completedAt">Date de fin.</param>
    /// <param name="concurrencyToken">Jeton de concurrence.</param>
    /// <returns>L'instance rehydratee.</returns>
    public static ProcessInstance Restore(
        Guid id,
        string definitionKey,
        int definitionVersion,
        ProcessStatus status,
        string currentNodeId,
        string? variablesJson,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid? parentInstanceId = null,
        string? parentNodeId = null,
        int depth = 0,
        string? correlationId = null,
        string? outcome = null,
        string? faultMessage = null,
        string? faultDetail = null,
        DateTimeOffset? completedAt = null,
        object? concurrencyToken = null)
    {
        var instance = new ProcessInstance(
            id,
            definitionKey,
            definitionVersion,
            currentNodeId,
            ProcessVariables.FromJson(variablesJson),
            createdAt)
        {
            Status = status,
            UpdatedAt = updatedAt,
            ParentInstanceId = parentInstanceId,
            ParentNodeId = parentNodeId,
            Depth = depth,
            CorrelationId = correlationId,
            Outcome = outcome,
            FaultMessage = faultMessage,
            FaultDetail = faultDetail,
            CompletedAt = completedAt,
            ConcurrencyToken = concurrencyToken,
        };

        return instance;
    }
}
