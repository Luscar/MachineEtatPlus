using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MachineEtatPlus.Model;

/// <summary>
/// Sac de variables d'une instance de processus. Chaque valeur est conservee sous forme JSON,
/// ce qui permet a l'implementation de stockage cliente de ne persister qu'une seule colonne texte.
/// </summary>
public sealed class ProcessVariables
{
    private readonly Dictionary<string, string> _values;

    /// <summary>Cree un sac de variables vide.</summary>
    public ProcessVariables()
        : this(new Dictionary<string, string>(StringComparer.Ordinal))
    {
    }

    private ProcessVariables(Dictionary<string, string> values) => _values = values;

    /// <summary>Options de serialisation utilisees pour les valeurs. Modifiable au demarrage de l'application.</summary>
    public static JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    /// <summary>Noms des variables presentes.</summary>
    public IReadOnlyCollection<string> Names => _values.Keys;

    /// <summary>Nombre de variables presentes.</summary>
    public int Count => _values.Count;

    /// <summary>Indique si une variable porte ce nom.</summary>
    /// <param name="name">Nom de la variable.</param>
    /// <returns><c>true</c> si la variable existe.</returns>
    public bool Contains(string name) => _values.ContainsKey(Require(name));

    /// <summary>Lit une variable, ou retourne la valeur par defaut de <typeparamref name="T"/> si elle est absente.</summary>
    /// <typeparam name="T">Type attendu.</typeparam>
    /// <param name="name">Nom de la variable.</param>
    /// <returns>La valeur deserialisee, ou <c>default</c>.</returns>
    public T? Get<T>(string name)
        => _values.TryGetValue(Require(name), out string? json)
            ? JsonSerializer.Deserialize<T>(json, SerializerOptions)
            : default;

    /// <summary>Lit une variable, ou retourne <paramref name="fallback"/> si elle est absente.</summary>
    /// <typeparam name="T">Type attendu.</typeparam>
    /// <param name="name">Nom de la variable.</param>
    /// <param name="fallback">Valeur retournee en cas d'absence.</param>
    /// <returns>La valeur deserialisee, ou <paramref name="fallback"/>.</returns>
    public T GetOrDefault<T>(string name, T fallback)
        => _values.TryGetValue(Require(name), out string? json)
            ? JsonSerializer.Deserialize<T>(json, SerializerOptions)!
            : fallback;

    /// <summary>Ecrit une variable.</summary>
    /// <typeparam name="T">Type de la valeur.</typeparam>
    /// <param name="name">Nom de la variable.</param>
    /// <param name="value">Valeur a serialiser.</param>
    public void Set<T>(string name, T value)
        => _values[Require(name)] = JsonSerializer.Serialize(value, SerializerOptions);

    /// <summary>Ecrit une variable dont le type n'est connu qu'a l'execution.</summary>
    /// <param name="name">Nom de la variable.</param>
    /// <param name="value">Valeur a serialiser.</param>
    public void SetObject(string name, object? value)
        => _values[Require(name)] = value is null
            ? "null"
            : JsonSerializer.Serialize(value, value.GetType(), SerializerOptions);

    /// <summary>Supprime une variable.</summary>
    /// <param name="name">Nom de la variable.</param>
    /// <returns><c>true</c> si la variable existait.</returns>
    public bool Remove(string name) => _values.Remove(Require(name));

    /// <summary>Copie toutes les variables de <paramref name="source"/> dans ce sac.</summary>
    /// <param name="source">Sac source.</param>
    public void CopyFrom(ProcessVariables source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        foreach (KeyValuePair<string, string> pair in source._values)
        {
            _values[pair.Key] = pair.Value;
        }
    }

    /// <summary>Copie une seule variable depuis <paramref name="source"/>, en la renommant si besoin.</summary>
    /// <param name="source">Sac source.</param>
    /// <param name="sourceName">Nom dans le sac source.</param>
    /// <param name="targetName">Nom cible, ou <c>null</c> pour conserver le meme nom.</param>
    /// <returns><c>true</c> si la variable existait dans la source.</returns>
    public bool CopyFrom(ProcessVariables source, string sourceName, string? targetName = null)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (!source._values.TryGetValue(Require(sourceName), out string? json))
        {
            return false;
        }

        _values[targetName is null ? sourceName : Require(targetName)] = json;
        return true;
    }

    /// <summary>Serialise l'ensemble des variables sous forme d'un unique document JSON.</summary>
    /// <returns>Le document JSON a persister.</returns>
    public string ToJson() => JsonSerializer.Serialize(_values, SerializerOptions);

    /// <summary>Reconstruit un sac de variables a partir du document JSON produit par <see cref="ToJson"/>.</summary>
    /// <param name="json">Document JSON, eventuellement vide ou <c>null</c>.</param>
    /// <returns>Le sac de variables reconstruit.</returns>
    public static ProcessVariables FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ProcessVariables();
        }

        Dictionary<string, string>? values = JsonSerializer.Deserialize<Dictionary<string, string>>(json!, SerializerOptions);
        return values is null
            ? new ProcessVariables()
            : new ProcessVariables(new Dictionary<string, string>(values, StringComparer.Ordinal));
    }

    private static string Require(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom de variable est obligatoire.", nameof(name));
        }

        return name;
    }
}
