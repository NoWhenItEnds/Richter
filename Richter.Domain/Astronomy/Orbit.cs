namespace Richter.Domain.Astronomy;

/// <summary>
/// A Keplerian orbit: the elements that fix an object's path around its parent body, and where
/// along that path it is at any instant.
/// </summary>
/// <remarks>
/// <para>
/// Orbits are on rails: a fixed ellipse, followed at the speed Kepler's laws give. A position
/// comes from these elements and the time, not from simulating gravity, so it can be worked out
/// for any instant directly and is the same on every run. Nothing disturbs an orbit: not other
/// bodies, a planet's bulge or drag.
/// </para>
/// <para>
/// Angles are measured against <see cref="Plane"/>: the parent's equator for satellites, or the
/// system's reference plane for planets and the Moon, as their published elements are.
/// </para>
/// </remarks>
public record Orbit
{
    /// <summary> The body being orbited. Its gravitational parameter sets the period. </summary>
    public required CelestialBodyId Parent { get; init; }

    /// <summary> The plane the angles are measured against. </summary>
    public required OrbitPlane Plane { get; init; }

    /// <summary> Half the longest diameter of the orbit's ellipse, in metres. </summary>
    public required Double SemiMajorAxis
    {
        get;
        init
        {
            field = Require.Positive(value, nameof(SemiMajorAxis));
        }
    }

    /// <summary>
    /// How stretched the ellipse is: 0 is a circle, and it approaches 1 as the ellipse lengthens.
    /// At 1 or more the path is an escape trajectory, which isn't modelled.
    /// </summary>
    public required Double Eccentricity
    {
        get;
        init
        {
            if (!(value >= 0 && value < 1))
            {
                throw new ArgumentOutOfRangeException(nameof(Eccentricity), value, $"Eccentricity must be at least 0 and below 1, but is {value}.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Tilt of the orbit from <see cref="Plane"/>, in degrees from 0 to 180. Above 90 the object
    /// travels backwards (retrograde).
    /// </summary>
    public required Double Inclination
    {
        get;
        init
        {
            field = Require.Between(value, 0, 180, nameof(Inclination));
        }
    }

    /// <summary>
    /// Where the orbit rises through <see cref="Plane"/> (the ascending node), in degrees
    /// anticlockwise seen from above, from that plane's zero (see <see cref="OrbitPlane"/>).
    /// </summary>
    public required Double LongitudeOfAscendingNode
    {
        get;
        init
        {
            field = Require.Finite(value, nameof(LongitudeOfAscendingNode));
        }
    }

    /// <summary>
    /// Angle from the ascending node to the point closest to the parent (periapsis), in degrees,
    /// measured in the direction of travel.
    /// </summary>
    public required Double ArgumentOfPeriapsis
    {
        get;
        init
        {
            field = Require.Finite(value, nameof(ArgumentOfPeriapsis));
        }
    }

    /// <summary>
    /// How far round the orbit from periapsis the object was at <see cref="Epoch"/>, in degrees,
    /// as if it moved at a constant speed (the mean anomaly).
    /// </summary>
    public required Double MeanAnomalyAtEpoch
    {
        get;
        init
        {
            field = Require.Finite(value, nameof(MeanAnomalyAtEpoch));
        }
    }

    /// <summary> The instant these elements describe. </summary>
    public required DateTimeOffset Epoch { get; init; }
}
