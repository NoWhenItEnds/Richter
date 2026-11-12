namespace Richter.Domain.Networking;

/// <summary>
/// A kind of device, such as a door or a camera, as content defines it: its name and the
/// components its devices have.
/// </summary>
/// <remarks>
/// Kinds are data, so a mod can add one without code. What a device can do comes from its
/// components, each an optional property here and on <see cref="Device"/>, null when the kind
/// doesn't have it. A new component needs code: a definition here, the device's state on
/// <see cref="Device"/>, and its document in <c>Richter.Json</c>.
/// </remarks>
public record DeviceKind
{
    public required DeviceKindId Id { get; init; }

    public required String Name { get; init; }

    /// <summary> The lock that devices of this kind have, or null if they have none. </summary>
    public LockDefinition? Lock { get; init; }
}
