namespace Richter.Json;

/// <summary>
/// The units content files are written in, as multiples of the model's SI units. Files use the
/// units sources publish in, so values can be copied in unchanged.
/// </summary>
internal static class ContentUnits
{
    internal const Double MetresPerKilometre = 1_000;
    internal const Double CubicMetresPerCubicKilometre = 1e9;
    internal const Double SecondsPerDay = 86_400;
}
