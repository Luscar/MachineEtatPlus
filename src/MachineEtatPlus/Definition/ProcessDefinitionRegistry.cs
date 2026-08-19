using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineEtatPlus.Definition;

/// <summary>Catalogue en memoire des definitions de processus.</summary>
public sealed class ProcessDefinitionRegistry : IProcessDefinitionProvider
{
    private readonly Dictionary<string, SortedDictionary<int, ProcessDefinition>> _definitions =
        new Dictionary<string, SortedDictionary<int, ProcessDefinition>>(StringComparer.Ordinal);

    /// <summary>Cree un catalogue vide.</summary>
    public ProcessDefinitionRegistry()
    {
    }

    /// <summary>Cree un catalogue alimente par les definitions fournies.</summary>
    /// <param name="definitions">Definitions a enregistrer.</param>
    public ProcessDefinitionRegistry(IEnumerable<ProcessDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        foreach (ProcessDefinition definition in definitions)
        {
            Add(definition);
        }
    }

    /// <summary>Enregistre une definition.</summary>
    /// <param name="definition">Definition a enregistrer.</param>
    /// <returns>Ce catalogue, pour chainage.</returns>
    /// <exception cref="ProcessDefinitionException">La cle et la version sont deja enregistrees.</exception>
    public ProcessDefinitionRegistry Add(ProcessDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (!_definitions.TryGetValue(definition.Key, out SortedDictionary<int, ProcessDefinition>? versions))
        {
            versions = new SortedDictionary<int, ProcessDefinition>();
            _definitions[definition.Key] = versions;
        }

        if (versions.ContainsKey(definition.Version))
        {
            throw new ProcessDefinitionException($"Le processus '{definition.Key}' v{definition.Version} est deja enregistre.");
        }

        versions.Add(definition.Version, definition);
        return this;
    }

    /// <inheritdoc />
    public ProcessDefinition Get(string key, int? version = null)
        => Find(key, version)
           ?? throw new ProcessDefinitionException(
               version.HasValue
                   ? $"Le processus '{key}' v{version.Value} est introuvable."
                   : $"Le processus '{key}' est introuvable.");

    /// <inheritdoc />
    public ProcessDefinition? Find(string key, int? version = null)
    {
        if (key is null || !_definitions.TryGetValue(key, out SortedDictionary<int, ProcessDefinition>? versions) || versions.Count == 0)
        {
            return null;
        }

        if (version.HasValue)
        {
            return versions.TryGetValue(version.Value, out ProcessDefinition? found) ? found : null;
        }

        return versions[versions.Keys.Last()];
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ProcessDefinition> All()
        => _definitions.Values.SelectMany(v => v.Values).ToList();
}
