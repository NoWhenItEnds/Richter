using Richter.Domain.Astronomy;
using Richter.Domain.Networking;

namespace Richter.Tests.Networking;

/// <summary>
/// Checks what goes wrong quietly in <see cref="NetworkGraph"/>: indexes falling out of step,
/// where routing would use a link that no longer exists, and link delays and horizons, which give
/// plausible numbers even when they're wrong.
/// </summary>
public class NetworkGraphTests
{
    private static readonly DeviceKind GatewayKind = new DeviceKind { Id = new DeviceKindId("gateway"), Name = "Gateway" };
    private static readonly DeviceKind DoorKind = new DeviceKind
    {
        Id = new DeviceKindId("door"),
        Name = "Door",
        Lock = new LockDefinition { StartsLocked = true },
    };

    private readonly NetworkGraph _graph = new NetworkGraph(TestBodies.CreateSolarSystem(TestBodies.CreateEarth()), [GatewayKind, DoorKind]);

    [Fact]
    public void RemovingANetworkRemovesItsDevicesAndLinksFromBothEnds()
    {
        Network removed = AddNetwork(Sydney);
        Network neighbour = AddNetwork(Sydney);
        Network farther = AddNetwork(Sydney);
        _graph.AddDevice(new Device(_graph.NewDeviceId(), DoorKind, "Door", removed.Id));
        _graph.AddLink(NewLink(removed.Id, neighbour.Id, LinkMedium.Cable));
        Link remaining = NewLink(neighbour.Id, farther.Id, LinkMedium.Cable);
        _graph.AddLink(remaining);

        _graph.RemoveNetwork(removed.Id);

        Assert.DoesNotContain(_graph.Devices, device => device.Network == removed.Id);
        Assert.Same(remaining, Assert.Single(_graph.Links));
        Assert.Same(remaining, Assert.Single(_graph.GetLinks(neighbour.Id)));
    }

    [Fact]
    public void RemovingALinkRemovesItFromBothNetworks()
    {
        Network first = AddNetwork(Sydney);
        Network second = AddNetwork(Sydney);
        Link link = NewLink(first.Id, second.Id, LinkMedium.Cable);
        _graph.AddLink(link);

        _graph.RemoveLink(link.Id);

        Assert.Empty(_graph.GetLinks(first.Id));
        Assert.Empty(_graph.GetLinks(second.Id));
    }

    [Fact]
    public void DelaysACableByItsLengthAlongTheGround()
    {
        // Sydney to London is 16,994 km along the surface, which at two-thirds of the speed of
        // light takes 85.0 ms. Straight through the Earth would be 12,386 km and 62.0 ms.
        Network sydney = AddNetwork(Sydney);
        Network london = AddNetwork(Ground(51.5074, -0.1278));
        Link cable = NewLink(sydney.Id, london.Id, LinkMedium.Cable);
        _graph.AddLink(cable);

        TimeSpan? delay = _graph.DelayOf(cable.Id, TestBodies.Epoch);

        Assert.NotNull(delay);
        Assert.Equal(85.03, delay.Value.TotalMilliseconds, 0.01);
    }

    [Fact]
    public void DelaysARadioLinkByTheDistanceOverTheSpeedOfLight()
    {
        // A satellite at 7000 km from the centre, overhead a station on the 6371 km Earth, is
        // 629 km away: 2.098 ms.
        Network station = AddNetwork(Ground(0, 0));
        Network satellite = AddNetwork(EquatorialOrbit(degreesEast: 0));
        Link radio = NewLink(station.Id, satellite.Id, LinkMedium.Radio);
        _graph.AddLink(radio);

        TimeSpan? delay = _graph.DelayOf(radio.Id, TestBodies.Epoch);

        Assert.NotNull(delay);
        Assert.Equal(2.098, delay.Value.TotalMilliseconds, 0.001);
    }

    [Theory]
    // From the equator, a satellite at 7000 km in an equatorial orbit is 12° up when it is 15°
    // east, 5° up at 20° (in sight, but too low), and below the horizon beyond about 25°.
    [InlineData(0.0, true)]
    [InlineData(15.0, true)]
    [InlineData(20.0, false)]
    [InlineData(180.0, false)]
    public void KeepsARadioLinkDownUntilTheSatelliteIsTenDegreesUp(Double degreesEast, Boolean up)
    {
        Network station = AddNetwork(Ground(0, 0));
        Network satellite = AddNetwork(EquatorialOrbit(degreesEast));
        Link radio = NewLink(station.Id, satellite.Id, LinkMedium.Radio);
        _graph.AddLink(radio);

        Assert.Equal(up, _graph.DelayOf(radio.Id, TestBodies.Epoch).HasValue);
    }

