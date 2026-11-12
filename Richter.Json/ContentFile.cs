using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Richter.Json;

/// <summary> Reads one content file into its document record. </summary>
internal static class ContentFile
{
    /// <summary>
    /// Reads a document from <paramref name="stream"/>. Throws <see cref="InvalidDataException"/>,
    /// naming <paramref name="file"/> and where in it the problem is, if it isn't valid.
    /// </summary>
    internal static async Task<FromFile<T>> ReadAsync<T>(Stream stream, String file, JsonTypeInfo<T> type, CancellationToken cancellationToken)
        where T : class
    {
        T? document;

        try
        {
            document = await JsonSerializer.DeserializeAsync(stream, type, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw ContentErrors.From(exception, file);
        }

        if (document is null)
        {
            throw new InvalidDataException($"{file} holds null.");
        }

        return new FromFile<T>(document, file);
    }
}
