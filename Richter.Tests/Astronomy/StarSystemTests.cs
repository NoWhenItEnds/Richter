using Richter.Domain.Astronomy;

namespace Richter.Tests.Astronomy;

/// <summary>
/// Checks the frame and rotation conventions behind every position, which give plausible
/// positions even when they're wrong, and that the positions the system keeps don't go stale.
/// </summary>
public class StarSystemTests
{
    // Earth's pole is tilted away from the reference frame's, so that mixing up the two frames
    // shows.
    private const Double PoleLongitude = 30;
    private const Double PoleLatitude = 40;

    private readonly StarSystem _system = TestBodies.CreateSolarSystem(TestBodies.CreateEarth(PoleLongitude, PoleLatitude));

    [Fact]
    public void PutsTheNorthPoleAlongTheBodysPole()
    {
        Vector3D northPole = FromEarthsCentre(OnEarth(latitude: 90, longitude: 0), TestBodies.Epoch + TimeSpan.FromHours(5));

        AssertClose(TestBodies.EarthRadius * PoleDirection(), northPole);
    }

    [Fact]
    public void MeasuresLongitudeFromTheMarchEquinox()
    {
        // With the prime meridian at 0 at the epoch, longitude 0 on the equator points as nearly
        // at the reference direction (the March equinox) as the tilted equator allows: at right
        // angles to the pole, in the plane of the pole and that direction, and on its side.
        Vector3D primeMeridian = FromEarthsCentre(OnEarth(latitude: 0, longitude: 0), TestBodies.Epoch);
        Vector3D equinox = new Vector3D(1, 0, 0);

        Assert.Equal(0, Vector3D.Dot(primeMeridian, PoleDirection()), 0.001);
        Assert.Equal(0, Vector3D.Dot(primeMeridian, Vector3D.Cross(PoleDirection(), equinox)), 0.001);
        Assert.True(Vector3D.Dot(primeMeridian, equinox) > 0, $"Expected the prime meridian on the equinox side, but it was at {primeMeridian}.");
    }

    [Fact]
    public void TurnsEastwardOnceASiderealDay()
    {
        Vector3D primeMeridian = FromEarthsCentre(OnEarth(latitude: 0, longitude: 0), TestBodies.Epoch);
        Vector3D quarterEast = FromEarthsCentre(OnEarth(latitude: 0, longitude: 90), TestBodies.Epoch);

        // Spin follows the right-hand rule about the pole, so east is the pole crossed with the
        // prime meridian, and a quarter of a day turns the prime meridian to where 90° east was.
        AssertClose(Vector3D.Cross(PoleDirection(), primeMeridian), quarterEast);
        AssertClose(quarterEast, FromEarthsCentre(OnEarth(latitude: 0, longitude: 0), TestBodies.Epoch + TestBodies.SiderealDay / 4));
    }

    [Fact]
    public void PlacesASatelliteByItsNodeAndInclination()
    {
        // With the prime meridian at the equator's zero at the epoch, a satellite at its ascending
        // node is directly over the longitude equal to the node. With the periapsis 45° past the
        // node and the satellite 45° past the periapsis, it is a quarter of a circular orbit past
        // the node, at its furthest north: a sin i above the equator.
        const Double semiMajorAxis = 7_000_000;
        const Double inclination = 50;
        Orbit atTheNode = TestBodies.OrbitAroundEarth(semiMajorAxis, 0, inclination, longitudeOfAscendingNode: 40);
        Orbit quarterRound = atTheNode with { ArgumentOfPeriapsis = 45, MeanAnomalyAtEpoch = 45 };

        Vector3D overhead = FromEarthsCentre(new OrbitalLocation { Orbit = atTheNode }, TestBodies.Epoch);
        Vector3D furthestNorth = FromEarthsCentre(new OrbitalLocation { Orbit = quarterRound }, TestBodies.Epoch);

        AssertClose(semiMajorAxis / TestBodies.EarthRadius * FromEarthsCentre(OnEarth(latitude: 0, longitude: 40), TestBodies.Epoch), overhead);
        Assert.Equal(semiMajorAxis * Math.Sin(Double.DegreesToRadians(inclination)), Vector3D.Dot(furthestNorth, PoleDirection()), 0.001);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(20.0, true)]
    [InlineData(30.0, false)]
    [InlineData(180.0, false)]
    public void SeesASatelliteOnlyAboveTheHorizon(Double degreesEastOfTheStation, Boolean visible)
    {
        // From the equator, a satellite in an equatorial orbit at 7000 km sets about 24° away.
        OrbitalLocation satellite = new OrbitalLocation
        {
            Orbit = TestBodies.OrbitAroundEarth(7_000_000, 0, 0, meanAnomalyAtEpoch: degreesEastOfTheStation),
        };

        Boolean seen = _system.HasLineOfSight(OnEarth(latitude: 0, longitude: 0), satellite, TestBodies.Epoch);

        Assert.Equal(visible, seen);
    }

    [Fact]
    public void MovesBodiesOnWhenAskedAboutAnotherInstant()
    {
        // The system keeps the bodies' positions at the last instant it was asked about. If it
        // didn't notice a new instant, Earth would stay where it was.
        DateTimeOffset later = TestBodies.Epoch + TimeSpan.FromDays(100);
        StarSystem fresh = TestBodies.CreateSolarSystem(TestBodies.CreateEarth(PoleLongitude, PoleLatitude));

        Vector3D atEpoch = _system.PositionOf(TestBodies.Earth, TestBodies.Epoch);
        Vector3D atLater = _system.PositionOf(TestBodies.Earth, later);

        Assert.NotEqual(atEpoch, atLater);
        Assert.Equal(fresh.PositionOf(TestBodies.Earth, later), atLater);
    }

    private static SurfaceLocation OnEarth(Double latitude, Double longitude)
    {
        return new SurfaceLocation { Body = TestBodies.Earth, Latitude = latitude, Longitude = longitude, Altitude = 0 };
    }

    private static Vector3D PoleDirection()
    {
        Double longitude = Double.DegreesToRadians(PoleLongitude);
        Double latitude = Double.DegreesToRadians(PoleLatitude);

        return new Vector3D(Math.Cos(latitude) * Math.Cos(longitude), Math.Cos(latitude) * Math.Sin(longitude), Math.Sin(latitude));
    }

    private static void AssertClose(Vector3D expected, Vector3D actual)
    {
        Assert.True((actual - expected).Length < 0.001, $"Expected {expected}, but got {actual}.");
    }

    private Vector3D FromEarthsCentre(Location location, DateTimeOffset time)
    {
        return _system.PositionOf(location, time) - _system.PositionOf(TestBodies.Earth, time);
    }
}
