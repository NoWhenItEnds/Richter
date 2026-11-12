using System.Text.Json.Serialization.Metadata;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// The mods installed in the game's <c>Mods</c> folder, in the order they load, and the files
/// they hold.
/// </summary>
/// <remarks>
/// <para>
/// The game's own content is the mod <c>core</c>, read the same way as every other mod, so a mod
/// can change anything the game's content sets. Each mod is a folder with a <c>mod.json</c>.
/// </para>
/// <para>
/// A file is found by its path inside a mod, such as <c>scenarios/default.json</c>. Where
/// several mods have the same path, a data file is read from each and merged in load order
/// (see <see cref="ContentMerge"/>); any other file, such as a shader, comes whole from the last
/// mod that has it.
/// </para>
/// </remarks>
public class ModList
{
    /// <summary> The folder under the game's directory that holds one folder per mod. </summary>
    public const String Folder = "Mods";

    /// <summary>
    /// The version of the content format this build of the game reads. It goes up when a change
    /// would break mods that already exist, so that the game names the problem instead of
    /// misreading them.
    /// </summary>
    public const Int32 ContentFormat = 1;

    private const String ManifestFile = "mod.json";

    private readonly String _gameDirectory;

    private ModList(String gameDirectory, IReadOnlyList<Mod> mods)
    {
        _gameDirectory = gameDirectory;
        Mods = mods;
    }

    /// <summary> In load order: <c>core</c> first, and each mod after the mods it needs. </summary>
    public IReadOnlyList<Mod> Mods { get; }

    /// <summary>
    /// Finds every mod in <paramref name="gameDirectory"/>'s <c>Mods</c> folder and puts them in
    /// load order. Throws <see cref="InvalidDataException"/>, with a message a player or modder
    /// can act on, if a mod's <c>mod.json</c> is missing or invalid, or the mods can't be ordered.
    /// </summary>
    public static async Task<ModList> LoadAsync(String gameDirectory, CancellationToken cancellationToken)
    {
        String modsDirectory = Path.Combine(gameDirectory, Folder);

        if (!Directory.Exists(modsDirectory))
        {
            throw new InvalidDataException($"The {Folder} folder is missing.");
        }

        List<Mod> mods = [];

        foreach (String directory in Directory.EnumerateDirectories(modsDirectory).Order(StringComparer.Ordinal))
        {
            mods.Add(await ReadModAsync(gameDirectory, $"{Folder}/{Path.GetFileName(directory)}", cancellationToken));
        }

        return new ModList(gameDirectory, LoadOrder.Sort(mods));
    }

    /// <summary>
    /// Reads the copy of <paramref name="path"/> from the last mod that has one, such as a
    /// shader that a mod replaces. Throws <see cref="InvalidDataException"/>, naming the file, if
    /// no mod has it or <paramref name="readAsync"/> finds it invalid.
    /// </summary>
    public async Task<T> ReadFileAsync<T>(String path, Func<Stream, CancellationToken, Task<T>> readAsync, CancellationToken cancellationToken)
    {
        String file = FilesAt(path).LastOrDefault() ?? throw new InvalidDataException($"No mod has {path}.");
        T content;

        await using FileStream stream = File.OpenRead(Path.Combine(_gameDirectory, file));

        try
        {
            content = await readAsync(stream, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException($"{file} isn't valid. {exception.Message}", exception);
        }

        return content;
    }

    /// <summary>
    /// Reads every mod's copy of the data file at <paramref name="path"/>, in load order, ready to
    /// merge. Empty if no mod has one.
    /// </summary>
    internal async Task<IReadOnlyList<FromFile<T>>> ReadAllAsync<T>(String path, JsonTypeInfo<T> type, CancellationToken cancellationToken)
        where T : class
    {
        List<FromFile<T>> documents = [];

        foreach (String file in FilesAt(path))
        {
            await using FileStream stream = File.OpenRead(Path.Combine(_gameDirectory, file));
            documents.Add(await ContentFile.ReadAsync(stream, file, type, cancellationToken));
        }

        return documents;
    }

    private static async Task<Mod> ReadModAsync(String gameDirectory, String folder, CancellationToken cancellationToken)
    {
        String manifestFile = $"{folder}/{ManifestFile}";
        String manifestPath = Path.Combine(gameDirectory, manifestFile);

        if (!File.Exists(manifestPath))
        {
            throw new InvalidDataException($"{folder} has no {ManifestFile}, so it can't be loaded as a mod.");
        }

        await using FileStream stream = File.OpenRead(manifestPath);
        FromFile<ModDocument> manifest = await ContentFile.ReadAsync(stream, manifestFile, ContentJsonContext.Default.ModDocument, cancellationToken);

        if (manifest.Value.ContentFormat != ContentFormat)
        {
            throw new InvalidDataException($"{manifestFile}: the mod is for content format {manifest.Value.ContentFormat}, but this version of Richter reads format {ContentFormat}.");
        }

        if (String.IsNullOrWhiteSpace(manifest.Value.Id))
        {
            throw new InvalidDataException($"{manifestFile}: the mod's id can't be empty.");
        }

        return new Mod
        {
            Id = manifest.Value.Id,
            Name = manifest.Value.Name,
            Version = manifest.Value.Version,
            Dependencies = manifest.Value.Dependencies ?? [],
            Path = folder,
        };
    }

    /// <summary> Every mod's file at <paramref name="path"/> that exists, in load order. </summary>
    private IReadOnlyList<String> FilesAt(String path)
    {
        return Mods
            .Select(mod => $"{mod.Path}/{path}")
            .Where(file => File.Exists(Path.Combine(_gameDirectory, file)))
            .ToList();
    }
}
