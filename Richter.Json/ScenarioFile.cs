using Richter.Domain;
using Richter.Domain.Astronomy;
using Richter.Domain.Networking;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// Starts a <see cref="World"/> from every mod's copy of a scenario: its start time, and the
/// networks, devices and links in place when play begins.
/// </summary>
/// <remarks>
/// Everything goes in through <see cref="NetworkGraph"/>, so a scenario meets the same rules as
/// play, and a broken rule is reported with the file and entry it came from.
/// </remarks>
internal static class ScenarioFile
{
    /// <summary> The kind of device a network's gateway is when its file doesn't say. </summary>
    internal const String DefaultGatewayKind = "gateway";

    /// <summary>
    /// Merges the files and starts a world in <paramref name="starSystem"/>. Throws
    /// <see cref="InvalidDataException"/>, with a message a player or modder can act on, if the
    /// result isn't a valid scenario.
    /// </summary>
    /// <param name="files"> Every mod's copy, in load order. </param>
    /// <param name="path"> The path inside a mod, such as <c>scenarios/default.json</c>, for errors. </param>
    /// <param name="starSystem"> The star system the scenario names. </param>
    /// <param name="deviceKinds"> Every kind of device there can be. </param>
    internal static World Build(IReadOnlyList<FromFile<ScenarioDocument>> files, String path, StarSystem starSystem, IReadOnlyList<DeviceKind> deviceKinds)
    {
        FromFile<String> startTimeText = ContentMerge.Last(files, file => file.StartTime)
            ?? throw new InvalidDataException($"No mod's {path} sets startTime.");
        DateTimeOffset startTime = InstantText.Parse(startTimeText.Value, $"{startTimeText.File}: startTime");
        World world = new World(starSystem, deviceKinds, startTime);
        Dictionary<String, Double> gravitationalParameters = starSystem.Bodies.ToDictionary(body => body.Id.Key, body => body.GravitationalParameter);
        Dictionary<String, DeviceKind> kinds = deviceKinds.ToDictionary(kind => kind.Id.Key);
        Dictionary<String, NetworkId> networkIds = new Dictionary<String, NetworkId>();

        foreach (FromFile<NetworkDocument> network in ContentMerge.ById(files, file => file.Networks, network => network.Id, "networks"))
        {
            NetworkId id = world.Networks.NewNetworkId();
            networkIds.Add(network.Value.Id, id);
            AddNetwork(world.Networks, network.Value, id, $"{network.File}, network \"{network.Value.Id}\"", startTime, gravitationalParameters, kinds);
        }

        foreach (FromFile<LinkDocument> link in ContentMerge.ById(files, file => file.Links, link => link.Id, "links"))
        {
            AddLink(world.Networks, link.Value, $"{link.File}, link \"{link.Value.Id}\"", networkIds);
        }

        return world;
    }

    private static void AddNetwork(
        NetworkGraph graph,
        NetworkDocument document,
        NetworkId id,
        String where,
        DateTimeOffset startTime,
        Dictionary<String, Double> gravitationalParameters,
        Dictionary<String, DeviceKind> kinds)
    {
        // The domain checks every value and rule; this puts the network's place on what it finds.
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
            DeviceDocument gateway = document.Gateway ?? new DeviceDocument { Kind = DefaultGatewayKind, Name = $"{document.Name} gateway" };

            graph.AddNetwork(
                new Network { Id = id, Key = document.Id, Name = document.Name, Location = location },
                ToDevice(gateway, graph.NewDeviceId(), id, kinds, $"{where}, gateway"));

            foreach ((Int32 index, DeviceDocument device) in (document.Devices ?? []).Index())
            {
                graph.AddDevice(ToDevice(device, graph.NewDeviceId(), id, kinds, $"{where}, devices[{index}] ({device.Name})"));
            }
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{where}: {ContentErrors.Describe(exception)}", exception);
        }
    }

    private static Device ToDevice(DeviceDocument document, DeviceId id, NetworkId network, Dictionary<String, DeviceKind> kinds, String where)
    {
        if (!kinds.TryGetValue(document.Kind, out DeviceKind? kind))
        {
            throw new InvalidDataException($"{where}: there's no device kind \"{document.Kind}\".");
        }

        LockDocument? lockSettings = document.Components?.Lock;

        if (lockSettings is not null && kind.Lock is null)
        {
            throw new InvalidDataException($"{where}: devices of kind \"{kind.Id.Key}\" have no lock.");
        }

        Device device = new Device(id, kind, document.Name, network) { Powered = document.Powered ?? true };

        // A device's own settings replace the ones its kind starts it with.
        if (device.Lock is not null && lockSettings?.Locked is Boolean locked)
        {
            device.Lock.Locked = locked;
        }

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
            graph.AddLink(new Link { Id = graph.NewLinkId(), Key = document.Id, NetworkA = ends[0], NetworkB = ends[1], Medium = medium });
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{where}: {ContentErrors.Describe(exception)}", exception);
        }
    }
}
