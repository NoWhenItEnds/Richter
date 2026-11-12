namespace Richter.Domain.Astronomy;

/// <summary>
/// Turns positions measured against a body, along its equator or fixed to its spinning surface,
/// into the system's reference frame.
/// </summary>
/// <remarks>
/// A body's equatorial frame has its z axis along the body's pole and its x axis where the
/// system's reference direction falls when projected straight onto the equator. Both
/// <see cref="Rotation.PrimeMeridianAtEpoch"/> and <see cref="Orbit.LongitudeOfAscendingNode"/>
/// are measured from that x axis. In the Solar System the reference direction is the March
/// equinox, which lies on Earth's equator, so Earth's prime meridian angle is Greenwich sidereal
/// time and satellite nodes follow the usual convention.
/// </remarks>
internal static class BodyFrames
{
    private const Double MinimumProjection = 1e-9;

    /// <summary> The axes of the body's equatorial frame, as unit vectors in the system frame. </summary>
    internal static (Vector3D X, Vector3D Y, Vector3D Z) EquatorAxes(Rotation rotation)
    {
        Vector3D pole = Vector3D.Direction(rotation.PoleLongitude, rotation.PoleLatitude);
        Vector3D towardsReference = new Vector3D(1, 0, 0) - pole.X * pole;
        Double projection = towardsReference.Length;

        if (projection < MinimumProjection)
        {
            throw new ArgumentException("The pole points along the reference direction, which leaves its equator no zero for longitudes.", nameof(rotation));
        }

        Vector3D xAxis = (1 / projection) * towardsReference;

        return (xAxis, Vector3D.Cross(pole, xAxis), pole);
    }

    internal static Vector3D EquatorToSystem(Vector3D vector, Rotation rotation)
    {
        (Vector3D xAxis, Vector3D yAxis, Vector3D zAxis) = EquatorAxes(rotation);

        return vector.X * xAxis + vector.Y * yAxis + vector.Z * zAxis;
    }

    /// <summary>
    /// Where <paramref name="location"/> is at <paramref name="time"/>, in metres from the centre
    /// of <paramref name="body"/> along the system's reference axes.
    /// </summary>
    internal static Vector3D SurfaceToSystem(SurfaceLocation location, CelestialBody body, DateTimeOffset time)
    {
        if (body.Id != location.Body)
        {
            throw new ArgumentException($"The location is on body \"{location.Body.Key}\", not {body.Name}.", nameof(body));
        }

        Vector3D fixedToBody = (body.Radius + location.Altitude) * Vector3D.Direction(location.Longitude, location.Latitude);

        return EquatorToSystem(fixedToBody.RotateZ(PrimeMeridianAt(body.Rotation, time)), body.Rotation);
    }

    /// <summary> The prime meridian's angle from the equatorial frame's x axis, in radians. </summary>
    private static Double PrimeMeridianAt(Rotation rotation, DateTimeOffset time)
    {
        // Keeping only the part-turn holds the angle's precision centuries from the epoch.
        Double turns = (time - rotation.Epoch) / rotation.SiderealPeriod;

        return Double.DegreesToRadians(rotation.PrimeMeridianAtEpoch) + 2 * Math.PI * (turns - Math.Floor(turns));
    }
}
