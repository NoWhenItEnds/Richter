using Richter.Domain.Astronomy;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary> Turns an orbit as written in a content file into an <see cref="Orbit"/>. </summary>
internal static class OrbitConversion
{
    /// <param name="orbit"> The orbit as written. </param>
    /// <param name="where"> Where it is in the file, for error messages. </param>
    /// <param name="epoch"> The instant the elements describe. </param>
    /// <param name="gravitationalParameters"> Every body's GM in m³/s², by id. </param>
    internal static Orbit ToOrbit(OrbitDocument orbit, String where, DateTimeOffset epoch, IReadOnlyDictionary<String, Double> gravitationalParameters)
    {
        if (!gravitationalParameters.TryGetValue(orbit.Parent, out Double parentGravitationalParameter))
        {
            throw new InvalidDataException($"{where}: it orbits body \"{orbit.Parent}\", which isn't in the star system.");
        }

        Double semiMajorAxis = (orbit.Period, orbit.SemiMajorAxis) switch
        {
            (Double period, null) => SemiMajorAxisFor(parentGravitationalParameter, period),
            (null, Double kilometres) => kilometres * ContentUnits.MetresPerKilometre,
            _ => throw new InvalidDataException($"{where}: an orbit needs either a period or a semiMajorAxis, not both or neither."),
        };
        OrbitPlane plane = orbit.Plane switch
        {
            "reference" => OrbitPlane.ReferencePlane,
            "parentEquator" => OrbitPlane.ParentEquator,
            _ => throw new InvalidDataException($"{where}: plane is \"{orbit.Plane}\", but must be \"reference\" or \"parentEquator\"."),
        };

        return new Orbit
        {
            Parent = new CelestialBodyId(orbit.Parent),
            Plane = plane,
            SemiMajorAxis = semiMajorAxis,
            Eccentricity = orbit.Eccentricity,
            Inclination = orbit.Inclination,
            LongitudeOfAscendingNode = orbit.LongitudeOfAscendingNode,
            ArgumentOfPeriapsis = orbit.ArgumentOfPeriapsis,
            MeanAnomalyAtEpoch = orbit.MeanAnomaly,
            Epoch = epoch,
        };
    }

    /// <summary>
    /// Kepler's third law: the semi-major axis, in metres, that gives an orbit this period, in
    /// days. Sizing an orbit by its period keeps the body in time with the real one.
    /// </summary>
    private static Double SemiMajorAxisFor(Double parentGravitationalParameter, Double periodInDays)
    {
        if (!(Double.IsFinite(periodInDays) && periodInDays > 0))
        {
            throw new ArgumentOutOfRangeException("period", periodInDays, $"period must be a finite number above 0, but is {periodInDays}.");
        }

        Double radiansPerSecond = 2 * Math.PI / (periodInDays * ContentUnits.SecondsPerDay);

        return Math.Cbrt(parentGravitationalParameter / (radiansPerSecond * radiansPerSecond));
    }
}
