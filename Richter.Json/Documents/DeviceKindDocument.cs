namespace Richter.Json.Documents;

/// <summary> One kind of device in <c>device-kinds.json</c>. </summary>
internal record DeviceKindDocument
{
    /// <summary> The key that devices name their kind by, such as <c>door</c>. </summary>
    public required String Id { get; init; }

    public required String Name { get; init; }

    /// <summary> The components devices of this kind have, with their starting settings. </summary>
    public ComponentsDocument? Components { get; init; }
}
