namespace Richter.Json;

/// <summary> One installed mod, as its <c>mod.json</c> describes it. </summary>
public record Mod
{
    public required String Id { get; init; }

    public required String Name { get; init; }

    public required String Version { get; init; }

    /// <summary>
    /// The ids of the mods it needs, which load before it. Every mod also loads after
    /// <c>core</c>, whether it lists it or not.
    /// </summary>
    public required IReadOnlyList<String> Dependencies { get; init; }

    /// <summary> Its folder under the game's directory, such as <c>Mods/core</c>. </summary>
    public required String Path { get; init; }
}
