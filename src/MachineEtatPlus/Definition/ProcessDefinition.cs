using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineEtatPlus.Definition;

/// <summary>Definition immuable et versionnee d'un processus.</summary>
public sealed class ProcessDefinition
{
    private readonly Dictionary<string, NodeDefinition> _nodes;

    internal ProcessDefinition(string key, int version, string? name, string startNodeId, IReadOnlyList<NodeDefinition> nodes)
    {
        Key = key;
        Version = version;
        Name = name;
        StartNodeId = startNodeId;
        Nodes = nodes;
        _nodes = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
    }

    /// <summary>Cle fonctionnelle du processus.</summary>
    public string Key { get; }

    /// <summary>Version de la definition.</summary>
    public int Version { get; }

    /// <summary>Libelle lisible.</summary>
    public string? Name { get; }

    /// <summary>Identifiant du noeud de depart.</summary>
    public string StartNodeId { get; }

    /// <summary>Ensemble des noeuds.</summary>
    public IReadOnlyList<NodeDefinition> Nodes { get; }

    /// <summary>Ouvre un constructeur fluide pour une nouvelle definition.</summary>
    /// <param name="key">Cle fonctionnelle du processus.</param>
    /// <param name="version">Version de la definition.</param>
    /// <returns>Le constructeur.</returns>
    public static ProcessDefinitionBuilder Create(string key, int version = 1) => new ProcessDefinitionBuilder(key, version);

    /// <summary>Retourne un noeud par son identifiant.</summary>
    /// <param name="nodeId">Identifiant recherche.</param>
    /// <returns>Le noeud correspondant.</returns>
    /// <exception cref="ProcessDefinitionException">Le noeud n'existe pas.</exception>
    public NodeDefinition GetNode(string nodeId)
        => _nodes.TryGetValue(nodeId, out NodeDefinition? node)
            ? node
            : throw new ProcessDefinitionException($"Le processus '{Key}' v{Version} ne contient aucun noeud '{nodeId}'.");

    /// <summary>Tente de retourner un noeud par son identifiant.</summary>
    /// <param name="nodeId">Identifiant recherche.</param>
    /// <returns>Le noeud, ou <c>null</c>.</returns>
    public NodeDefinition? FindNode(string nodeId)
        => _nodes.TryGetValue(nodeId, out NodeDefinition? node) ? node : null;

    /// <inheritdoc />
    public override string ToString() => $"{Key} v{Version}";
}
