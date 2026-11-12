namespace Richter.Json.Documents;

/// <summary> The settings of a lock component. </summary>
internal record LockDocument
{
    /// <summary>
    /// On a device kind, whether new devices start locked; left out, they don't. On a device,
    /// whether it starts locked; left out, it's as its kind says.
    /// </summary>
    public Boolean? Locked { get; init; }
}
