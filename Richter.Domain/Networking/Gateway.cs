namespace Richter.Domain.Networking;

/// <summary>
/// The device that every <see cref="Link"/> to or from its network passes through.
/// </summary>
/// <remarks>
/// Each network has exactly one. <see cref="NetworkGraph.AddNetwork"/> adds it with its network,
/// <see cref="NetworkGraph.RemoveNetwork"/> removes it with its network, and the graph refuses
/// to add or remove a gateway on its own.
/// </remarks>
public class Gateway : Device
{
}
