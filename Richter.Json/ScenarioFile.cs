using System.Text.Json;
using Richter.Domain;
using Richter.Domain.Astronomy;
using Richter.Domain.Networking;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// Reads a scenario file such as <c>Data/scenario.json</c>, and starts a <see cref="World"/> from
/// it: its start time, and the networks, devices and links in place when play begins.
/// </summary>
/// <remarks>
/// Everything goes in through <see cref="NetworkGraph"/>, so a scenario meets the same rules as
/// play, and a broken rule is reported with where it is in the file.
/// </remarks>
public static class ScenarioFile
{
    /// <summary>
    /// Reads a scenario and starts a world in <paramref name="starSystem"/>. Throws
    /// <see cref="InvalidDataException"/>, with a message a player or modder can act on, if the
    /// file isn't a valid scenario.
    /// </summary>
    public static async Task<World> ReadAsync(Stream stream, StarSystem starSystem, CancellationToken cancellationToken)
    {
        ScenarioDocument? document;

        try
        {
            document = await JsonSerializer.DeserializeAsync(stream, ContentJsonContext.Default.ScenarioDocument, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw ContentErrors.From(exception);
        }

        if (document is null)
        {
            throw new InvalidDataException("The file holds null instead of a scenario.");
        }

        DateTimeOffset startTime = InstantText.Parse(document.StartTime, "startTime");
        World world = new World(starSystem, startTime);
        Dictionary<String, Double> gravitationalParameters = starSystem.Bodies.ToDictionary(body => body.Id.Key, body => body.GravitationalParameter);
        Dictionary<String, NetworkId> networkIds = new Dictionary<String, NetworkId>();

        foreach ((Int32 index, NetworkDocument network) in (document.Networks ?? []).Index())
        {
            String where = $"networks[{index}] ({network.Name})";

            if (networkIds.ContainsKey(network.Id))
            {
                throw new InvalidDataException($"{where}: another network already has the id \"{network.Id}\".");
            }

            networkIds.Add(network.Id, world.Networks.NewNetworkId());
            AddNetwork(world.Networks, network, networkIds[network.Id], where, startTime, gravitationalParameters);
        }

        foreach ((Int32 index, LinkDocument link) in (document.Links ?? []).Index())
        {
            AddLink(world.Networks, link, $"links[{index}]", networkIds);
        }

        return world;
    }

    private static void AddNetwork(NetworkGraph graph, NetworkDocument document, NetworkId id, String where, DateTimeOffset startTime, Dictionary<String, Double> gravitationalParameters)
    {
        // The domain checks every value and rule; this puts the network's place in the file on
        // what it finds.
        try
        {
            Location location = (document.Surface, document.Orbit) switch
            {
                (SurfaceDocument surface, null) => new SurfaceLocation
                {
                    Body = new CelestialBodyId(surface.Body),
                    Latitude = surface.Latitude,
                    Longitude = surface.Longitude,
                    Altitude = surface.Altitude * ContentUnits.MetresPerKilometre,
                },
                (null, OrbitDocument orbit) => new OrbitalLocation { Orbit = OrbitConversion.ToOrbit(orbit, where, startTime, gravitationalParameters) },
                _ => throw new InvalidDataException($"{where}: a network needs either a surface or an orbit, not both or neither."),
            };

            graph.AddNetwork(
                new Network { Id = id, Name = document.Name, Location = location },
                new Gateway { Id = graph.NewDeviceId(), Name = document.Gateway ?? $"{document.Name} gateway", Network = id });

            foreach ((Int32 index, DeviceDocument device) in (document.Devices ?? []).Index())
            {
                graph.AddDevice(ToDevice(device, graph.NewDeviceId(), id, $"{where}.devices[{index}] ({device.Name})"));
            }
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{where}: {ContentErrors.Describe(exception)}", exception);
        }
    }

    private static Device ToDevice(DeviceDocument document, DeviceId id, NetworkId network, String where)
    {
        if (document.Kind != "door" && document.Locked is not null)
        {
            throw new InvalidDataException($"{where}: only a door can be locked.");
        }

        Device device = document.Kind switch
        {
            "workstation" => new Workstation { Id = id, Name = document.Name, Network = network },
            "camera" => new Camera { Id = id, Name = document.Name, Network = network },
            "door" => new Door
            {
                Id = id,
                Name = document.Name,
                Network = network,
                Locked = document.Locked ?? throw new InvalidDataException($"{where}: a door needs \"locked\": true or false."),
            },
            _ => throw new InvalidDataException($"{where}: kind is \"{document.Kind}\", but must be \"workstation\", \"camera\" or \"door\"."),
        };
        device.Powered = document.Powered ?? true;

        return device;
    }

    private static void AddLink(NetworkGraph graph, LinkDocument document, String where, Dictionary<String, NetworkId> networkIds)
    {
        if (document.Between.Count != 2)
        {
            throw new InvalidDataException($"{where}: between needs exactly two network ids, but has {document.Between.Count}.");
        }

        LinkMedium medium = document.Medium switch
        {
            "cable" => LinkMedium.Cable,
            "radio" => LinkMedium.Radio,
            "laser" => LinkMedium.Laser,
            _ => throw new InvalidDataException($"{where}: medium is \"{document.Medium}\", but must be \"cable\", \"radio\" or \"laser\"."),
        };
        NetworkId[] ends = document.Between
            .Select(key => networkIds.TryGetValue(key, out NetworkId id) ? id : throw new InvalidDataException($"{where}: there's no network with the id \"{key}\"."))
            .ToArray();

        try
        {
            graph.AddLink(new Link { Id = graph.NewLinkId(), NetworkA = ends[0], NetworkB = ends[1], Medium = medium });
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{where}: {ContentErrors.Describe(exception)}", exception);
        }
    }
}
