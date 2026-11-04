using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>
/// Contrat commun à tous les résolveurs. La catégorie n'est pas portée par le résolveur :
/// elle est uniquement la clé de l'enregistrement Autofac.
/// </summary>
public interface IResolveurTypeProcessus
{
    /// <summary>Type du paramètre attendu ; <c>null</c> si aucun paramètre n'est attendu.</summary>
    Type? TypeParametre { get; }
}

/// <summary>Résolveur ne nécessitant aucun paramètre.</summary>
public interface IResolveurSansParametre : IResolveurTypeProcessus
{
    TypeProcessus Resoudre();
}

/// <summary>Résolveur déterminant le <see cref="TypeProcessus"/> à partir d'un paramètre.</summary>
public interface IResolveurTypeProcessus<in TParam> : IResolveurTypeProcessus
{
    TypeProcessus Resoudre(TParam parametre);
}
