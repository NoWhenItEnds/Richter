namespace Richter.Domain.Astronomy;

/// <summary> Where an object following an <see cref="Orbit"/> is at a given instant. </summary>
internal static class OrbitMotion
{
    /// <summary>
    /// Where the object is at <paramref name="time"/>, in metres from the centre of
    /// <paramref name="parent"/> along the system's reference axes.
    /// </summary>
    internal static Vector3D PositionAt(Orbit orbit, CelestialBody parent, DateTimeOffset time)
    {
        Double eccentricAnomaly = Kepler.EccentricAnomaly(MeanAnomalyAt(orbit, parent, time), orbit.Eccentricity);

        return PointAt(orbit, parent, eccentricAnomaly);
    }

    /// <summary>
    /// <paramref name="points"/> points round the whole orbit, in metres from the centre of
    /// <paramref name="parent"/>, starting where the object is at <paramref name="time"/>. The
    /// last point repeats the first, so joining them in order closes the loop.
    /// </summary>
    /// <remarks>
    /// Starting at the object puts it exactly on the path, however coarse the path is between
    /// points. The points are evenly spaced in eccentric anomaly, which needs no Kepler solve.
    /// </remarks>
    internal static IReadOnlyList<Vector3D> PathAt(Orbit orbit, CelestialBody parent, DateTimeOffset time, Int32 points)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(points, 2);

        Double start = Kepler.EccentricAnomaly(MeanAnomalyAt(orbit, parent, time), orbit.Eccentricity);
        Vector3D[] path = new Vector3D[points];

        for (Int32 i = 0; i < points; i++)
        {
            path[i] = PointAt(orbit, parent, start + 2 * Math.PI * i / (points - 1));
        }

        return path;
    }

    /// <summary> The mean anomaly at <paramref name="time"/>, in radians. </summary>
    private static Double MeanAnomalyAt(Orbit orbit, CelestialBody parent, DateTimeOffset time)
    {
        if (parent.Id != orbit.Parent)
        {
            throw new ArgumentException($"The orbit is around body \"{orbit.Parent.Key}\", not {parent.Name}.", nameof(parent));
        }

        Double semiMajorAxis = orbit.SemiMajorAxis;
        Double meanMotion = Math.Sqrt(parent.GravitationalParameter / (semiMajorAxis * semiMajorAxis * semiMajorAxis));

        return Double.DegreesToRadians(orbit.MeanAnomalyAtEpoch) + meanMotion * (time - orbit.Epoch).TotalSeconds;
    }

    /// <summary>
    /// The point on the orbit at an eccentric anomaly, in metres from the centre of
    /// <paramref name="parent"/> along the system's reference axes.
    /// </summary>
    private static Vector3D PointAt(Orbit orbit, CelestialBody parent, Double eccentricAnomaly)
    {
        Double semiMajorAxis = orbit.SemiMajorAxis;
        Double eccentricity = orbit.Eccentricity;
        Vector3D inOrbitPlane = new Vector3D(
            semiMajorAxis * (Math.Cos(eccentricAnomaly) - eccentricity),
            semiMajorAxis * Math.Sqrt(1 - eccentricity * eccentricity) * Math.Sin(eccentricAnomaly),
            0);
        Vector3D inPlane = inOrbitPlane
            .RotateZ(Double.DegreesToRadians(orbit.ArgumentOfPeriapsis))
            .RotateX(Double.DegreesToRadians(orbit.Inclination))
            .RotateZ(Double.DegreesToRadians(orbit.LongitudeOfAscendingNode));

        Vector3D position = orbit.Plane switch
        {
            OrbitPlane.ParentEquator => BodyFrames.EquatorToSystem(inPlane, parent.Rotation),
            OrbitPlane.ReferencePlane => inPlane,
            _ => throw new ArgumentOutOfRangeException(nameof(orbit), orbit.Plane, "Not a known orbit plane."),
        };

        return position;
    }
}
