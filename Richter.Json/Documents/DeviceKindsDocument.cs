namespace Richter.Json.Documents;

/// <summary> The format of <c>device-kinds.json</c>. </summary>
/// <remarks>
/// Every mod's copy is merged in load order: a kind replaces one with the same id, or is added.
/// Renaming a property breaks every file and mod that sets it.
/// </remarks>
internal record DeviceKindsDocument
{
    public required IReadOnlyList<DeviceKindDocument> Kinds { get; init; }
}
