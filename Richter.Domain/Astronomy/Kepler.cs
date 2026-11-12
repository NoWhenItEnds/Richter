namespace Richter.Domain.Astronomy;

/// <summary> Solves Kepler's equation, which turns time along an orbit into a place on it. </summary>
internal static class Kepler
{
    private const Double Tolerance = 1e-12;
    private const Int32 MaxIterations = 50;

    /// <summary>
    /// The eccentric anomaly E, in radians, for which E − e sin E equals
    /// <paramref name="meanAnomaly"/> (in radians).
    /// </summary>
    /// <remarks>
    /// Newton's method from the starting value Vallado gives in <i>Fundamentals of
    /// Astrodynamics</i>: M + e, or M − e when M is negative. It converges in a few steps for any
    /// eccentricity below 1. The iteration limit is there so that a case it can't solve throws
    /// instead of looping for ever.
    /// </remarks>
    internal static Double EccentricAnomaly(Double meanAnomaly, Double eccentricity)
    {
        Double mean = Math.IEEERemainder(meanAnomaly, 2 * Math.PI);
        Double anomaly = mean < 0 ? mean - eccentricity : mean + eccentricity;
        Double step = Double.PositiveInfinity;
        Int32 iterations = 0;

        while (Math.Abs(step) > Tolerance && iterations < MaxIterations)
        {
            step = (anomaly - eccentricity * Math.Sin(anomaly) - mean) / (1 - eccentricity * Math.Cos(anomaly));
            anomaly -= step;
            iterations++;
        }

        if (Math.Abs(step) > Tolerance)
        {
            throw new InvalidOperationException($"Kepler's equation didn't converge for mean anomaly {meanAnomaly} and eccentricity {eccentricity}.");
        }

        return anomaly;
    }
}
