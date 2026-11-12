using System.Text;
using Richter.Domain;
using Richter.Domain.Astronomy;
using Richter.Domain.Networking;
using Richter.Json;
using Richter.Json.Documents;

namespace Richter.Tests.Json;

/// <summary>
/// Checks what a misread scenario would get quietly wrong: a start time read in the wrong time zone,
/// distances in the wrong unit, a link joining the wrong networks, or a device's own settings
/// ignored in favour of its kind's. Each still starts a game.
/// </summary>
public class ScenarioFileTests
{
    private static readonly DateTimeOffset NewYear2112 = new DateTimeOffset(2112, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<DeviceKind> DeviceKinds =
    [
        new DeviceKind { Id = new DeviceKindId("gateway"), Name = "Gateway" },
        new DeviceKind { Id = new DeviceKindId("door"), Name = "Door", Lock = new LockDefinition { StartsLocked = true } },
    ];

    [Theory]
    [InlineData("2112-01-01T00:00:00Z")]
    [InlineData("2112-01-01T10:00:00+10:00")]
    // Without an offset the time is UTC. Read in the machine's own time zone instead, this would
    // fail on any machine not set to UTC.
    [InlineData("2112-01-01T00:00:00")]
    [InlineData("2112-01-01")]
    public async Task ReadsTheStartTimeAsTheSameInstantOnEveryMachine(String startTime)
    {
        World world = await BuildAsync($$"""{ "startTime": "{{startTime}}" }""");

        Assert.Equal(NewYear2112, world.Time);
    }

    [Fact]
    public async Task RefusesADateThatMeansDifferentDaysInDifferentCountries()
    {
        // 01/02/2112 is 1 February in Australia and 2 January in the United States.
        await Assert.ThrowsAsync<InvalidDataException>(() => BuildAsync("""{ "startTime": "01/02/2112" }"""));
    }

    [Fact]
    public async Task PlacesNetworksAndLinksAsWritten()
    {
        World world = await BuildAsync("""
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
              "links": [ { "id": "uplink", "between": ["relay", "hill"], "medium": "radio" } ]
            }
            """);

        // Distances in the file are in km, and the link joins the two networks it names.
        Network hill = Assert.Single(world.Networks.Networks, network => network.Key == "hill");
        Network relay = Assert.Single(world.Networks.Networks, network => network.Key == "relay");
        Assert.Equal(500, Assert.IsType<SurfaceLocation>(hill.Location).Altitude);
        Assert.Equal(7_000_000, Assert.IsType<OrbitalLocation>(relay.Location).Orbit.SemiMajorAxis);
        Link link = Assert.Single(world.Networks.Links);
        Assert.Equal((relay.Id, hill.Id), (link.NetworkA, link.NetworkB));
    }

    [Fact]
    public async Task AppliesADevicesOwnSettingsOverItsKinds()
    {
        // Doors start locked by their kind; the second door's own setting unlocks it.
        World world = await BuildAsync("""
            {
              "startTime": "2112-01-01T00:00:00Z",
              "networks": [
                {
                  "id": "base", "name": "Base", "surface": { "body": "earth", "latitude": 0, "longitude": 0, "altitude": 0 },
                  "devices": [
                    { "kind": "door", "name": "Kept" },
                    { "kind": "door", "name": "Unlocked", "components": { "lock": { "locked": false } } }
                  ]
                }
              ]
            }
            """);

        Assert.True(Assert.Single(world.Networks.Devices, device => device.Name == "Kept").Lock?.Locked);
        Assert.False(Assert.Single(world.Networks.Devices, device => device.Name == "Unlocked").Lock?.Locked);
    }

    /// <param name="files"> Each mod's copy of the scenario, in load order. </param>
    private static async Task<World> BuildAsync(params String[] files)
    {
        List<FromFile<ScenarioDocument>> documents = [];

        foreach ((Int32 index, String json) in files.Index())
        {
            documents.Add(await ContentFile.ReadAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(json)),
                $"Mods/mod{index}/scenarios/test.json",
                ContentJsonContext.Default.ScenarioDocument,
                TestContext.Current.CancellationToken));
        }

        return ScenarioFile.Build(documents, "scenarios/test.json", TestBodies.CreateSolarSystem(TestBodies.CreateEarth()), DeviceKinds);
    }
}
