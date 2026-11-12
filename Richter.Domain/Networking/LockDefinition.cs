namespace Richter.Domain.Networking;

/// <summary> The lock component of a <see cref="DeviceKind"/>, such as a door's. </summary>
public record LockDefinition
{
    /// <summary> Whether a new device of the kind starts locked. </summary>
    public required Boolean StartsLocked { get; init; }
}
