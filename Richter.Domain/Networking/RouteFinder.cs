namespace Richter.Domain.Networking;

/// <summary>
/// Finds the lowest-delay route between two networks over the links that are up.
/// </summary>
/// <remarks>
/// Dijkstra's algorithm, with each link's delay as its length. A link's delay is worked out only
/// when the search first reaches one of its ends, and links that are down are skipped. Links are
/// tried in ID order, which is the order they were added, so when two routes have the same delay,
/// the same one wins on every run.
/// </remarks>
internal static class RouteFinder
{
    internal static Route? Find(NetworkGraph graph, NetworkId from, NetworkId to, DateTimeOffset time)
    {
        Dictionary<NetworkId, TimeSpan> fastest = new Dictionary<NetworkId, TimeSpan> { [from] = TimeSpan.Zero };
        Dictionary<NetworkId, (NetworkId Previous, LinkId Link)> arrivedBy = new Dictionary<NetworkId, (NetworkId Previous, LinkId Link)>();
        HashSet<NetworkId> settled = new HashSet<NetworkId>();
        PriorityQueue<NetworkId, TimeSpan> frontier = new PriorityQueue<NetworkId, TimeSpan>();
        frontier.Enqueue(from, TimeSpan.Zero);

        while (!settled.Contains(to) && frontier.TryDequeue(out NetworkId network, out TimeSpan delay))
        {
            // A network can be queued more than once; only its first, fastest arrival counts.
            if (settled.Add(network))
            {
                foreach (Link link in graph.GetLinks(network).OrderBy(link => link.Id.Value))
                {
                    NetworkId next = link.NetworkA == network ? link.NetworkB : link.NetworkA;

                    if (!settled.Contains(next) && graph.DelayOf(link.Id, time) is TimeSpan crossing)
                    {
                        TimeSpan arrival = delay + crossing;

                        if (!fastest.TryGetValue(next, out TimeSpan known) || arrival < known)
                        {
                            fastest[next] = arrival;
                            arrivedBy[next] = (network, link.Id);
                            frontier.Enqueue(next, arrival);
                        }
                    }
                }
            }
        }

        Route? route = null;

        if (settled.Contains(to))
        {
            List<NetworkId> networks = [to];
            List<LinkId> links = [];
            NetworkId current = to;

            while (current != from)
            {
                (NetworkId previous, LinkId link) = arrivedBy[current];
                networks.Add(previous);
                links.Add(link);
                current = previous;
            }

            networks.Reverse();
            links.Reverse();
            route = new Route(networks, links, fastest[to]);
        }

        return route;
    }
}
