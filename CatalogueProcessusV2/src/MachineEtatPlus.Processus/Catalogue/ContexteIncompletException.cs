namespace MachineEtatPlus.Processus.Catalogue;

/// <summary>Levée lorsqu'un résolveur a besoin d'une information absente du contexte.</summary>
public sealed class ContexteIncompletException(string champ, string? message = null, Exception? inner = null)
    : InvalidOperationException(message ?? $"Le contexte ne contient pas '{champ}'.", inner)
{
    public string Champ { get; } = champ;
}
