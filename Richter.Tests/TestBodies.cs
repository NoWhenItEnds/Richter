using Richter.Domain.Astronomy;

namespace Richter.Tests;

/// <summary>
/// The Sun and Earth for tests. Earth's gravity and size are real. Its spin is simplified: the
/// prime meridian faces the March equinox at <see cref="Epoch"/>, and the pole can be tilted so
/// that tests show code using Earth's own frame.
/// </summary>
internal static class TestBodies
{
    public const Double EarthGravitationalParameter = 3.986004418e14;
    public const Double EarthRadius = 6_371_000;

    public static readonly CelestialBodyId Sun = new CelestialBodyId("sun");
    public static readonly CelestialBodyId Earth = new CelestialBodyId("earth");
    public static readonly DateTimeOffset Epoch = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly TimeSpan SiderealDay = TimeSpan.FromSeconds(86_164.0905);

    public static CelestialBody CreateEarth(Double poleLongitude = 0, Double poleLatitude = 90)
    {
        return new CelestialBody
        {
            Id = Earth,
            Name = "Earth",
            GravitationalParameter = EarthGravitationalParameter,
            Radius = EarthRadius,
            Orbit = new Orbit
            {
                Parent = Sun,
                Plane = OrbitPlane.ReferencePlane,
                SemiMajorAxis = 149_597_870_700,
                Eccentricity = 0,
                Inclination = 0,
                LongitudeOfAscendingNode = 0,
                ArgumentOfPeriapsis = 0,
                MeanAnomalyAtEpoch = 0,
                Epoch = Epoch,
            },
            Rotation = Spin(poleLongitude, poleLatitude, SiderealDay),
        };
    }

    public static StarSystem CreateSolarSystem(CelestialBody earth)
    {
        CelestialBody sun = new CelestialBody
        {
            Id = Sun,
            Name = "Sun",
            GravitationalParameter = 1.32712440041e20,
            Radius = 695_700_000,
            Orbit = null,
            Rotation = Spin(0, 90, TimeSpan.FromDays(25.38)),
        };

        return new StarSystem([sun, earth]);
    }

    public static Orbit OrbitAroundEarth(
        Double semiMajorAxis,
        Double eccentricity,
        Double inclination,
        Double longitudeOfAscendingNode = 0,
        Double argumentOfPeriapsis = 0,
        Double meanAnomalyAtEpoch = 0)
    {
        return new Orbit
        {
            Parent = Earth,
            Plane = OrbitPlane.ParentEquator,
            SemiMajorAxis = semiMajorAxis,
            Eccentricity = eccentricity,
            Inclination = inclination,
            LongitudeOfAscendingNode = longitudeOfAscendingNode,
            ArgumentOfPeriapsis = argumentOfPeriapsis,
            MeanAnomalyAtEpoch = meanAnomalyAtEpoch,
            Epoch = Epoch,
        };
    }

    private static Rotation Spin(Double poleLongitude, Double poleLatitude, TimeSpan siderealPeriod)
    {
        return new Rotation
        {
            PoleLongitude = poleLongitude,
            PoleLatitude = poleLatitude,
            PrimeMeridianAtEpoch = 0,
            SiderealPeriod = siderealPeriod,
            Epoch = Epoch,
        };
    }
}
