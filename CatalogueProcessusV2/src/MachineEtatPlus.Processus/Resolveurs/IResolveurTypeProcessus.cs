using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>
/// Détermine le <see cref="TypeProcessus"/> d'une catégorie. La catégorie n'est pas portée
/// par le résolveur : elle n'est déclarée qu'une fois, à l'enregistrement.
/// </summary>
public interface IResolveurTypeProcessus
{
    TypeProcessus Resoudre(ContexteProcessus contexte);
}
