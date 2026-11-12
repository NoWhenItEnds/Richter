using System.Collections.ObjectModel;
using Richter.Domain.Astronomy;

namespace Richter.Domain.Networking;

/// <summary>
/// The networks, devices and links in a world, and the only way to add or remove them.
/// </summary>
/// <remarks>
/// <para>
/// Every rule about how they fit together is checked here: each network has exactly one
/// gateway, every device is on a network in the graph, a link joins two different networks in
/// the graph, no two links of the same medium join the same pair, a cable joins only networks on
/// the ground of the same body, and every network is on or around a body in the star system. The
/// graph also keeps each network's devices and links. Entities refer to each other only by ID, so
/// nothing can change those connections without going through the graph.
/// </para>
/// <para>
/// IDs come from <see cref="NewNetworkId"/>, <see cref="NewDeviceId"/> and
/// <see cref="NewLinkId"/>, which share one counter that starts at 1. Content is read in file
/// order, so a scenario gives everything the same IDs on every run, and
/// <see cref="FindRoute"/> breaks ties the same way. A saved game will need to keep the counter.
/// </para>
/// <para>
/// The collections it returns are live, read-only views. Copy one before adding or removing
/// anything while going through it.
/// </para>
/// </remarks>
public class NetworkGraph
{
    private readonly StarSystem _starSystem;
    private readonly Dictionary<NetworkId, Network> _networks = new Dictionary<NetworkId, Network>();
    private readonly Dictionary<DeviceId, Device> _devices = new Dictionary<DeviceId, Device>();
    private readonly Dictionary<LinkId, Link> _links = new Dictionary<LinkId, Link>();
    private readonly Dictionary<NetworkId, Gateway> _gateways = new Dictionary<NetworkId, Gateway>();
    private readonly Dictionary<NetworkId, HashSet<Device>> _devicesByNetwork = new Dictionary<NetworkId, HashSet<Device>>();
    private readonly Dictionary<NetworkId, HashSet<Link>> _linksByNetwork = new Dictionary<NetworkId, HashSet<Link>>();
    private Int32 _lastId;

    /// <param name="starSystem"> The bodies that networks are placed on or around. </param>
    public NetworkGraph(StarSystem starSystem)
    {
        _starSystem = starSystem;
    }

    public IReadOnlyCollection<Network> Networks => _networks.Values;

    /// <summary> Every device in the graph, gateways included. </summary>
    public IReadOnlyCollection<Device> Devices => _devices.Values;

    public IReadOnlyCollection<Link> Links => _links.Values;

    public Network GetNetwork(NetworkId id)
    {
        return _networks[id];
    }

    public Gateway GetGateway(NetworkId network)
    {
        return _gateways[network];
    }

    /// <summary> The devices on a network, its gateway included. </summary>
    public IReadOnlyCollection<Device> GetDevices(NetworkId network)
    {
        return new ReadOnlySet<Device>(_devicesByNetwork[network]);
    }

    /// <summary> The links with the network at either end. </summary>
    public IReadOnlyCollection<Link> GetLinks(NetworkId network)
    {
        return new ReadOnlySet<Link>(_linksByNetwork[network]);
    }

    /// <summary> An ID for a new network, higher than any this graph has handed out. </summary>
    public NetworkId NewNetworkId()
    {
        _lastId++;

        return new NetworkId(_lastId);
    }

    /// <summary> An ID for a new device, higher than any this graph has handed out. </summary>
    public DeviceId NewDeviceId()
    {
        _lastId++;

        return new DeviceId(_lastId);
    }

    /// <summary> An ID for a new link, higher than any this graph has handed out. </summary>
    public LinkId NewLinkId()
    {
        _lastId++;

        return new LinkId(_lastId);
    }

    /// <summary> Adds a network together with its gateway, since every network has exactly one. </summary>
    public void AddNetwork(Network network, Gateway gateway)
    {
        if (_networks.ContainsKey(network.Id))
        {
            throw new ArgumentException($"Network {network.Id.Value} is already in the graph.", nameof(network));
        }

        CelestialBodyId body = BodyOf(network.Location);
        if (!_starSystem.Contains(body))
        {
            throw new ArgumentException($"Network {network.Name} is placed by body \"{body.Key}\", which isn't in the star system.", nameof(network));
        }

        if (gateway.Network != network.Id)
        {
            throw new ArgumentException($"Gateway {gateway.Name} belongs to a different network.", nameof(gateway));
        }

        if (_devices.ContainsKey(gateway.Id))
        {
            throw new ArgumentException($"Device {gateway.Id.Value} is already in the graph.", nameof(gateway));
        }

        _networks.Add(network.Id, network);
        _gateways.Add(network.Id, gateway);
        _devices.Add(gateway.Id, gateway);
        _devicesByNetwork.Add(network.Id, new HashSet<Device> { gateway });
        _linksByNetwork.Add(network.Id, new HashSet<Link>());
    }

