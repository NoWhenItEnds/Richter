namespace Richter.Domain.Networking;

/// <summary>
/// Identifies a <see cref="DeviceKind"/> by the key content gives it, such as <c>door</c>.
/// </summary>
public readonly record struct DeviceKindId
{
    public DeviceKindId(String key)
    {
        if (String.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A device kind's id can't be empty.", nameof(key));
        }

        Key = key;
    }

    public String Key { get; }
}
