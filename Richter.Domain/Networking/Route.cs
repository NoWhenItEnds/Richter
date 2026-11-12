namespace Richter.Domain.Networking;

/// <summary>
/// The fastest way from one network to another at one instant: the networks passed through and
/// the links crossed, in order, and the total delay.
/// </summary>
/// <remarks>
/// A route holds only for the instant it was found for, because links come and go as things move.
/// It's a class rather than a record because a record's equality would compare the lists by
/// reference, not by what they hold.
/// </remarks>
public class Route
{
    public Route(IReadOnlyList<NetworkId> networks, IReadOnlyList<LinkId> links, TimeSpan delay)
    {
        Networks = networks;
        Links = links;
        Delay = delay;
    }

    /// <summary>
    /// From the start to the end, both included. A route from a network to itself has just that
    /// network.
    /// </summary>
    public IReadOnlyList<NetworkId> Networks { get; }

    /// <summary> <c>Links[i]</c> joins <c>Networks[i]</c> to <c>Networks[i + 1]</c>. </summary>
    public IReadOnlyList<LinkId> Links { get; }

    /// <summary> The sum of the links' delays. </summary>
    public TimeSpan Delay { get; }
}
