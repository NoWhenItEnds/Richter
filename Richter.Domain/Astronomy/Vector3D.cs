namespace Richter.Domain.Astronomy;

/// <summary>
/// A position or direction in space. Positions are in metres along the axes of the system's
/// reference frame (see <see cref="StarSystem"/>).
/// </summary>
/// <remarks>
/// Built on <see cref="Double"/> rather than using <c>System.Numerics.Vector3</c>, whose
/// <see cref="Single"/> components can only place things in steps of about 16 km at Earth's
/// distance from the Sun.
/// </remarks>
public readonly record struct Vector3D
{
    public static readonly Vector3D Zero = new Vector3D(0, 0, 0);

    public Vector3D(Double x, Double y, Double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Double X { get; }

    public Double Y { get; }

    public Double Z { get; }

    public Double Length => Math.Sqrt(Dot(this, this));

    /// <summary>
    /// The unit vector at <paramref name="longitude"/> degrees round from the x axis towards the
    /// y axis and <paramref name="latitude"/> degrees above the xy plane.
    /// </summary>
    public static Vector3D Direction(Double longitude, Double latitude)
    {
        Double lambda = Double.DegreesToRadians(longitude);
        Double phi = Double.DegreesToRadians(latitude);

        return new Vector3D(Math.Cos(phi) * Math.Cos(lambda), Math.Cos(phi) * Math.Sin(lambda), Math.Sin(phi));
    }

    public static Double Dot(Vector3D a, Vector3D b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    /// <summary> The vector at right angles to both, following the right-hand rule. </summary>
    public static Vector3D Cross(Vector3D a, Vector3D b)
    {
        return new Vector3D(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    }

    public static Vector3D operator +(Vector3D a, Vector3D b)
    {
        return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3D operator -(Vector3D a, Vector3D b)
    {
        return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector3D operator *(Double scale, Vector3D vector)
    {
        return new Vector3D(scale * vector.X, scale * vector.Y, scale * vector.Z);
    }

    /// <summary> Turns the vector anticlockwise about the x axis, seen from the positive end. </summary>
    internal Vector3D RotateX(Double radians)
    {
        Double cos = Math.Cos(radians);
        Double sin = Math.Sin(radians);

        return new Vector3D(X, Y * cos - Z * sin, Y * sin + Z * cos);
    }

    /// <summary> Turns the vector anticlockwise about the z axis, seen from the positive end. </summary>
    internal Vector3D RotateZ(Double radians)
    {
        Double cos = Math.Cos(radians);
        Double sin = Math.Sin(radians);

        return new Vector3D(X * cos - Y * sin, X * sin + Y * cos, Z);
    }
}
