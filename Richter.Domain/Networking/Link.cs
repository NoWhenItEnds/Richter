namespace Richter.Domain.Networking;

/// <summary>
/// A connection between two networks over one medium, such as a cable or a radio beam.
/// </summary>
/// <remarks>
/// Traffic crosses both ways, so which network is A and which is B carries no meaning. Whether the
/// link is up, and how long a signal takes, depend on where its ends are at the time:
/// <see cref="NetworkGraph.DelayOf"/> works them out.
/// </remarks>
public class Link
{
    public required LinkId Id { get; init; }

    /// <summary>
    /// The id content gave the link, such as <c>sydney-london</c>, which mods and saves refer to
    /// it by. Null for a link made during play.
    /// </summary>
    public String? Key { get; init; }

    public required NetworkId NetworkA { get; init; }

    public required NetworkId NetworkB { get; init; }

    public required LinkMedium Medium { get; init; }
}
