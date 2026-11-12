using Richter.Domain.Astronomy;

namespace Richter.Domain.Networking;

/// <summary> Whether a link can carry a signal at an instant, and how long the signal takes. </summary>
/// <remarks>
/// The delay is the distance at one instant over the signal's speed. It ignores that the ends move
/// while the signal travels, which matters only between planets, and then by a fraction of a
/// second.
/// </remarks>
internal static class SignalPropagation
{
    private const Double SpeedOfLight = 299_792_458;

    /// <summary>
    /// Light in optical fibre travels at about two-thirds of its speed in a vacuum, about 5 µs a
    /// kilometre.
    /// </summary>
    private const Double SpeedInCable = SpeedOfLight * 2 / 3;

    /// <summary>
    /// How far above a ground station's horizon, in degrees, the other end of a beam must be. Real
    /// stations use 5 to 10°, because near the horizon a beam crosses more air and meets hills and
    /// buildings.
    /// </summary>
    private const Double MinimumElevation = 10;

    /// <summary>
    /// The highest a <see cref="SurfaceLocation"/> can be, in metres, and still count as on the
    /// ground: the Kármán line, the usual edge of space. Above it, a location fixed over one spot
    /// is a spacecraft, such as a geostationary satellite.
    /// </summary>
    private const Double GroundCeiling = 100_000;

    /// <summary>
    /// Whether a cable can join the two locations: both on the ground of the same body.
    /// </summary>
    internal static Boolean CanJoinByCable(Location a, Location b)
    {
        return a is SurfaceLocation start && b is SurfaceLocation end
            && IsOnTheGround(start) && IsOnTheGround(end)
            && start.Body == end.Body;
    }

    /// <summary>
    /// How long a signal takes to cross a link between <paramref name="a"/> and
    /// <paramref name="b"/> at <paramref name="time"/>, or null if the link is down then.
    /// </summary>
    internal static TimeSpan? DelayOf(LinkMedium medium, Location a, Location b, StarSystem starSystem, DateTimeOffset time)
    {
        TimeSpan? delay = medium switch
        {
            LinkMedium.Cable when a is SurfaceLocation start && b is SurfaceLocation end => CableDelay(start, end, starSystem),
            LinkMedium.Cable => throw new InvalidOperationException("A cable can only join two networks on the ground."),
            LinkMedium.Radio or LinkMedium.Laser => BeamDelay(a, b, starSystem, time),
            _ => throw new ArgumentOutOfRangeException(nameof(medium), medium, "Not a known link medium."),
        };

        return delay;
    }

    /// <summary>
    /// A cable follows the ground, so its length is the arc between its ends at the body's radius.
    /// Both ends turn with the body, so the length never changes.
    /// </summary>
    private static TimeSpan CableDelay(SurfaceLocation start, SurfaceLocation end, StarSystem starSystem)
    {
        Vector3D from = Vector3D.Direction(start.Longitude, start.Latitude);
        Vector3D to = Vector3D.Direction(end.Longitude, end.Latitude);
        Double angle = Math.Atan2(Vector3D.Cross(from, to).Length, Vector3D.Dot(from, to));
        Double length = angle * starSystem.GetBody(start.Body).Radius;

        return TimeSpan.FromSeconds(length / SpeedInCable);
    }

    private static TimeSpan? BeamDelay(Location a, Location b, StarSystem starSystem, DateTimeOffset time)
    {
        Vector3D start = starSystem.PositionOf(a, time);
        Vector3D end = starSystem.PositionOf(b, time);
        TimeSpan? delay = null;

        if (ClearsHorizon(a, start, end, starSystem, time)
            && ClearsHorizon(b, end, start, starSystem, time)
            && starSystem.HasLineOfSight(a, b, time))
        {
            delay = TimeSpan.FromSeconds((end - start).Length / SpeedOfLight);
        }

        return delay;
    }

    /// <summary>
    /// Whether <paramref name="target"/> is at least <see cref="MinimumElevation"/> above the
    /// horizon of <paramref name="from"/>. Only a location on the ground has a horizon; from
    /// anywhere else, every direction clears.
    /// </summary>
    private static Boolean ClearsHorizon(Location from, Vector3D fromPosition, Vector3D target, StarSystem starSystem, DateTimeOffset time)
    {
        Boolean clears = true;

        if (from is SurfaceLocation ground && IsOnTheGround(ground))
        {
            Vector3D up = fromPosition - starSystem.PositionOf(ground.Body, time);
            Vector3D towardsTarget = target - fromPosition;
            Double sineOfElevation = Vector3D.Dot(up, towardsTarget) / (up.Length * towardsTarget.Length);
            clears = sineOfElevation >= Math.Sin(Double.DegreesToRadians(MinimumElevation));
        }

        return clears;
    }

    private static Boolean IsOnTheGround(SurfaceLocation location)
    {
        return location.Altitude < GroundCeiling;
    }
}
