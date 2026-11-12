namespace Richter.Domain;

/// <summary>
/// Checks numbers as they're set, so bad data fails where it's loaded instead of turning into a
/// wrong position later.
/// </summary>
/// <remarks>
/// The messages name the value and say what it was, because content loaders show them to the
/// players and modders who wrote the data.
/// </remarks>
internal static class Require
{
    internal static Double Finite(Double value, String name)
    {
        if (!Double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be a finite number, but is {value}.");
        }

        return value;
    }

    internal static Double Positive(Double value, String name)
    {
        if (!(Double.IsFinite(value) && value > 0))
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be a finite number above 0, but is {value}.");
        }

        return value;
    }

    /// <summary> Checks that the value is in the range, including both ends. </summary>
    internal static Double Between(Double value, Double minimum, Double maximum, String name)
    {
        if (!(value >= minimum && value <= maximum))
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be from {minimum} to {maximum}, but is {value}.");
        }

        return value;
    }
}
