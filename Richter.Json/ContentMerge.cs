namespace Richter.Json;

/// <summary>
/// Merges the copies of one content file that several mods have, in load order, so that a mod's
/// file only needs to hold what it adds or changes.
/// </summary>
internal static class ContentMerge
{
    /// <summary>
    /// Every file's entries, merged in load order. An entry replaces an earlier one with the same
    /// id, keeping its place, and an entry with a new id goes after the rest.
    /// </summary>
    /// <remarks>
    /// Two entries with the same id in one file are an error, because the second would quietly
    /// replace the first.
    /// </remarks>
    /// <param name="files"> The files, in load order. </param>
    /// <param name="entriesOf"> A file's entries, or null if it has none. </param>
    /// <param name="idOf"> An entry's id. </param>
    /// <param name="noun"> What the entries are, in the plural, for errors: <c>networks</c>. </param>
    internal static IReadOnlyList<FromFile<TEntry>> ById<TDocument, TEntry>(
        IReadOnlyList<FromFile<TDocument>> files,
        Func<TDocument, IReadOnlyList<TEntry>?> entriesOf,
        Func<TEntry, String> idOf,
        String noun)
    {
        OrderedDictionary<String, FromFile<TEntry>> merged = new OrderedDictionary<String, FromFile<TEntry>>(StringComparer.Ordinal);

        foreach (FromFile<TDocument> file in files)
        {
            HashSet<String> idsInFile = new HashSet<String>(StringComparer.Ordinal);

            foreach (TEntry entry in entriesOf(file.Value) ?? [])
            {
                String id = idOf(entry);

                if (!idsInFile.Add(id))
                {
                    throw new InvalidDataException($"{file.File}: two {noun} have the id \"{id}\".");
                }

                merged[id] = new FromFile<TEntry>(entry, file.File);
            }
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// The value from the last file, in load order, that sets one, or null if none does.
    /// </summary>
    internal static FromFile<TValue>? Last<TDocument, TValue>(IReadOnlyList<FromFile<TDocument>> files, Func<TDocument, TValue?> valueOf)
        where TValue : class
    {
        FromFile<TValue>? last = null;

        foreach (FromFile<TDocument> file in files)
        {
            if (valueOf(file.Value) is TValue value)
            {
                last = new FromFile<TValue>(value, file.File);
            }
        }

        return last;
    }
}
