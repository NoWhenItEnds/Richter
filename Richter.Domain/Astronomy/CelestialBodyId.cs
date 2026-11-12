namespace Richter.Domain.Astronomy;

/// <summary>
/// Identifies a <see cref="CelestialBody"/> by the key its star system file gives it, such as
/// <c>earth</c>.
/// </summary>
/// <remarks>
/// Bodies only come from content, so the key a modder writes is the ID: scenarios refer to bodies
/// by it, and errors can name it. Keys are compared exactly, so <c>Earth</c> and <c>earth</c> are
/// different bodies.
/// </remarks>
public readonly record struct CelestialBodyId
{
    public CelestialBodyId(String key)
    {
        if (String.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A body's id can't be empty.", nameof(key));
        }

        Key = key;
    }

    public String Key { get; }
}
