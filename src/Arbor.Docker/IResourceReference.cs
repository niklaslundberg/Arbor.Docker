using System.Collections.Immutable;

namespace Arbor.Docker;

public interface IResourceReference
{
    public ImmutableArray<IResourceEndPoint> EndPoints { get; }

    public ResourceEvents Events { get; }

    public string Name { get; }
}