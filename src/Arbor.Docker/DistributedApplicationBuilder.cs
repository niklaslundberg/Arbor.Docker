using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Arbor.Docker;

public sealed class DistributedApplicationBuilder
{
    private readonly Dictionary<string, ResourceNode> _nodes = [];
    private ILogger? _logger;

    internal DistributedApplicationBuilder(string[] args, ILogger? logger = null)
    {
        _logger = logger;
        CancellationTokenSource = new();
        PortHelper = new PortHelper(_logger ?? Serilog.Core.Logger.None);
        ResourceEvents = new ResourceEvents(CancellationTokenSource);
    }

    public ResourceEvents ResourceEvents { get; }

    internal CancellationTokenSource CancellationTokenSource { get; }
    public PortHelper PortHelper { get; }
    internal ILogger? Logger => _logger;

    public IResourceReference AddResource(string name, IResource resource)
    {
        var treeNode = new ResourceNode(name, this) { Resource = resource };
        _nodes.Add(name, treeNode);

        return treeNode;
    }
    internal DistributedApplicationBuilder AddResource(ResourceNode resource)
    {
        _nodes.Add(resource.Name, resource);

        return this;
    }

    public async Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (HasCycles())
        {
            throw new InvalidOperationException("References contains cycles: " + ToString());
        }

        var app = new DistributedApplication(_nodes, _logger ?? Serilog.Core.Logger.None, ResourceEvents);

        await app.Initialize();

        return app;
    }

    public DistributedApplicationBuilder AddLogging(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public bool HasStartOrderCycles() => HasCycles(node => node.DependsOnStarted);

    public bool HasDependencyCycles() => HasCycles(node => node.DependsOn);

    public bool HasCycles() => HasDependencyCycles() || HasStartOrderCycles();

    private bool HasCycles(Func<ResourceNode, HashSet<ResourceNode>> set)
    {
        var visited = new HashSet<ResourceNode>();
        var recursionStack = new HashSet<ResourceNode>();

        bool DetectCycle(ResourceNode node)
        {
            if (recursionStack.Contains(node))
            {
                return true;
            }

            if (!visited.Add(node))
            {
                return false;
            }

            recursionStack.Add(node);

            foreach (var dependency in set(node))
            {
                if (DetectCycle(dependency))
                {
                    return true;
                }
            }

            recursionStack.Remove(node);
            return false;
        }

        foreach (var node in _nodes)
        {
            if (DetectCycle(node.Value))
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        StringBuilder builder = new();

        builder.AppendLine("Resources: " + _nodes.Count);
        builder.AppendLine("HasStartOrderCycles: " + HasStartOrderCycles());
        builder.AppendLine("HasDependencyCycles: " + HasDependencyCycles());

        return builder.ToString();
    }
}