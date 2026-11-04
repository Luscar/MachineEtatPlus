using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Résolveur basé sur une table de correspondance clé → <see cref="TypeProcessus"/>.</summary>
public sealed class ResolveurParCle<TCle> : IResolveurTypeProcessus<TCle>
    where TCle : notnull
{
    private readonly IReadOnlyDictionary<TCle, TypeProcessus> _correspondances;

    public ResolveurParCle(IReadOnlyDictionary<TCle, TypeProcessus> correspondances)
    {
        ArgumentNullException.ThrowIfNull(correspondances);
        // Copie défensive : la table ne peut plus être modifiée après l'enregistrement.
        _correspondances = new Dictionary<TCle, TypeProcessus>(correspondances);
    }

    public Type? TypeParametre => typeof(TCle);

    public TypeProcessus Resoudre(TCle cle) =>
        _correspondances.TryGetValue(cle, out var type)
            ? type
            : throw new InvalidOperationException($"Aucun TypeProcessus pour {typeof(TCle).Name}.{cle}.");
}
