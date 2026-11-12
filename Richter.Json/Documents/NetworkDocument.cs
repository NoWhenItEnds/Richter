namespace Richter.Json.Documents;

/// <summary> A network in a scenario file, with its devices. </summary>
internal record NetworkDocument
{
    /// <summary>
    /// Unique in the scenario. Links and later mods refer to the network by it, and it's kept as
    /// the network's <c>Key</c>.
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

    /// <summary>
    /// The device that every link to or from the network passes through. Left out, it's of kind
    /// <c>gateway</c>, named after the network.
    /// </summary>
    public DeviceDocument? Gateway { get; init; }

    // Nullable rather than defaulting to empty: the generated reader sets a missing init
    // property to null, over any default.
    public IReadOnlyList<DeviceDocument>? Devices { get; init; }
}
