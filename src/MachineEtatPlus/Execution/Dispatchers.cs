using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MachineEtatPlus.Abstractions;

namespace MachineEtatPlus.Execution;

/// <summary>
/// Route les commandes vers le <see cref="ICommandHandler{TCommand}"/> ferme sur le type reel
/// de la commande, resolu depuis le conteneur d'injection de dependances.
/// </summary>
public sealed class ServiceProviderCommandDispatcher : ICommandDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> Methods = new ConcurrentDictionary<Type, MethodInfo>();
    private readonly IServiceProvider _services;

    /// <summary>Cree le dispatcher.</summary>
    /// <param name="services">Fournisseur de services.</param>
    public ServiceProviderCommandDispatcher(IServiceProvider services)
        => _services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    public Task<string?> DispatchAsync(object command, INodeExecutionContext context, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        Type handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        object handler = _services.GetService(handlerType)
                         ?? throw new ProcessHandlerNotFoundException(command.GetType(), handlerType);

        MethodInfo method = Methods.GetOrAdd(
            handlerType,
            static type => type.GetMethod(nameof(ICommandHandler<object>.HandleAsync))
                           ?? throw new ProcessEngineException($"Le type '{type}' n'expose pas HandleAsync."));

        return (Task<string?>)method.Invoke(handler, new object?[] { command, context, cancellationToken })!;
    }
}

/// <summary>
/// Route les requetes vers le <see cref="IDecisionHandler{TQuery}"/> ferme sur le type reel
/// de la requete, resolu depuis le conteneur d'injection de dependances.
/// </summary>
public sealed class ServiceProviderDecisionDispatcher : IDecisionDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> Methods = new ConcurrentDictionary<Type, MethodInfo>();
    private readonly IServiceProvider _services;

    /// <summary>Cree le dispatcher.</summary>
    /// <param name="services">Fournisseur de services.</param>
    public ServiceProviderDecisionDispatcher(IServiceProvider services)
        => _services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    public Task<string> DispatchAsync(object query, INodeExecutionContext context, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        Type handlerType = typeof(IDecisionHandler<>).MakeGenericType(query.GetType());
        object handler = _services.GetService(handlerType)
                         ?? throw new ProcessHandlerNotFoundException(query.GetType(), handlerType);

        MethodInfo method = Methods.GetOrAdd(
            handlerType,
            static type => type.GetMethod(nameof(IDecisionHandler<object>.EvaluateAsync))
                           ?? throw new ProcessEngineException($"Le type '{type}' n'expose pas EvaluateAsync."));

        return (Task<string>)method.Invoke(handler, new object?[] { query, context, cancellationToken })!;
    }
}

/// <summary>
/// Dispatcher de repli utilise quand aucun conteneur n'est disponible : il refuse toute commande,
/// ce qui oblige les definitions a utiliser des traitements en ligne.
/// </summary>
public sealed class UnavailableDispatcher : ICommandDispatcher, IDecisionDispatcher
{
    /// <summary>Instance partagee.</summary>
    public static readonly UnavailableDispatcher Instance = new UnavailableDispatcher();

    /// <inheritdoc />
    Task<string?> ICommandDispatcher.DispatchAsync(object command, INodeExecutionContext context, CancellationToken cancellationToken)
        => throw new ProcessHandlerNotFoundException(command?.GetType() ?? typeof(object), typeof(ICommandHandler<>));

    /// <inheritdoc />
    Task<string> IDecisionDispatcher.DispatchAsync(object query, INodeExecutionContext context, CancellationToken cancellationToken)
        => throw new ProcessHandlerNotFoundException(query?.GetType() ?? typeof(object), typeof(IDecisionHandler<>));
}
