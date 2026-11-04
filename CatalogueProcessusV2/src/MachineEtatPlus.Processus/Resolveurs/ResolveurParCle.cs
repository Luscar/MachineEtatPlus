using System.Collections.Frozen;
using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Table de correspondance sur une information du contexte.</summary>
public sealed class ResolveurParCle<TCle> : IResolveurTypeProcessus
    where TCle : struct
{
    private readonly Func<ContexteProcessus, TCle> _extraire;
    private readonly FrozenDictionary<TCle, TypeProcessus> _correspondances;

    public ResolveurParCle(Func<ContexteProcessus, TCle> extraire,
        IReadOnlyDictionary<TCle, TypeProcessus> correspondances)
    {
        ArgumentNullException.ThrowIfNull(extraire);
        ArgumentNullException.ThrowIfNull(correspondances);
        _extraire = extraire;
        _correspondances = correspondances.ToFrozenDictionary(); // copie : immuable après enregistrement
    }

    public TypeProcessus Resoudre(ContexteProcessus contexte)
    {
        var cle = _extraire(contexte);
        return _correspondances.TryGetValue(cle, out var type)
            ? type
            : throw new InvalidOperationException($"Aucun TypeProcessus pour {typeof(TCle).Name}.{cle}.");
    }
}

public static class ResolveurParCle
{
    public static ResolveurParCle<TypeFormulaire> ParTypeFormulaire(
        IReadOnlyDictionary<TypeFormulaire, TypeProcessus> correspondances)
        => new(c => c.ExigerTypeFormulaire(), correspondances);

    public static ResolveurParCle<TypeDossier> ParTypeDossier(
        IReadOnlyDictionary<TypeDossier, TypeProcessus> correspondances)
        => new(c => c.ExigerTypeDossier(), correspondances);
}
