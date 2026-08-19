using System;
using System.Collections.Generic;

namespace MachineEtatPlus.Definition;

/// <summary>Noeud d'une definition de processus.</summary>
public abstract class NodeDefinition
{
    /// <summary>Initialise la partie commune d'un noeud.</summary>
    /// <param name="id">Identifiant, unique dans la definition.</param>
    /// <param name="name">Libelle lisible, optionnel.</param>
    /// <param name="transitions">Arcs sortants.</param>
    protected NodeDefinition(string id, string? name, IReadOnlyList<Transition> transitions)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("L'identifiant d'un noeud est obligatoire.", nameof(id));
        }

        Id = id;
        Name = name;
        Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
    }

    /// <summary>Identifiant du noeud.</summary>
    public string Id { get; }

    /// <summary>Libelle lisible.</summary>
    public string? Name { get; }

    /// <summary>Arcs sortants.</summary>
    public IReadOnlyList<Transition> Transitions { get; }

    /// <summary>Type du noeud.</summary>
    public abstract NodeKind Kind { get; }

    /// <summary>
    /// Indique si le noeud suspend l'instance : le moteur ne peut pas le franchir dans la
    /// transaction courante.
    /// </summary>
    public virtual bool IsBlocking => false;

    /// <inheritdoc />
    public override string ToString() => Name is null ? $"{Kind}:{Id}" : $"{Kind}:{Id} ({Name})";
}
