using System.Text.Json;
using Richter.Domain.Astronomy;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// Reads a <see cref="StarSystem"/> from a content file such as <c>Data/solar-system.json</c>,
/// converting its published values to the model's units and conventions.
/// </summary>
public static class StarSystemFile
{
    /// <summary>
    /// Reads a star system. Throws <see cref="InvalidDataException"/>, with a message a player or
    /// modder can act on, if the file isn't a valid star system.
    /// </summary>
    public static async Task<StarSystem> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        StarSystemDocument? document;

        try
        {
            document = await JsonSerializer.DeserializeAsync(stream, ContentJsonContext.Default.StarSystemDocument, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw ContentErrors.From(exception);
        }

        if (document is null)
        {
            throw new InvalidDataException("The file holds null instead of a star system.");
        }

        DateTimeOffset epoch = InstantText.Parse(document.Epoch, "epoch");

        // Every body's GM first, because an orbit given as a period needs its parent's.
        Dictionary<String, Double> gravitationalParameters = new Dictionary<String, Double>();
        foreach ((Int32 index, BodyDocument body) in document.Bodies.Index())
        {
            if (!gravitationalParameters.TryAdd(body.Id, body.GravitationalParameter * ContentUnits.CubicMetresPerCubicKilometre))
            {
                throw new InvalidDataException($"bodies[{index}] ({body.Name}): another body already has the id \"{body.Id}\".");
            }
        }

        List<CelestialBody> bodies = document.Bodies
            .Select((body, index) => ToBody(body, $"bodies[{index}] ({body.Name})", epoch, document.ReferencePlaneTilt, gravitationalParameters))
            .ToList();
        StarSystem starSystem;

        try
        {
            starSystem = new StarSystem(bodies);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(ContentErrors.Describe(exception), exception);
        }

        return starSystem;
    }

    /// <param name="body"> The body as written. </param>
    /// <param name="where"> Where it is in the file, for error messages. </param>
    /// <param name="epoch"> The instant the file's elements describe. </param>
    /// <param name="referencePlaneTilt"> The file's tilt of its reference plane, in degrees. </param>
    /// <param name="gravitationalParameters"> Every body's GM in m³/s², by id. </param>
    private static CelestialBody ToBody(BodyDocument body, String where, DateTimeOffset epoch, Double referencePlaneTilt, Dictionary<String, Double> gravitationalParameters)
    {
        CelestialBody result;

        // The domain checks every value's range; this puts the body's name on what it finds.
        try
        {
            result = new CelestialBody
            {
                Id = new CelestialBodyId(body.Id),
                Name = body.Name,
                GravitationalParameter = body.GravitationalParameter * ContentUnits.CubicMetresPerCubicKilometre,
                Radius = body.Radius * ContentUnits.MetresPerKilometre,
                Orbit = body.Orbit is null ? null : OrbitConversion.ToOrbit(body.Orbit, where, epoch, gravitationalParameters),
                Rotation = IauRotation.ToRotation(
                    body.Spin.PoleRightAscension,
                    body.Spin.PoleDeclination,
                    body.Spin.PrimeMeridian,
                    body.Spin.DegreesPerDay,
                    epoch,
                    referencePlaneTilt),
            };
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{where}: {ContentErrors.Describe(exception)}", exception);
        }

        return result;
    }
}
