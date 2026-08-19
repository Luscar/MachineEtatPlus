using System;
using System.Collections.Generic;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MachineEtatPlus.DependencyInjection;

/// <summary>Points d'entree pour brancher le moteur sur un conteneur d'injection de dependances.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre le moteur, ses dispatchers et le catalogue de definitions.
    /// L'application cliente doit ensuite fournir sa persistance, sa gestion de taches et,
    /// si elle n'utilise pas la session ambiante, son accesseur de session.
    /// </summary>
    /// <param name="services">Collection de services.</param>
    /// <param name="configure">Reglages du moteur.</param>
    /// <returns>Un constructeur permettant d'enregistrer definitions et handlers.</returns>
    public static MachineEtatPlusBuilder AddMachineEtatPlus(
        this IServiceCollection services,
        Action<ProcessEngineOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var options = new ProcessEngineOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddSingleton<AmbientDbSessionAccessor>();
        services.TryAddSingleton<IDbSessionAccessor>(sp => sp.GetRequiredService<AmbientDbSessionAccessor>());
        services.TryAddSingleton<IDbSessionScopeFactory>(sp => sp.GetRequiredService<AmbientDbSessionAccessor>());

        services.TryAddSingleton<IProcessClock>(SystemProcessClock.Instance);

        // Les dispatchers sont enregistres par portee : ils recoivent ainsi le fournisseur de la
        // portee courante et peuvent resoudre des handlers eux-memes par portee, qui partagent la
        // transaction du client.
        services.TryAddScoped<ICommandDispatcher, ServiceProviderCommandDispatcher>();
        services.TryAddScoped<IDecisionDispatcher, ServiceProviderDecisionDispatcher>();

        services.TryAddSingleton<IProcessDefinitionProvider>(sp =>
            new ProcessDefinitionRegistry(sp.GetServices<ProcessDefinition>()));

        services.TryAddScoped<IProcessEngine>(sp => new ProcessEngine(
            sp.GetRequiredService<IProcessDefinitionProvider>(),
            sp.GetRequiredService<IProcessStore>(),
            sp.GetRequiredService<IProcessJobScheduler>(),
            sp.GetRequiredService<IDbSessionAccessor>(),
            sp.GetService<ICommandDispatcher>(),
            sp.GetService<IDecisionDispatcher>(),
            sp.GetService<IProcessClock>(),
            sp.GetRequiredService<ProcessEngineOptions>(),
            sp,
            sp.GetService<ILogger<ProcessEngine>>()));

        return new MachineEtatPlusBuilder(services);
    }
}

/// <summary>Constructeur d'enregistrement des definitions, handlers et adaptateurs clients.</summary>
public sealed class MachineEtatPlusBuilder
{
    internal MachineEtatPlusBuilder(IServiceCollection services) => Services = services;

    /// <summary>Collection de services sous-jacente.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Enregistre une definition de processus.</summary>
    /// <param name="definition">Definition a enregistrer.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder AddDefinition(ProcessDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        Services.AddSingleton(definition);
        return this;
    }

    /// <summary>Enregistre une definition construite a la demande.</summary>
    /// <param name="factory">Fabrique de la definition.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder AddDefinition(Func<IServiceProvider, ProcessDefinition> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        Services.AddSingleton(factory);
        return this;
    }

    /// <summary>Enregistre plusieurs definitions.</summary>
    /// <param name="definitions">Definitions a enregistrer.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder AddDefinitions(IEnumerable<ProcessDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        foreach (ProcessDefinition definition in definitions)
        {
            AddDefinition(definition);
        }

        return this;
    }

    /// <summary>Enregistre la persistance cliente.</summary>
    /// <typeparam name="TStore">Implementation de <see cref="IProcessStore"/>.</typeparam>
    /// <param name="lifetime">Duree de vie ; portee par defaut, comme la transaction.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder UseStore<TStore>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TStore : class, IProcessStore
    {
        Services.Add(new ServiceDescriptor(typeof(IProcessStore), typeof(TStore), lifetime));
        return this;
    }

    /// <summary>Enregistre la gestion de taches cliente.</summary>
    /// <typeparam name="TScheduler">Implementation de <see cref="IProcessJobScheduler"/>.</typeparam>
    /// <param name="lifetime">Duree de vie ; portee par defaut, comme la transaction.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder UseJobScheduler<TScheduler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TScheduler : class, IProcessJobScheduler
    {
        Services.Add(new ServiceDescriptor(typeof(IProcessJobScheduler), typeof(TScheduler), lifetime));
        return this;
    }

    /// <summary>Remplace l'accesseur de session ambiant par celui de l'application cliente.</summary>
    /// <typeparam name="TAccessor">Implementation de <see cref="IDbSessionAccessor"/>.</typeparam>
    /// <param name="lifetime">Duree de vie ; portee par defaut.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder UseSessionAccessor<TAccessor>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TAccessor : class, IDbSessionAccessor
    {
        Services.RemoveAll<IDbSessionAccessor>();
        Services.Add(new ServiceDescriptor(typeof(IDbSessionAccessor), typeof(TAccessor), lifetime));
        return this;
    }

    /// <summary>Remplace l'horloge du moteur.</summary>
    /// <typeparam name="TClock">Implementation de <see cref="IProcessClock"/>.</typeparam>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder UseClock<TClock>()
        where TClock : class, IProcessClock
    {
        Services.RemoveAll<IProcessClock>();
        Services.AddSingleton<IProcessClock, TClock>();
        return this;
    }

    /// <summary>Enregistre le traitement d'une commande de noeud metier.</summary>
    /// <typeparam name="TCommand">Type de la commande.</typeparam>
    /// <typeparam name="THandler">Type du traitement.</typeparam>
    /// <param name="lifetime">Duree de vie ; portee par defaut.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder AddCommandHandler<TCommand, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, ICommandHandler<TCommand>
    {
        Services.Add(new ServiceDescriptor(typeof(ICommandHandler<TCommand>), typeof(THandler), lifetime));
        return this;
    }

    /// <summary>Enregistre l'evaluation d'une requete de noeud decision.</summary>
    /// <typeparam name="TQuery">Type de la requete.</typeparam>
    /// <typeparam name="THandler">Type de l'evaluation.</typeparam>
    /// <param name="lifetime">Duree de vie ; portee par defaut.</param>
    /// <returns>Ce constructeur.</returns>
    public MachineEtatPlusBuilder AddDecisionHandler<TQuery, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IDecisionHandler<TQuery>
    {
        Services.Add(new ServiceDescriptor(typeof(IDecisionHandler<TQuery>), typeof(THandler), lifetime));
        return this;
    }
}
