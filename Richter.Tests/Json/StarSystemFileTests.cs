using System.Globalization;
using Richter.Domain.Astronomy;
using Richter.Json;
using Richter.Json.Documents;

namespace Richter.Tests.Json;

/// <summary>
/// Checks the game's Solar System data and its conversion from published values against things
/// anyone can look up: where the Sun is overhead, the Moon facing Earth, the Moon's tilt, which
/// way the planets spin, and Mars at opposition.
/// </summary>
public class StarSystemFileTests
{
    private static readonly CelestialBodyId Sun = new CelestialBodyId("sun");
    private static readonly CelestialBodyId Earth = new CelestialBodyId("earth");
    private static readonly CelestialBodyId Moon = new CelestialBodyId("moon");
    private static readonly CelestialBodyId Mars = new CelestialBodyId("mars");

    [Theory]
    // On 1 January the Sun is over 23.0°S, and at 12:00 UTC it is about 0.8° east of Greenwich
    // because the equation of time is about -3 minutes. At the June solstice it is over 23.4°N and
    // about 0.4° east. The last case is a century on, where drift would show.
    [InlineData("2000-01-01T12:00:00Z", -23.0, 0.8)]
    [InlineData("2000-06-21T12:00:00Z", 23.4, 0.4)]
    [InlineData("2112-06-21T12:00:00Z", 23.4, 0.4)]
    public async Task PutsTheSunOverheadWhereItIsAtNoonInGreenwich(String time, Double latitude, Double longitude)
    {
        StarSystem system = await LoadSolarSystemAsync();
        DateTimeOffset at = Parse(time);
        Vector3D earth = system.PositionOf(Earth, at);
        Vector3D towardsSun = system.PositionOf(Sun, at) - earth;
        Vector3D subsolarPoint = system.PositionOf(On(Earth, latitude, longitude), at) - earth;

        Assert.InRange(AngleBetween(towardsSun, subsolarPoint), 0, 1);
    }

    [Theory]
    [InlineData("2000-01-01T12:00:00Z")]
    [InlineData("2050-07-01T00:00:00Z")]
    [InlineData("2112-01-01T00:00:00Z")]
    public async Task KeepsTheMoonsNearSideFacingEarth(String time)
    {
        // The Moon's prime meridian is defined by the mean direction of Earth. Libration and the
        // periodic terms left out of its spin move it by up to about 10°.
        StarSystem system = await LoadSolarSystemAsync();
        DateTimeOffset at = Parse(time);
        Vector3D moon = system.PositionOf(Moon, at);
        Vector3D nearSide = system.PositionOf(On(Moon, 0, 0), at) - moon;
        Vector3D towardsEarth = system.PositionOf(Earth, at) - moon;

        Assert.InRange(AngleBetween(nearSide, towardsEarth), 0, 15);
    }

    [Fact]
    public async Task TiltsTheMoonsOrbitByItsPublishedAngleToTheEcliptic()
    {
        // JPL publishes 5.16° against the ecliptic, and Earth's equator is tilted 23.4° from it, so
        // measuring against the wrong plane shows. The reference plane is the ecliptic, so the
        // Moon's angle above it is its z over its distance. Samples are two hours apart.
        StarSystem system = await LoadSolarSystemAsync();
        DateTimeOffset start = Parse("2000-01-01T12:00:00Z");

        Double highest = Enumerable.Range(0, 28 * 12).Max(step =>
        {
            DateTimeOffset at = start + TimeSpan.FromHours(2 * step);
            Vector3D fromEarth = system.PositionOf(Moon, at) - system.PositionOf(Earth, at);
            return Math.Abs(Double.RadiansToDegrees(Math.Asin(fromEarth.Z / fromEarth.Length)));
        });

        Assert.Equal(5.16, highest, 0.02);
    }

    [Theory]
    // Venus and Uranus spin backwards: seen from north of the ecliptic, clockwise. Uranus lies on
    // its side, so only a little of its spin shows from there, but that little is clockwise.
    [InlineData("earth", true)]
    [InlineData("venus", false)]
    [InlineData("uranus", false)]
    public async Task SpinsEachPlanetTheWayItReallyTurns(String key, Boolean anticlockwise)
    {
        StarSystem system = await LoadSolarSystemAsync();
        CelestialBodyId body = new CelestialBodyId(key);
        DateTimeOffset start = Parse("2000-01-01T12:00:00Z");
        DateTimeOffset hourLater = start + TimeSpan.FromHours(1);
        Vector3D before = system.PositionOf(On(body, 0, 0), start) - system.PositionOf(body, start);
        Vector3D after = system.PositionOf(On(body, 0, 0), hourLater) - system.PositionOf(body, hourLater);

        Assert.Equal(anticlockwise, Vector3D.Cross(before, after).Z > 0);
    }

    [Theory]
    // Mars was at opposition, opposite the Sun in Earth's sky, at about 17:56 UTC on 28 August
    // 2003 and about 02:32 UTC on 16 January 2025.
    [InlineData("2003-08-28T18:00:00Z")]
    [InlineData("2025-01-16T02:30:00Z")]
    public async Task PutsMarsOppositeTheSunAtOpposition(String time)
    {
        StarSystem system = await LoadSolarSystemAsync();
        DateTimeOffset at = Parse(time);
        Vector3D earth = system.PositionOf(Earth, at);
        Double sunLongitude = EclipticLongitude(system.PositionOf(Sun, at) - earth);
        Double marsLongitude = EclipticLongitude(system.PositionOf(Mars, at) - earth);

        Assert.InRange(Math.IEEERemainder(marsLongitude - sunLongitude - 180, 360), -1, 1);
    }

    private static async Task<StarSystem> LoadSolarSystemAsync()
    {
        await using Stream stream = typeof(StarSystemFileTests).Assembly.GetManifestResourceStream("solar-system.json")
            ?? throw new InvalidOperationException("The test project should embed solar-system.json.");
        FromFile<StarSystemDocument> file = await ContentFile.ReadAsync(
            stream,
            "Mods/core/star-systems/solar-system.json",
            ContentJsonContext.Default.StarSystemDocument,
            TestContext.Current.CancellationToken);

        return StarSystemFile.Build([file], "star-systems/solar-system.json");
    }

    private static DateTimeOffset Parse(String time)
    {
        return DateTimeOffset.Parse(time, CultureInfo.InvariantCulture);
    }

    private static SurfaceLocation On(CelestialBodyId body, Double latitude, Double longitude)
    {
        return new SurfaceLocation { Body = body, Latitude = latitude, Longitude = longitude, Altitude = 0 };
    }

    private static Double AngleBetween(Vector3D a, Vector3D b)
    {
        return Double.RadiansToDegrees(Math.Acos(Math.Clamp(Vector3D.Dot(a, b) / (a.Length * b.Length), -1, 1)));
    }

    private static Double EclipticLongitude(Vector3D direction)
    {
        return Double.RadiansToDegrees(Math.Atan2(direction.Y, direction.X));
    }
}
