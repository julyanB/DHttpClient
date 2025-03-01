using System.Globalization;

namespace DHttpClient.Extensions;

public static class NumericExtensions
{
    public static TNumber ToNumber<TNumber>(this string value)
    {
        try
        {
            return (TNumber)Convert.ChangeType(value, typeof(TNumber), CultureInfo.InvariantCulture);
        }
        catch
        {
            return default!;
        }
    }
}

