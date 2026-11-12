namespace Richter.Json.Documents;

/// <summary> The format of a star system file, <c>star-systems/&lt;id&gt;.json</c>. </summary>
/// <remarks>
/// <para>
/// Every mod's copy of a star system is merged in load order: a body replaces one with the same
/// id, or is added. Each file's epoch and tilt apply to the bodies in that file, because
/// published elements always come with the epoch they describe.
/// </para>
/// <para>
/// Renaming a property breaks every star system file and mod that sets it. Numbers are in the
/// units sources publish them in, km, km³/s², degrees and days, so they can be copied unchanged.
/// </para>
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
