using Richter.Domain;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// Builds a game's starting <see cref="World"/> from every mod's content: the scenario, its star
/// system, and the kinds of device there can be.
/// </summary>
public static class GameContent
{
    /// <summary>
    /// Merges every mod's <c>scenarios/</c><paramref name="scenario"/><c>.json</c>, the star system
    /// it names and <c>device-kinds.json</c>, and starts a world from them. Throws
    /// <see cref="InvalidDataException"/>, with a message a player or modder can act on, if the
    /// content is missing or invalid.
    /// </summary>
    public static async Task<World> LoadWorldAsync(ModList mods, String scenario, CancellationToken cancellationToken)
    {
        String scenarioPath = $"scenarios/{scenario}.json";
        IReadOnlyList<FromFile<ScenarioDocument>> scenarioFiles = await mods.ReadAllAsync(scenarioPath, ContentJsonContext.Default.ScenarioDocument, cancellationToken);

        if (scenarioFiles.Count == 0)
        {
            throw new InvalidDataException($"No mod has the scenario {scenarioPath}.");
        }

        FromFile<String> starSystem = ContentMerge.Last(scenarioFiles, file => file.StarSystem)
            ?? throw new InvalidDataException($"No mod's {scenarioPath} sets starSystem.");
        String starSystemPath = $"star-systems/{starSystem.Value}.json";
        IReadOnlyList<FromFile<StarSystemDocument>> starSystemFiles = await mods.ReadAllAsync(starSystemPath, ContentJsonContext.Default.StarSystemDocument, cancellationToken);

        if (starSystemFiles.Count == 0)
        {
            throw new InvalidDataException($"{starSystem.File}: starSystem is \"{starSystem.Value}\", but no mod has {starSystemPath}.");
        }

        IReadOnlyList<FromFile<DeviceKindsDocument>> deviceKindFiles = await mods.ReadAllAsync(DeviceKindsFile.FileName, ContentJsonContext.Default.DeviceKindsDocument, cancellationToken);

        return ScenarioFile.Build(
            scenarioFiles,
            scenarioPath,
            StarSystemFile.Build(starSystemFiles, starSystemPath),
            DeviceKindsFile.Build(deviceKindFiles));
    }
}
