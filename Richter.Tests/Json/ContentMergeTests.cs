using Richter.Json;
using Richter.Json.Documents;

namespace Richter.Tests.Json;

/// <summary>
/// Checks how mods' copies of a file merge. Getting it wrong is quiet: a mod's change is lost, or
/// replaces the wrong thing, and the game still starts.
/// </summary>
public class ContentMergeTests
{
    [Fact]
    public void ReplacesAnEntryInItsPlaceAndAddsNewOnesAfter()
    {
        FromFile<ScenarioDocument> core = Scenario("Mods/core/scenarios/test.json", startTime: null, ["first", "second"]);
        FromFile<ScenarioDocument> mod = Scenario("Mods/mod/scenarios/test.json", startTime: null, ["third", "first"]);

        IReadOnlyList<FromFile<NetworkDocument>> merged = ContentMerge.ById([core, mod], file => file.Networks, network => network.Id, "networks");

        Assert.Equal(["first", "second", "third"], merged.Select(network => network.Value.Id));
        Assert.Equal(["Mods/mod/scenarios/test.json", "Mods/core/scenarios/test.json", "Mods/mod/scenarios/test.json"], merged.Select(network => network.File));
    }

    [Fact]
    public void TakesASettingFromTheLastFileThatSetsIt()
    {
        FromFile<ScenarioDocument> core = Scenario("Mods/core/scenarios/test.json", startTime: "2112-01-01", []);
        FromFile<ScenarioDocument> changes = Scenario("Mods/changes/scenarios/test.json", startTime: "2150-01-01", []);
        FromFile<ScenarioDocument> leavesItAlone = Scenario("Mods/other/scenarios/test.json", startTime: null, []);

        FromFile<String>? startTime = ContentMerge.Last([core, changes, leavesItAlone], file => file.StartTime);

        Assert.Equal("2150-01-01", startTime?.Value);
        Assert.Equal("Mods/changes/scenarios/test.json", startTime?.File);
    }

    private static FromFile<ScenarioDocument> Scenario(String file, String? startTime, IReadOnlyList<String> networkIds)
    {
        ScenarioDocument document = new ScenarioDocument
        {
            StartTime = startTime,
            Networks = networkIds
                .Select(id => new NetworkDocument { Id = id, Name = id })
                .ToList(),
        };

        return new FromFile<ScenarioDocument>(document, file);
    }
}
