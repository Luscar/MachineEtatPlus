using System;
using MachineEtatPlus.Execution;

namespace MachineEtatPlus.Definition;

/// <summary>Arc sortant d'un noeud.</summary>
public sealed class Transition
{
    /// <summary>Cree une transition.</summary>
    /// <param name="target">Identifiant du noeud cible.</param>
    /// <param name="outcome">
    /// Resultat qui declenche cette transition, ou <c>null</c> pour la transition par defaut.
    /// </param>
    /// <param name="condition">Garde optionnelle evaluee avant de retenir la transition.</param>
    public Transition(string target, string? outcome = null, Func<INodeExecutionContext, bool>? condition = null)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("La cible d'une transition est obligatoire.", nameof(target));
        }

        Target = target;
        Outcome = outcome;
        Condition = condition;
    }

    /// <summary>Identifiant du noeud cible.</summary>
    public string Target { get; }

    /// <summary>Resultat declencheur, ou <c>null</c> pour la transition par defaut.</summary>
    public string? Outcome { get; }

    /// <summary>Garde optionnelle.</summary>
    public Func<INodeExecutionContext, bool>? Condition { get; }

    /// <summary>Indique si la transition est celle empruntee a defaut de correspondance de resultat.</summary>
    public bool IsDefault => Outcome is null;
}
