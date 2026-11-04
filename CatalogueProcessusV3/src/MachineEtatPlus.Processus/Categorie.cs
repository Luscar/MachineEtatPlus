using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus;

/// <summary>Catégorie qui n'a besoin d'aucune autre information pour décider.</summary>
public sealed class Categorie
{
    private readonly Func<TypeProcessus> _decider;

    internal Categorie(CategorieProcessus valeur, Func<TypeProcessus> decider)
    {
        Valeur = valeur;
        _decider = decider;
    }

    public CategorieProcessus Valeur { get; }

    internal TypeProcessus Decider() => _decider();

    public override string ToString() => Valeur.ToString();
}

/// <summary>
/// Catégorie qui a besoin d'une information de type <typeparamref name="TInfo"/> pour décider.
/// Le type de l'information fait partie de la catégorie : passer la mauvaise ne compile pas.
/// </summary>
public sealed class Categorie<TInfo>
{
    private readonly Func<TInfo, TypeProcessus> _decider;

    internal Categorie(CategorieProcessus valeur, Func<TInfo, TypeProcessus> decider)
    {
        Valeur = valeur;
        _decider = decider;
    }

    public CategorieProcessus Valeur { get; }

    internal TypeProcessus Decider(TInfo info) => _decider(info);

    public override string ToString() => Valeur.ToString();
}
