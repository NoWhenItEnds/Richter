namespace Richter.Domain.Networking;

/// <summary> A device's lock, which it has when its kind has a <see cref="LockDefinition"/>. </summary>
public class DeviceLock
{
    public Boolean Locked { get; set; }
}
