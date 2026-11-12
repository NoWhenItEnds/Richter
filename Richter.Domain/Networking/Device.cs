namespace Richter.Domain.Networking;

/// <summary> A machine on a network, of a <see cref="DeviceKind"/> that content defines. </summary>
/// <remarks>
/// <para>
/// Identity (<see cref="Id"/>, <see cref="Kind"/>, <see cref="Name"/>, <see cref="Network"/>) is
/// fixed at creation. The components the kind has, such as <see cref="Lock"/>, are created with
/// the device from the kind's definitions; one the kind doesn't have is null.
/// </para>
/// <para>
/// State, such as <see cref="Powered"/>, is a plain property on the device or its components,
/// with a public setter because no rule guards it yet. When one does, make that setter
/// <c>internal</c> and put the rule in a game action in this project: the compiler then lists
/// every caller that has to move to the action.
/// </para>
/// </remarks>
public class Device
{
    public Device(DeviceId id, DeviceKind kind, String name, NetworkId network)
    {
        Id = id;
        Kind = kind.Id;
        Name = name;
        Network = network;
        Lock = kind.Lock is null ? null : new DeviceLock { Locked = kind.Lock.StartsLocked };
    }

    public DeviceId Id { get; }

    public DeviceKindId Kind { get; }

    public String Name { get; }

    public NetworkId Network { get; }

    public Boolean Powered { get; set; } = true;

    /// <summary> The device's lock, or null if its kind has none. </summary>
    public DeviceLock? Lock { get; }
}
