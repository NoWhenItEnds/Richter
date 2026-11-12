using Richter.Domain.Astronomy;

namespace Richter.Domain.Networking;

/// <summary> A group of devices in one place, which reaches other networks through links. </summary>
/// <remarks>
/// A network doesn't list its devices or links: <see cref="NetworkGraph"/> keeps those, and is
/// the only way to add a network, together with its <see cref="Gateway"/>.
/// </remarks>
public class Network
{
    public required NetworkId Id { get; init; }

    public required String Name { get; init; }

    /// <summary>
    /// Where the network is: a <see cref="SurfaceLocation"/> on the ground, or an
    /// <see cref="OrbitalLocation"/> on a satellite.
    /// </summary>
    public required Location Location { get; init; }
}