    [Theory]
    // Two satellites at 7000 km have no horizon, only the Earth between them. The line between
    // them passes 7000 cos(half the gap) km from the centre: 6578 km at a 40° gap, which clears
    // the 6371 km Earth, and 6062 km at 60°, which doesn't.
    [InlineData(40.0, true)]
    [InlineData(60.0, false)]
    [InlineData(180.0, false)]
    public void KeepsABeamBetweenSatellitesDownWhileTheEarthIsInTheWay(Double degreesApart, Boolean up)
    {
        Network first = AddNetwork(EquatorialOrbit(degreesEast: 0));
        Network second = AddNetwork(EquatorialOrbit(degreesApart));
        Link laser = NewLink(first.Id, second.Id, LinkMedium.Laser);
        _graph.AddLink(laser);

        Assert.Equal(up, _graph.DelayOf(laser.Id, TestBodies.Epoch).HasValue);
    }

    [Theory]
    // Two stations 20° apart on the equator: 2224 km of cable takes 11.13 ms. A satellite at
    // 7000 km halfway between them is 1323 km from each and 23° up, so going through it takes
    // 8.83 ms: more hops, but faster. Behind the Earth the satellite can't be reached.
    [InlineData(10.0, true)]
    [InlineData(190.0, false)]
    public void RoutesOverTheFastestLinksThatAreUp(Double satelliteDegreesEast, Boolean throughTheSatellite)
    {
        Network west = AddNetwork(Ground(0, 0));
        Network east = AddNetwork(Ground(0, 20));
        Network satellite = AddNetwork(EquatorialOrbit(satelliteDegreesEast));
        Link cable = NewLink(west.Id, east.Id, LinkMedium.Cable);
        Link upward = NewLink(west.Id, satellite.Id, LinkMedium.Radio);
        Link downward = NewLink(satellite.Id, east.Id, LinkMedium.Radio);
        _graph.AddLink(cable);
        _graph.AddLink(upward);
        _graph.AddLink(downward);

        Route? route = _graph.FindRoute(west.Id, east.Id, TestBodies.Epoch);

        Assert.NotNull(route);
        LinkId[] expectedLinks = throughTheSatellite ? [upward.Id, downward.Id] : [cable.Id];
        Assert.Equal(expectedLinks, route.Links);
        Assert.Equal(throughTheSatellite ? 8.83 : 11.13, route.Delay.TotalMilliseconds, 0.01);
    }

    [Theory]
    // A radio and a laser link between the same two networks have exactly the same delay. They're
    // added to the graph in the opposite order to their IDs, so only the IDs can decide.
    [InlineData(LinkMedium.Radio, LinkMedium.Laser)]
    [InlineData(LinkMedium.Laser, LinkMedium.Radio)]
    public void TakesTheEarlierLinkWhenTwoRoutesAreEqual(LinkMedium earlierMedium, LinkMedium laterMedium)
    {
        Network station = AddNetwork(Ground(0, 0));
        Network satellite = AddNetwork(EquatorialOrbit(degreesEast: 0));
        Link earlier = NewLink(station.Id, satellite.Id, earlierMedium);
        Link later = NewLink(station.Id, satellite.Id, laterMedium);
        _graph.AddLink(later);
        _graph.AddLink(earlier);

        Route? route = _graph.FindRoute(station.Id, satellite.Id, TestBodies.Epoch);

        Assert.NotNull(route);
        Assert.Equal([earlier.Id], route.Links);
    }

    [Fact]
    public void FindsNoRouteWhileEveryLinkThereIsDown()
    {
        Network station = AddNetwork(Ground(0, 0));
        Network satellite = AddNetwork(EquatorialOrbit(degreesEast: 180));
        _graph.AddLink(NewLink(station.Id, satellite.Id, LinkMedium.Radio));

        Assert.Null(_graph.FindRoute(station.Id, satellite.Id, TestBodies.Epoch));
    }

    private static SurfaceLocation Sydney => Ground(-33.8688, 151.2093);

    private static SurfaceLocation Ground(Double latitude, Double longitude)
    {
        return new SurfaceLocation { Body = TestBodies.Earth, Latitude = latitude, Longitude = longitude, Altitude = 0 };
    }

    private static OrbitalLocation EquatorialOrbit(Double degreesEast)
    {
        return new OrbitalLocation { Orbit = TestBodies.OrbitAroundEarth(7_000_000, 0, 0, meanAnomalyAtEpoch: degreesEast) };
    }

    private Link NewLink(NetworkId a, NetworkId b, LinkMedium medium)
    {
        return new Link { Id = _graph.NewLinkId(), NetworkA = a, NetworkB = b, Medium = medium };
    }

    private Network AddNetwork(Location location)
    {
        Network network = new Network { Id = _graph.NewNetworkId(), Name = "Network", Location = location };
        _graph.AddNetwork(network, new Device(_graph.NewDeviceId(), GatewayKind, "Gateway", network.Id));
        return network;
    }
}
