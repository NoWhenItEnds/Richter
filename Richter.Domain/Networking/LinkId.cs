namespace Richter.Domain.Networking;

/// <summary>
/// Identifies a <see cref="Link"/>. <see cref="NetworkGraph.NewLinkId"/> hands them out.
/// </summary>
public readonly record struct LinkId
{
    public LinkId(Int32 value)
    {
        Value = value;
    }

    public Int32 Value { get; }
}
