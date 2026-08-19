using System;
using System.Threading;
using MachineEtatPlus.Abstractions;
using MachineEtatPlus.Definition;
using MachineEtatPlus.Model;

namespace MachineEtatPlus.Execution;

internal sealed class NodeExecutionContext : INodeExecutionContext
{
    internal NodeExecutionContext(
        ProcessDefinition definition,
        NodeDefinition node,
        ProcessInstance instance,
        IDbSession session,
        IServiceProvider? services,
        DateTimeOffset now,
        object? input,
        CancellationToken cancellationToken)
    {
        Definition = definition;
        Node = node;
        Instance = instance;
        Session = session;
        Services = services;
        Now = now;
        Input = input;
        CancellationToken = cancellationToken;
    }

    public ProcessDefinition Definition { get; }

    public NodeDefinition Node { get; }

    public ProcessInstance Instance { get; }

    public ProcessVariables Variables => Instance.Variables;

    public IDbSession Session { get; }

    public IServiceProvider? Services { get; }

    public DateTimeOffset Now { get; }

    public object? Input { get; }

    public CancellationToken CancellationToken { get; }

    public T? GetVariable<T>(string name) => Variables.Get<T>(name);

    public void SetVariable<T>(string name, T value) => Variables.Set(name, value);
}
