namespace Richter.Json;

/// <summary> Puts mods in the order they load, which decides whose content wins. </summary>
/// <remarks>
/// <c>core</c>, the game's own content, loads first. After it, each mod loads once everything it
/// depends on has, and mods that don't depend on each other load in order of id, so the order is
/// the same on every machine.
/// </remarks>
internal static class LoadOrder
{
    internal const String CoreId = "core";

    /// <summary>
    /// <paramref name="mods"/> in load order. Throws <see cref="InvalidDataException"/> if two
    /// share an id, <c>core</c> is missing, a dependency isn't installed, or mods depend on each
    /// other in a loop.
    /// </summary>
    internal static IReadOnlyList<Mod> Sort(IReadOnlyList<Mod> mods)
    {
        foreach (IGrouping<String, Mod> sameId in mods.GroupBy(mod => mod.Id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            throw new InvalidDataException($"{String.Join(" and ", sameId.Select(mod => mod.Path))} both have the id \"{sameId.Key}\".");
        }

        if (!mods.Any(mod => mod.Id == CoreId))
        {
            throw new InvalidDataException($"The game's own content, the mod \"{CoreId}\", is missing.");
        }

        HashSet<String> installed = mods.Select(mod => mod.Id).ToHashSet(StringComparer.Ordinal);

        foreach (Mod mod in mods)
        {
            foreach (String dependency in mod.Dependencies.Where(dependency => !installed.Contains(dependency)))
            {
                throw new InvalidDataException($"{mod.Path} needs the mod \"{dependency}\", which isn't installed.");
            }
        }

        List<Mod> waiting = mods.OrderBy(mod => mod.Id, StringComparer.Ordinal).ToList();
        List<Mod> ordered = [];
        HashSet<String> loaded = new HashSet<String>(StringComparer.Ordinal);
        Mod? next = waiting.Find(mod => mod.Id == CoreId);

        while (next is not null)
        {
            ordered.Add(next);
            loaded.Add(next.Id);
            waiting.Remove(next);
            next = waiting.Find(mod => mod.Dependencies.All(loaded.Contains));
        }

        if (waiting.Count > 0)
        {
            throw new InvalidDataException($"These mods depend on each other in a loop, so none can load first: {String.Join(", ", waiting.Select(mod => mod.Id))}.");
        }

        return ordered;
    }
}
