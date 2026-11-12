using Richter.Json;

namespace Richter.Tests.Json;

/// <summary>
/// Checks the order mods load in, which decides whose content wins. A wrong order is quiet: the
/// game starts, with the wrong mod's version of something.
/// </summary>
public class LoadOrderTests
{
    [Fact]
    public void LoadsCoreFirstThenModsInIdOrderAfterWhatTheyNeed()
    {
        // "able" sorts first by id, but needs "zebra", so it waits for it. "middle" needs nothing
        // but core, so it goes first among the rest.
        IReadOnlyList<Mod> mods =
        [
            Installed("zebra"),
            Installed("able", "zebra"),
            Installed("core"),
            Installed("middle"),
        ];

        IReadOnlyList<Mod> ordered = LoadOrder.Sort(mods);

        Assert.Equal(["core", "middle", "zebra", "able"], ordered.Select(mod => mod.Id));
    }

    private static Mod Installed(String id, params String[] dependencies)
    {
        return new Mod { Id = id, Name = id, Version = "1.0.0", Dependencies = dependencies, Path = $"Mods/{id}" };
    }
}
