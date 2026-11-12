namespace Richter.Json.Documents;

/// <summary> The format of a star system file such as <c>Data/solar-system.json</c>. </summary>
/// <remarks>
/// Renaming a property breaks every star system file and mod that sets it. Numbers are in the
/// units sources publish them in, km, km³/s², degrees and days, so they can be copied unchanged.
/// </remarks>
internal record StarSystemDocument
{
    /// <summary> The instant every element in the file describes. </summary>
    public required String Epoch { get; init; }

    /// <summary>
    /// How far the system's reference plane is tilted from Earth's J2000 equator, in degrees. Spin
    /// elements are published against that equator.
    /// </summary>
    public required Double ReferencePlaneTilt { get; init; }

    public required IReadOnlyList<BodyDocument> Bodies { get; init; }
}
