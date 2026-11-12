using System.Text.Json;
using System.Text.RegularExpressions;

namespace Richter.Json;

/// <summary>
/// Turns errors from reading content files into messages a player or modder can act on: where
/// the problem is, and what it is, without .NET type names.
/// </summary>
/// <remarks>
/// System.Text.Json gives no structured reason for an error, only an English message, so the
/// known messages are matched by pattern. If a .NET update changes one, the error still shows,
/// in .NET's own words with the location added.
/// </remarks>
internal static partial class ContentErrors
{
    internal static InvalidDataException From(JsonException exception)
    {
        String path = exception.Path is null or "$" ? "" : exception.Path.TrimStart('$', '.');
        String where = exception.LineNumber is Int64 line ? $"Line {line + 1}" : "The file";

        if (path.Length > 0)
        {
            where = $"{where}, at {path}";
        }

        return new InvalidDataException($"{where}: {Describe(exception.Message)}", exception);
    }

    /// <summary>
    /// The message of an <see cref="ArgumentException"/> from the domain's checks, without the
    /// parameter name and value that .NET appends.
    /// </summary>
    internal static String Describe(ArgumentException exception)
    {
        String message = exception.Message;
        Int32 parameter = message.IndexOf(" (Parameter '", StringComparison.Ordinal);

        return parameter < 0 ? message : message[..parameter];
    }

    private static String Describe(String message)
    {
        String withoutLocation = LocationSuffix().Replace(message, "");
        Match unknown = UnknownProperty().Match(withoutLocation);
        Match missing = MissingProperties().Match(withoutLocation);
        Match nullValue = NullValue().Match(withoutLocation);
        String description;

        if (unknown.Success)
        {
            description = $"there's no property called \"{unknown.Groups["name"].Value}\". Check its spelling.";
        }
        else if (missing.Success)
        {
            description = $"missing {missing.Groups["names"].Value.Replace('\'', '"')}.";
        }
        else if (nullValue.Success)
        {
            description = $"\"{nullValue.Groups["name"].Value}\" can't be null.";
        }
        else if (withoutLocation.StartsWith("The JSON value could not be converted to ", StringComparison.Ordinal))
        {
            description = WrongKindOfValue(withoutLocation);
        }
        else
        {
            description = withoutLocation;
        }

        return description;
    }

    /// <remarks>
    /// The message names the expected type only sometimes. For a property set through a record's
    /// initialiser it names the record instead, so anything else gets the general message.
    /// </remarks>
    private static String WrongKindOfValue(String message)
    {
        String description;

        if (message.Contains("System.String", StringComparison.Ordinal))
        {
            description = "this should be text in quotes.";
        }
        else if (message.Contains("System.Double", StringComparison.Ordinal) || message.Contains("System.Int32", StringComparison.Ordinal))
        {
            description = "this should be a number.";
        }
        else
        {
            description = "this value is the wrong kind. Check whether it should be a number or text in quotes.";
        }

        return description;
    }

    [GeneratedRegex(@"\s*(Path: .*|LineNumber: .*)$", RegexOptions.Singleline)]
    private static partial Regex LocationSuffix();

    [GeneratedRegex(@"^The JSON property '(?<name>.+)' could not be mapped to any \.NET member")]
    private static partial Regex UnknownProperty();

    [GeneratedRegex(@"^JSON deserialization for type '.+' was missing required properties(?: including)?: (?<names>.+?)\.?$")]
    private static partial Regex MissingProperties();

    [GeneratedRegex(@"^The (?:property or field|constructor parameter) '(?<name>.+?)' on type '.+' doesn't allow")]
    private static partial Regex NullValue();
}
