namespace Richter.Json;

/// <summary>
/// Something read from a content file, kept with the file's path so that errors found after
/// mods are merged can still say where it came from.
/// </summary>
internal record FromFile<T>
{
    public FromFile(T value, String file)
    {
        Value = value;
        File = file;
    }

    public T Value { get; }

    /// <summary>
    /// The file's path under the game's directory, such as
    /// <c>Mods/core/scenarios/default.json</c>.
    /// </summary>
    public String File { get; }
}
