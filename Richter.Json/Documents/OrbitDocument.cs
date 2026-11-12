namespace Richter.Json.Documents;

/// <summary> A body's orbit in a star system file, as published elements. </summary>
internal record OrbitDocument
{
    /// <summary> The <see cref="BodyDocument.Id"/> of the body being orbited. </summary>
    public required String Parent { get; init; }

    /// <summary> <c>reference</c> or <c>parentEquator</c>: the plane the angles are against. </summary>
    public required String Plane { get; init; }

    /// <summary> In days. Give this or <see cref="SemiMajorAxis"/>, not both. </summary>
    public Double? Period { get; init; }

    /// <summary> In km. Give this or <see cref="Period"/>, not both. </summary>
    public Double? SemiMajorAxis { get; init; }

    public required Double Eccentricity { get; init; }

    public required Double Inclination { get; init; }

    public required Double LongitudeOfAscendingNode { get; init; }

    public required Double ArgumentOfPeriapsis { get; init; }

    public required Double MeanAnomaly { get; init; }
}
