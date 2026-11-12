namespace Richter.Domain.Networking;

/// <summary>
/// Identifies a <see cref="Device"/>. <see cref="NetworkGraph.NewDeviceId"/> hands them out.
/// </summary>
public readonly record struct DeviceId
{
    public DeviceId(Int32 value)
    {
        Value = value;
    }

    public Int32 Value { get; }
}
