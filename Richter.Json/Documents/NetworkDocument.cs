namespace Richter.Json.Documents;

/// <summary> A network in a scenario file, with its devices. </summary>
internal record NetworkDocument
{
    /// <summary>
    /// A short name, unique in the file, that links use to refer to the network. It isn't kept
    /// once the file is read.
    /// </summary>
    public required String Id { get; init; }

    public required String Name { get; init; }

    /// <summary> Where the network is on or above a body. Give this or <see cref="Orbit"/>. </summary>
    public SurfaceDocument? Surface { get; init; }

    /// <summary>
    /// The orbit of the satellite the network is on, as at the scenario's start. Give this or
    /// <see cref="Surface"/>.
    /// </summary>
    public OrbitDocument? Orbit { get; init; }

    /// <summary> The gateway's name. Left out, it's the network's name followed by "gateway". </summary>
    public String? Gateway { get; init; }

    // Nullable rather than defaulting to empty: the generated reader sets a missing init
    // property to null, over any default.
    public IReadOnlyList<DeviceDocument>? Devices { get; init; }
}
