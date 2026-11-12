using System.Text;
using Richter.Domain;
using Richter.Domain.Astronomy;
using Richter.Domain.Networking;
using Richter.Json;

namespace Richter.Tests.Json;

/// <summary>
/// Checks what a misread scenario would get quietly wrong: a start time read in the wrong time zone,
/// distances in the wrong unit, or a link joining the wrong networks. Each still starts a game.
/// </summary>
public class ScenarioFileTests
{
    private static readonly DateTimeOffset NewYear2112 = new DateTimeOffset(2112, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("2112-01-01T00:00:00Z")]
    [InlineData("2112-01-01T10:00:00+10:00")]
    // Without an offset the time is UTC. Read in the machine's own time zone instead, this would
    // fail on any machine not set to UTC.
    [InlineData("2112-01-01T00:00:00")]
    [InlineData("2112-01-01")]
    public async Task ReadsTheStartTimeAsTheSameInstantOnEveryMachine(String startTime)
    {
        World world = await ReadAsync($$"""{ "startTime": "{{startTime}}" }""");

        Assert.Equal(NewYear2112, world.Time);
    }

    [Fact]
    public async Task RefusesADateThatMeansDifferentDaysInDifferentCountries()
    {
        // 01/02/2112 is 1 February in Australia and 2 January in the United States.
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync("""{ "startTime": "01/02/2112" }"""));
    }

    [Fact]
    public async Task PlacesNetworksAndLinksAsWritten()
    {
        World world = await ReadAsync("""
            {
              "startTime": "2112-01-01T00:00:00Z",
              "networks": [
                { "id": "hill", "name": "Hill", "surface": { "body": "earth", "latitude": 10, "longitude": 20, "altitude": 0.5 } },
                { "id": "middle", "name": "Middle", "surface": { "body": "earth", "latitude": 0, "longitude": 0, "altitude": 0 } },
                {
                  "id": "relay",
                  "name": "Relay",
                  "orbit": {
                    "parent": "earth", "plane": "parentEquator", "semiMajorAxis": 7000, "eccentricity": 0,
                    "inclination": 0, "longitudeOfAscendingNode": 0, "argumentOfPeriapsis": 0, "meanAnomaly": 0
                  }
                }
              ],
              "links": [ { "between": ["relay", "hill"], "medium": "radio" } ]
            }
            """);

        // Distances in the file are in km, and the link joins the two networks it names.
        Network hill = Assert.Single(world.Networks.Networks, network => network.Name == "Hill");
        Network relay = Assert.Single(world.Networks.Networks, network => network.Name == "Relay");
        Assert.Equal(500, Assert.IsType<SurfaceLocation>(hill.Location).Altitude);
        Assert.Equal(7_000_000, Assert.IsType<OrbitalLocation>(relay.Location).Orbit.SemiMajorAxis);
        Link link = Assert.Single(world.Networks.Links);
        Assert.Equal((relay.Id, hill.Id), (link.NetworkA, link.NetworkB));
    }

    private static Task<World> ReadAsync(String json)
    {
        return ScenarioFile.ReadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(json)),
            TestBodies.CreateSolarSystem(TestBodies.CreateEarth()),
            TestContext.Current.CancellationToken);
    }
}
