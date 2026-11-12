namespace Richter.Domain.Astronomy;

/// <summary> A point fixed to a body, which turns with it. </summary>
/// <remarks>
/// Anything that stays over one spot can use this at any altitude, including a geostationary
/// satellite. Something that moves across the sky needs an <see cref="OrbitalLocation"/>.
/// </remarks>
public record SurfaceLocation : Location
{
    public required CelestialBodyId Body { get; init; }

    /// <summary> Degrees north of the equator, from -90 to 90. </summary>
    public required Double Latitude
    {
        get;
        init
        {
            field = Require.Between(value, -90, 90, nameof(Latitude));
        }
    }

    /// <summary> Degrees east of the prime meridian, from -180 to 180. </summary>
    public required Double Longitude
    {
        get;
        init
        {
            field = Require.Between(value, -180, 180, nameof(Longitude));
        }
    }

    /// <summary> Metres above the body's <see cref="CelestialBody.Radius"/>. </summary>
    public required Double Altitude
    {
        get;
        init
        {
            field = Require.Finite(value, nameof(Altitude));
        }
    }
}
