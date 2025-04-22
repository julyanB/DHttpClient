using System; 
using System.Globalization;

namespace DHttpClient.Extensions;

public static class NumericExtensions
{
    /// <summary>
    /// Converts a string representation of a number to a specified numeric type using InvariantCulture.
    /// Returns the default value for the numeric type if conversion fails.
    /// </summary>
    /// <typeparam name="TNumber">The target numeric type (e.g., int, double, decimal).</typeparam>
    /// <param name="value">The string to convert.</param>
    /// <returns>The converted number or the default value of TNumber on failure.</returns>
    public static TNumber ToNumber<TNumber>(this string? value) where TNumber : struct, IConvertible 
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        try
        {
            return (TNumber)Convert.ChangeType(value, typeof(TNumber), CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            // String was not in a valid format
            return default;
        }
        catch (InvalidCastException)
        {
            // Conversion is not supported
            return default;
        }
        catch (OverflowException)
        {
            // Value is out of range for the target type
            return default;
        }
    }
}