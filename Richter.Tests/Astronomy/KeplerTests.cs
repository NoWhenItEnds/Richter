using Richter.Domain.Astronomy;

namespace Richter.Tests.Astronomy;

/// <summary>
/// Checks that Kepler's equation is solved at every eccentricity an orbit can have. A wrong
/// answer still gives a point on the orbit, just not where the object is.
/// </summary>
public class KeplerTests
{
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.1, 0.5)]
    [InlineData(0.5, -2.0)]
    [InlineData(0.5, 100.0)]
    [InlineData(0.9, 3.1)]
    [InlineData(0.99, 0.001)]
    [InlineData(0.99, -3.1)]
    public void SolvesKeplersEquationAtAnyEccentricity(Double eccentricity, Double meanAnomaly)
    {
        Double anomaly = Kepler.EccentricAnomaly(meanAnomaly, eccentricity);

        Double residual = Math.IEEERemainder(anomaly - eccentricity * Math.Sin(anomaly) - meanAnomaly, 2 * Math.PI);
        Assert.InRange(residual, -1e-12, 1e-12);
    }
}
