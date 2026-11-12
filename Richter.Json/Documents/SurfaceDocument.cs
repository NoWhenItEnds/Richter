namespace Richter.Json.Documents;

/// <summary> A place fixed to a body in a scenario file. </summary>
internal record SurfaceDocument
{
    /// <summary> The <see cref="BodyDocument.Id"/> of the body, such as <c>earth</c>. </summary>
    public required String Body { get; init; }

    /// <summary> Degrees north of the equator. </summary>
    public required Double Latitude { get; init; }

    /// <summary> Degrees east of the prime meridian. </summary>
    public required Double Longitude { get; init; }

    /// <summary> Height above the body's mean radius, in km. </summary>
    public required Double Altitude { get; init; }
}