    /// <summary>
    /// Adds a device to a network already in the graph. A gateway can't be added this way; it
    /// comes with its network through <see cref="AddNetwork"/>.
    /// </summary>
    public void AddDevice(Device device)
    {
        if (device is Gateway)
        {
            throw new ArgumentException($"Gateway {device.Name} can only be added with its network.", nameof(device));
        }

        if (_devices.ContainsKey(device.Id))
        {
            throw new ArgumentException($"Device {device.Id.Value} is already in the graph.", nameof(device));
        }

        if (!_networks.ContainsKey(device.Network))
        {
            throw new ArgumentException($"Device {device.Name} is on network {device.Network.Value}, which isn't in the graph.", nameof(device));
        }

        _devices.Add(device.Id, device);
        _devicesByNetwork[device.Network].Add(device);
    }

    /// <summary>
    /// Adds a link between two different networks in the graph that no link of the same medium
    /// joins yet. A cable can only join two networks on the ground of the same body.
    /// </summary>
    public void AddLink(Link link)
    {
        if (_links.ContainsKey(link.Id))
        {
            throw new ArgumentException($"Link {link.Id.Value} is already in the graph.", nameof(link));
        }

        if (!_networks.TryGetValue(link.NetworkA, out Network? a) || !_networks.TryGetValue(link.NetworkB, out Network? b))
        {
            throw new ArgumentException($"Link {link.Id.Value} joins a network that isn't in the graph.", nameof(link));
        }

        // From here the networks are known, so messages name them: content files and players
        // know networks by name, not by ID.
        if (a == b)
        {
            throw new ArgumentException($"A link can't join {a.Name} to itself.", nameof(link));
        }

        if (_linksByNetwork[a.Id].Any(existing => existing.Medium == link.Medium && (existing.NetworkA == b.Id || existing.NetworkB == b.Id)))
        {
            throw new ArgumentException($"{a.Name} and {b.Name} are already linked by {link.Medium}.", nameof(link));
        }

        if (link.Medium == LinkMedium.Cable && !SignalPropagation.CanJoinByCable(a.Location, b.Location))
        {
            throw new ArgumentException($"A cable can't join {a.Name} and {b.Name}: cables only join networks on the ground of the same body.", nameof(link));
        }

        _links.Add(link.Id, link);
        _linksByNetwork[link.NetworkA].Add(link);
        _linksByNetwork[link.NetworkB].Add(link);
    }

    /// <summary> Removes a network along with its gateway, its other devices and its links. </summary>
    public void RemoveNetwork(NetworkId id)
    {
        if (!_networks.ContainsKey(id))
        {
            throw new KeyNotFoundException($"Network {id.Value} isn't in the graph.");
        }

        foreach (Link link in _linksByNetwork[id])
        {
            NetworkId otherEnd = link.NetworkA == id ? link.NetworkB : link.NetworkA;
            _linksByNetwork[otherEnd].Remove(link);
            _links.Remove(link.Id);
        }

        foreach (Device device in _devicesByNetwork[id])
        {
            _devices.Remove(device.Id);
        }

        _linksByNetwork.Remove(id);
        _devicesByNetwork.Remove(id);
        _gateways.Remove(id);
        _networks.Remove(id);
    }

    /// <summary>
    /// Removes a device. A gateway can't be removed this way; it goes with its network through
    /// <see cref="RemoveNetwork"/>.
    /// </summary>
    public void RemoveDevice(DeviceId id)
    {
        if (!_devices.TryGetValue(id, out Device? device))
        {
            throw new KeyNotFoundException($"Device {id.Value} isn't in the graph.");
        }

        if (device is Gateway)
        {
            throw new ArgumentException($"Gateway {device.Name} can only be removed with its network.", nameof(id));
        }

        _devices.Remove(id);
        _devicesByNetwork[device.Network].Remove(device);
    }

    public void RemoveLink(LinkId id)
    {
        if (!_links.TryGetValue(id, out Link? link))
        {
            throw new KeyNotFoundException($"Link {id.Value} isn't in the graph.");
        }

        _links.Remove(id);
        _linksByNetwork[link.NetworkA].Remove(link);
        _linksByNetwork[link.NetworkB].Remove(link);
    }

    /// <summary>
    /// How long a signal takes to cross link <paramref name="id"/> at <paramref name="time"/>, or
    /// null while the link is down. A cable is always up. A radio or laser beam is down while a
    /// body is in the way, or while the other end is less than 10° above a ground station's
    /// horizon.
    /// </summary>
    public TimeSpan? DelayOf(LinkId id, DateTimeOffset time)
    {
        Link link = _links[id];

        return SignalPropagation.DelayOf(link.Medium, _networks[link.NetworkA].Location, _networks[link.NetworkB].Location, _starSystem, time);
    }

    /// <summary>
    /// The lowest-delay route from network <paramref name="from"/> to network
    /// <paramref name="to"/> over the links that are up at <paramref name="time"/>, or null if no
    /// route reaches it then.
    /// </summary>
    public Route? FindRoute(NetworkId from, NetworkId to, DateTimeOffset time)
    {
        if (!_networks.ContainsKey(from) || !_networks.ContainsKey(to))
        {
            throw new KeyNotFoundException($"Network {from.Value} or {to.Value} isn't in the graph.");
        }

        return RouteFinder.Find(this, from, to, time);
    }

    private static CelestialBodyId BodyOf(Location location)
    {
        CelestialBodyId body = location switch
        {
            SurfaceLocation surface => surface.Body,
            OrbitalLocation orbital => orbital.Orbit.Parent,
            _ => throw new ArgumentException($"{location.GetType().Name} is not a known kind of location.", nameof(location)),
        };

        return body;
    }
}
