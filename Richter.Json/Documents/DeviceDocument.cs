namespace Richter.Json.Documents;

/// <summary> A device on a network in a scenario file. </summary>
internal record DeviceDocument
{
    /// <summary> The id of a kind in <c>device-kinds.json</c>, such as <c>door</c>. </summary>
    public required String Kind { get; init; }

    public required String Name { get; init; }

    /// <summary> Whether the device starts switched on. Left out, it does. </summary>
    public Boolean? Powered { get; init; }

    /// <summary> Settings that replace the kind's, for components the kind has. </summary>
    public ComponentsDocument? Components { get; init; }
}
