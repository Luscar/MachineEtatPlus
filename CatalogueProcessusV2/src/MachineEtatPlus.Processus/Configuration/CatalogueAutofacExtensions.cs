using Autofac;
using MachineEtatPlus.Processus.Catalogue;

namespace MachineEtatPlus.Processus.Configuration;

public static class CatalogueAutofacExtensions
{
    /// <summary>
    /// Enregistre <see cref="ICatalogueProcessus"/>. Le catalogue est auto-activé : une catégorie
    /// manquante ou une dépendance absente d'un résolveur dédié fait échouer <c>Build()</c>.
    /// </summary>
    public static ContainerBuilder AddCatalogueProcessus(
        this ContainerBuilder b, Action<DefinitionCatalogueProcessus> configurer)
    {
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(configurer);

        var definition = new DefinitionCatalogueProcessus();
        configurer(definition);

        foreach (var type in definition.TypesDedies)
        {
            var r = b.RegisterType(type).AsSelf();
            if (definition.ParScope) r.InstancePerLifetimeScope(); else r.SingleInstance();
        }

        var exclues = definition.Exclues.ToArray();
        var catalogue = b.Register(ctx => new CatalogueProcessus(definition.Construire(ctx), exclues))
            .As<ICatalogueProcessus>()
            .AutoActivate();
        if (definition.ParScope) catalogue.InstancePerLifetimeScope(); else catalogue.SingleInstance();

        return b;
    }
}
