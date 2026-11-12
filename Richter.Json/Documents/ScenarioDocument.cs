namespace Richter.Json.Documents;

/// <summary> The format of a scenario file such as <c>Data/scenario.json</c>. </summary>
/// <remarks>
/// Renaming a property breaks every scenario and mod that sets it. The start time is read as text
/// so that one written without an offset can be taken as UTC, rather than the time zone of
/// whichever machine reads it.
/// </remarks>
internal record ScenarioDocument
{
    public required String StartTime { get; init; }

    // Nullable rather than defaulting to empty: the generated reader sets a missing init
    // property to null, over any default.
    public IReadOnlyList<NetworkDocument>? Networks { get; init; }

    public IReadOnlyList<LinkDocument>? Links { get; init; }
}
