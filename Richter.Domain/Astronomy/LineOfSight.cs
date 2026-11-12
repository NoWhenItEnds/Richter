namespace Richter.Domain.Astronomy;

/// <summary> Whether a sphere is in the way of a straight line. </summary>
internal static class LineOfSight
{
    /// <summary>
    /// Whether the straight line from <paramref name="start"/> to <paramref name="end"/> passes
    /// inside the sphere.
    /// </summary>
    /// <remarks>
    /// Only a point strictly between the ends counts. A location on a body's surface looking away
    /// from the body is then never blocked by it, even when rounding puts the location a fraction
    /// below the surface.
    /// </remarks>
    internal static Boolean IsBlockedBySphere(Vector3D start, Vector3D end, Vector3D centre, Double radius)
    {
        Vector3D line = end - start;
        Double lengthSquared = Vector3D.Dot(line, line);
        Boolean blocked = false;

        if (lengthSquared > 0)
        {
            Double along = Vector3D.Dot(centre - start, line) / lengthSquared;

            if (along > 0 && along < 1)
            {
                blocked = (centre - (start + along * line)).Length < radius;
            }
        }

        return blocked;
    }
}
