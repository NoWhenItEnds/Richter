namespace Richter.Domain.Networking;

/// <summary>
/// Identifies a <see cref="Network"/>. <see cref="NetworkGraph.NewNetworkId"/> hands them out.
/// </summary>
public readonly record struct NetworkId
{
    public NetworkId(Int32 value)
    {
        Value = value;
    }

    public Int32 Value { get; }
}
