using System.Globalization;

namespace Richter.Json;

/// <summary> Reads the instants written in content files. </summary>
internal static class InstantText
{
    /// <summary>
    /// ISO 8601, with or without an offset. Other formats are refused, because a date like
    /// 01/02/2112 means a different day in different countries.
    /// </summary>
    private static readonly String[] Formats = ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd"];

    /// <summary>
    /// Reads an instant. One without an offset is UTC, so that it means the same instant on every
    /// machine.
    /// </summary>
    /// <param name="text"> The value as written. </param>
    /// <param name="where"> The file and property it came from, which the error message starts with. </param>
    internal static DateTimeOffset Parse(String text, String where)
    {
        if (!DateTimeOffset.TryParseExact(text, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset instant))
        {
            throw new InvalidDataException($"{where} \"{text}\" isn't an ISO 8601 date and time, such as 2112-01-01T00:00:00Z.");
        }

        return instant;
    }
}
