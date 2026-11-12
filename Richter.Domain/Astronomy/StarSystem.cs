namespace Richter.Domain.Astronomy;

/// <summary>
/// The celestial bodies of one star system, looked up by ID, and where they and the locations
/// around them are at any instant.
/// </summary>
/// <remarks>
/// <para>
/// Checks when it is created that the bodies form one tree: IDs are unique, every parent is in
/// the system, exactly one body (the star) has no orbit, and following parents from any body
/// reaches it. Bodies don't change during a game, so none can be added or removed afterwards.
/// </para>
/// <para>
/// Positions are in metres from the root body's centre, along the axes of the system's reference
/// frame: an inertial frame whose xy plane is the reference plane and whose x axis is the
/// reference direction. The Solar System uses the ecliptic (the plane of Earth's orbit) and the
/// March equinox, as fixed in 2000.
/// </para>
/// <para>
/// A frame asks where the bodies are many times over, once for every link it checks, so the
/// positions of bodies at the last instant asked about are kept. Asking about another instant
/// starts again. That makes a star system unsafe to use from more than one thread; the game uses
/// it only from the main thread.
/// </para>
/// </remarks>
public class StarSystem
{
    private readonly Dictionary<CelestialBodyId, CelestialBody> _bodies;
    private readonly Dictionary<CelestialBodyId, Vector3D> _positions = new Dictionary<CelestialBodyId, Vector3D>();
    private DateTimeOffset _positionsTime;

    public StarSystem(IEnumerable<CelestialBody> bodies)
    {
        Dictionary<CelestialBodyId, CelestialBody> byId = new Dictionary<CelestialBodyId, CelestialBody>();

        foreach (CelestialBody body in bodies)
        {
            if (!byId.TryAdd(body.Id, body))
            {
                throw new ArgumentException($"Two bodies have the id \"{body.Id.Key}\".", nameof(bodies));
            }
        }

        foreach (CelestialBody body in byId.Values)
        {
            if (body.Orbit is not null && !byId.ContainsKey(body.Orbit.Parent))
            {
                throw new ArgumentException($"{body.Name} orbits body \"{body.Orbit.Parent.Key}\", which isn't in the system.", nameof(bodies));
            }
        }

        Int32 rootCount = byId.Values.Count(body => body.Orbit is null);
        if (rootCount != 1)
        {
            throw new ArgumentException($"A star system needs exactly one body with no orbit, but has {rootCount}.", nameof(bodies));
        }

        foreach (CelestialBody body in byId.Values)
        {
            // With one root and every parent present, a walk longer than the number of bodies
            // can only mean a loop.
            CelestialBody current = body;
            Int32 steps = 0;
            while (current.Orbit is not null && steps <= byId.Count)
            {
                current = byId[current.Orbit.Parent];
                steps++;
            }

            if (current.Orbit is not null)
            {
                throw new ArgumentException($"{body.Name} is in a loop of bodies orbiting each other.", nameof(bodies));
            }
        }

        _bodies = byId;
    }

    public IReadOnlyCollection<CelestialBody> Bodies => _bodies.Values;

    public Boolean Contains(CelestialBodyId id)
    {
        return _bodies.ContainsKey(id);
    }

    public CelestialBody GetBody(CelestialBodyId id)
    {
        return _bodies[id];
    }

    /// <summary> Where the centre of body <paramref name="id"/> is at <paramref name="time"/>. </summary>
    public Vector3D PositionOf(CelestialBodyId id, DateTimeOffset time)
    {
        if (time != _positionsTime)
        {
            _positions.Clear();
            _positionsTime = time;
        }

        if (!_positions.TryGetValue(id, out Vector3D position))
        {
            CelestialBody body = _bodies[id];

            if (body.Orbit is not null)
            {
                position = PositionOf(body.Orbit.Parent, time) + OrbitMotion.PositionAt(body.Orbit, _bodies[body.Orbit.Parent], time);
            }

            _positions.Add(id, position);
        }

        return position;
    }

    /// <summary>
    /// <paramref name="points"/> points round the orbit of body <paramref name="id"/>, as it is at
    /// <paramref name="time"/>, for drawing it. The path starts at the body and closes on itself.
    /// </summary>
    public IReadOnlyList<Vector3D> OrbitPathOf(CelestialBodyId id, DateTimeOffset time, Int32 points)
    {
        CelestialBody body = _bodies[id];

        if (body.Orbit is null)
        {
            throw new ArgumentException($"{body.Name} has no orbit: it's the root of the system.", nameof(id));
        }

        return OrbitPathOf(body.Orbit, time, points);
    }

    /// <summary>
    /// <paramref name="points"/> points round <paramref name="orbit"/>, as it is at
    /// <paramref name="time"/>, for drawing it. The path starts where the orbiting object is and
    /// closes on itself.
    /// </summary>
    public IReadOnlyList<Vector3D> OrbitPathOf(Orbit orbit, DateTimeOffset time, Int32 points)
    {
        Vector3D parentPosition = PositionOf(orbit.Parent, time);

        return OrbitMotion.PathAt(orbit, _bodies[orbit.Parent], time, points)
            .Select(point => parentPosition + point)
            .ToList();
    }

    public Vector3D PositionOf(Location location, DateTimeOffset time)
    {
        Vector3D position = location switch
        {
            SurfaceLocation surface => PositionOf(surface.Body, time) + BodyFrames.SurfaceToSystem(surface, _bodies[surface.Body], time),
            OrbitalLocation orbital => PositionOf(orbital.Orbit.Parent, time) + OrbitMotion.PositionAt(orbital.Orbit, _bodies[orbital.Orbit.Parent], time),
            _ => throw new ArgumentException($"{location.GetType().Name} is not a known kind of location.", nameof(location)),
        };

        return position;
    }

    /// <summary>
    /// Whether no body is in the way of a straight line between two locations at
    /// <paramref name="time"/>.
    /// </summary>
    /// <remarks>
    /// Bodies are treated as spheres. This is the view at one instant: it ignores the time light
    /// takes to cross the gap, which matters between planets. It doesn't check range, or how far
    /// above the horizon the other end is.
    /// </remarks>
    public Boolean HasLineOfSight(Location from, Location to, DateTimeOffset time)
    {
        Vector3D start = PositionOf(from, time);
        Vector3D end = PositionOf(to, time);

        return !_bodies.Values.Any(body => LineOfSight.IsBlockedBySphere(start, end, PositionOf(body.Id, time), body.Radius));
    }
}
