namespace Richter.Domain.Astronomy;

/// <summary>
/// How a body spins, which turns a latitude and longitude on it into a direction in the reference
/// frame.
/// </summary>
/// <remarks>
/// A simplified form of the IAU rotation model: the pole stays fixed and the spin rate is
/// constant, so there is no precession. The pole is the one the body turns anticlockwise around
/// when seen from above it (the right-hand rule), so the period is always positive. For bodies
/// that spin backwards, such as Venus and Uranus, that is the pole the IAU calls south.
/// </remarks>
public record Rotation
{
    /// <summary>
    /// Direction of the pole, as a longitude on the reference plane in degrees, anticlockwise from
    /// the reference direction. In the Solar System this is ecliptic longitude.
    /// </summary>
    public required Double PoleLongitude
    {
        get;
        init
        {
            field = Require.Finite(value, nameof(PoleLongitude));
        }
    }

    /// <summary>
    /// Direction of the pole, as degrees above the reference plane, from -90 to 90. In the Solar
    /// System this is ecliptic latitude: Earth's pole is at about 66.6°.
    /// </summary>
    public required Double PoleLatitude
    {
        get;
        init
        {
            field = Require.Between(value, -90, 90, nameof(PoleLatitude));
        }
    }

    /// <summary>
    /// Where the prime meridian (longitude 0) faced at <see cref="Epoch"/>, in degrees: the angle
    /// along the equator, in the direction of spin, from the reference direction projected onto
    /// the equator. For Earth this is Greenwich mean sidereal time as an angle.
    /// </summary>
    public required Double PrimeMeridianAtEpoch
    {
        get;
        init
        {
            field = Require.Finite(value, nameof(PrimeMeridianAtEpoch));
        }
    }

    /// <summary>
    /// Time for one full turn relative to the stars. Earth's is about 23 hours 56 minutes, not
    /// 24 hours, which is the turn relative to the Sun.
    /// </summary>
    public required TimeSpan SiderealPeriod
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(SiderealPeriod));
            field = value;
        }
    }

    /// <summary> The instant <see cref="PrimeMeridianAtEpoch"/> describes. </summary>
    public required DateTimeOffset Epoch { get; init; }
}
