namespace Richter.Json.Documents;

/// <summary> A device on a network in a scenario file. </summary>
internal record DeviceDocument
{
    /// <summary> <c>workstation</c>, <c>camera</c> or <c>door</c>. </summary>
    public required String Kind { get; init; }

    public required String Name { get; init; }

    /// <summary> Whether the device starts switched on. Left out, it does. </summary>
    public Boolean? Powered { get; init; }

    /// <summary> Whether a door starts locked. Required for a door, and only a door has it. </summary>
    public Boolean? Locked { get; init; }
}
