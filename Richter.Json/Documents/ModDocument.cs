namespace Richter.Json.Documents;

/// <summary> The format of a mod's <c>mod.json</c>, which says what the mod is. </summary>
/// <remarks> Renaming a property breaks every mod. </remarks>
internal record ModDocument
{
    /// <summary> Unique among mods; other mods name it in their dependencies. </summary>
    public required String Id { get; init; }

    public required String Name { get; init; }

    public required String Version { get; init; }

    /// <summary> The content format the mod was written for: <see cref="ModList.ContentFormat"/>. </summary>
    public required Int32 ContentFormat { get; init; }

    /// <summary> The ids of the mods it needs, which load before it. </summary>
    public IReadOnlyList<String>? Dependencies { get; init; }
}
