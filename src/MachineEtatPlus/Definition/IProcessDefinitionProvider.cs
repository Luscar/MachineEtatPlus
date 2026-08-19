using System.Collections.Generic;

namespace MachineEtatPlus.Definition;

/// <summary>Catalogue des definitions connues du moteur.</summary>
public interface IProcessDefinitionProvider
{
    /// <summary>Retourne une definition.</summary>
    /// <param name="key">Cle fonctionnelle.</param>
    /// <param name="version">Version demandee, ou <c>null</c> pour la plus recente.</param>
    /// <returns>La definition correspondante.</returns>
    /// <exception cref="ProcessDefinitionException">Aucune definition ne correspond.</exception>
    ProcessDefinition Get(string key, int? version = null);

    /// <summary>Tente de retourner une definition.</summary>
    /// <param name="key">Cle fonctionnelle.</param>
    /// <param name="version">Version demandee, ou <c>null</c> pour la plus recente.</param>
    /// <returns>La definition, ou <c>null</c>.</returns>
    ProcessDefinition? Find(string key, int? version = null);

    /// <summary>Enumere toutes les definitions connues.</summary>
    /// <returns>Les definitions enregistrees.</returns>
    IReadOnlyCollection<ProcessDefinition> All();
}
