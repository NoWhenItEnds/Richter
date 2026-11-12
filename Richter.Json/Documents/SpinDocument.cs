namespace Richter.Json.Documents;

/// <summary>
/// A body's spin in a star system file, as the IAU publishes it: against Earth's J2000 equator.
/// </summary>
internal record SpinDocument
{
    /// <summary> α₀, in degrees. </summary>
    public required Double PoleRightAscension { get; init; }

    /// <summary> δ₀, in degrees. </summary>
    public required Double PoleDeclination { get; init; }

    /// <summary> W at the file's epoch, in degrees. </summary>
    public required Double PrimeMeridian { get; init; }

    /// <summary> W's rate, negative for a body that spins backwards. </summary>
    public required Double DegreesPerDay { get; init; }
}
