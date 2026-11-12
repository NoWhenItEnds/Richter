namespace Richter.Domain.Networking;

/// <summary> A door whose lock is controlled over the network. </summary>
public class Door : Device
{
    public required Boolean Locked { get; set; }
}
