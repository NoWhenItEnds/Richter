namespace Richter.Json.Documents;

/// <summary> The format of a scenario file, <c>scenarios/&lt;id&gt;.json</c>. </summary>
/// <remarks>
/// <para>
/// Every mod's copy of a scenario is merged in load order: a later file's <c>starSystem</c> and
/// <c>startTime</c> replace earlier ones, and its networks and links replace earlier ones with
/// the same id or are added. So a mod's copy only holds what it changes, and every property is
/// optional in a single file.
/// </para>
/// <para>
/// Renaming a property breaks every scenario and mod that sets it. The start time is read as text
/// so that one written without an offset can be taken as UTC, rather than the time zone of
/// whichever machine reads it.
/// </para>
/// </remarks>
internal record ScenarioDocument
{
    /// <summary> The id of the star system, its file in <c>star-systems/</c>. </summary>
    public String? StarSystem { get; init; }

    public String? StartTime { get; init; }

    // Nullable rather than defaulting to empty: the generated reader sets a missing init
    // property to null, over any default.
    public IReadOnlyList<NetworkDocument>? Networks { get; init; }

    public IReadOnlyList<LinkDocument>? Links { get; init; }
}
