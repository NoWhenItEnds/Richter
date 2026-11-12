using Richter.Domain.Astronomy;

namespace Richter.Tests.Astronomy;

/// <summary>
/// Checks that an object moves round its orbit at the speed Kepler's laws give. At the wrong
/// speed it would still trace the right ellipse.
/// </summary>
public class OrbitMotionTests
{
    [Fact]
    public void MovesBetweenPeriapsisAndApoapsisAsKeplersEquationSays()
    {
        const Double semiMajorAxis = 10_000_000;
        const Double eccentricity = 0.5;
        CelestialBody earth = TestBodies.CreateEarth();
        Orbit orbit = TestBodies.OrbitAroundEarth(semiMajorAxis, eccentricity, inclination: 30, longitudeOfAscendingNode: 40, argumentOfPeriapsis: 70);
        Double meanMotion = Math.Sqrt(TestBodies.EarthGravitationalParameter / (semiMajorAxis * semiMajorAxis * semiMajorAxis));

        // At eccentric anomaly E the distance is a(1 - e cos E), reached (E - e sin E) / n after
        // periapsis.
        Assert.Equal(semiMajorAxis * (1 - eccentricity), DistanceAfter(0), 0.01);
        Assert.Equal(semiMajorAxis, DistanceAfter((Math.PI / 2 - eccentricity) / meanMotion), 0.01);
        Assert.Equal(semiMajorAxis * (1 + eccentricity), DistanceAfter(Math.PI / meanMotion), 0.01);

        Double DistanceAfter(Double seconds)
        {
            DateTimeOffset time = TestBodies.Epoch + TimeSpan.FromTicks((Int64)Math.Round(seconds * TimeSpan.TicksPerSecond));
            return OrbitMotion.PositionAt(orbit, earth, time).Length;
        }
    }
}
