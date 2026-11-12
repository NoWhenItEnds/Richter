namespace Richter.Json.Documents;

/// <summary> One celestial body in a star system file. </summary>
internal record BodyDocument
{
    /// <summary>
    /// The key that orbits and scenarios refer to the body by, such as <c>earth</c>.
    /// </summary>
    public required String Id { get; init; }

    public required String Name { get; init; }

    /// <summary> GM, in km³/s². </summary>
    public required Double GravitationalParameter { get; init; }

    /// <summary> Mean radius, in km. </summary>
    public required Double Radius { get; init; }

    /// <summary> Left out for the star at the root of the system. </summary>
    public OrbitDocument? Orbit { get; init; }

    public required SpinDocument Spin { get; init; }
}
