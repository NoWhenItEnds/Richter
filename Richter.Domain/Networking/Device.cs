namespace Richter.Domain.Networking;

/// <summary>
/// A machine on a network. Each kind, such as <see cref="Gateway"/> or <see cref="Camera"/>,
/// derives from this.
/// </summary>
/// <remarks>
/// Identity (<see cref="Id"/>, <see cref="Name"/>, <see cref="Network"/>) is fixed at creation.
/// State, such as <see cref="Powered"/>, is a plain property on the device or its kind, with a
/// public setter because no rule guards it yet. When one does, make that setter <c>internal</c>
/// and put the rule in a game action in this project: the compiler then lists every caller that
/// has to move to the action.
/// </remarks>
public abstract class Device
{
    public required DeviceId Id { get; init; }

    public required String Name { get; init; }

    public required NetworkId Network { get; init; }

    public Boolean Powered { get; set; } = true;
}
