using Richter.Domain.Astronomy;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// Builds a <see cref="StarSystem"/> from every mod's copy of its file, converting published
/// values to the model's units and conventions.
/// </summary>
internal static class StarSystemFile
{
    /// <summary>
    /// Merges the files' bodies by id and builds the star system. Throws
    /// <see cref="InvalidDataException"/>, with a message a player or modder can act on, if the
    /// result isn't a valid star system.
    /// </summary>
    /// <param name="files"> Every mod's copy, in load order. </param>
    /// <param name="path"> The path inside a mod, such as <c>star-systems/solar-system.json</c>, for errors. </param>
    internal static StarSystem Build(IReadOnlyList<FromFile<StarSystemDocument>> files, String path)
    {
        Dictionary<String, (DateTimeOffset Epoch, Double ReferencePlaneTilt)> frames = files.ToDictionary(
            file => file.File,
            file => (InstantText.Parse(file.Value.Epoch, $"{file.File}: epoch"), file.Value.ReferencePlaneTilt));
        IReadOnlyList<FromFile<BodyDocument>> bodies = ContentMerge.ById(files, file => file.Bodies, body => body.Id, "bodies");

        // Every body's GM first, because an orbit given as a period needs its parent's.
        Dictionary<String, Double> gravitationalParameters = bodies.ToDictionary(
            body => body.Value.Id,
            body => body.Value.GravitationalParameter * ContentUnits.CubicMetresPerCubicKilometre);
        List<CelestialBody> converted = bodies
            .Select(body => ToBody(body, frames[body.File], gravitationalParameters))
            .ToList();
        StarSystem starSystem;

        try
        {
            starSystem = new StarSystem(converted);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"The star system in {path}: {ContentErrors.Describe(exception)}", exception);
        }

        return starSystem;
    }

    /// <param name="body"> The body as written, and its file. </param>
    /// <param name="frame"> The epoch and the reference plane's tilt from the body's file. </param>
    /// <param name="gravitationalParameters"> Every body's GM in m³/s², by id. </param>
    private static CelestialBody ToBody(
        FromFile<BodyDocument> body,
        (DateTimeOffset Epoch, Double ReferencePlaneTilt) frame,
        Dictionary<String, Double> gravitationalParameters)
    {
        String where = $"{body.File}, body \"{body.Value.Id}\"";
        BodyDocument document = body.Value;
        CelestialBody result;

        // The domain checks every value's range; this puts the body's place on what it finds.
        try
        {
            result = new CelestialBody
            {
                Id = new CelestialBodyId(document.Id),
                Name = document.Name,
                GravitationalParameter = document.GravitationalParameter * ContentUnits.CubicMetresPerCubicKilometre,
                Radius = document.Radius * ContentUnits.MetresPerKilometre,
                Orbit = document.Orbit is null ? null : OrbitConversion.ToOrbit(document.Orbit, where, frame.Epoch, gravitationalParameters),
                Rotation = IauRotation.ToRotation(
                    document.Spin.PoleRightAscension,
                    document.Spin.PoleDeclination,
                    document.Spin.PrimeMeridian,
                    document.Spin.DegreesPerDay,
                    frame.Epoch,
                    frame.ReferencePlaneTilt),
            };
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{where}: {ContentErrors.Describe(exception)}", exception);
        }

        return result;
    }
}
