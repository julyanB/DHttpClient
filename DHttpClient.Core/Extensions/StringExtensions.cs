using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; 

namespace DHttpClient.Extensions;

public static class StringExtensions
{
    public static string ToBase64(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string FromBase64(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        try
        {
            var base64EncodedBytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Input string is not a valid Base64 string.", nameof(value), ex);
        }
    }

    public static string Format(this string value, params object[] args)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        
        return string.Format(value, args);
    }

    public static string Join(this IEnumerable<string> values, string separator = " ")
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        
        return string.Join(separator, values);
    }

    /// <summary>
    /// Converts an object or dictionary into a URL-encoded query string.
    /// Correctly handles URL encoding of keys and values.
    /// </summary>
    /// <param name="obj">The object or Dictionary<string, string> containing parameters.</param>
    /// <returns>A query string starting with '?' or an empty string if no parameters.</returns>
    public static string ToQueryString(this object? obj)
    {
        if (obj is null)
            return string.Empty;

        IEnumerable<KeyValuePair<string, string>> keyValuePairs;

        if (obj is Dictionary<string, string> dict)
        {
            keyValuePairs = dict;
        }
        else
        {
            keyValuePairs = obj.ToKeyValue(); 
        }

        var encodedPairs = keyValuePairs
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}") 
            .ToList();

        if (!encodedPairs.Any())
            return string.Empty;

        return "?" + string.Join("&", encodedPairs);
    }

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
    };

    private static readonly JsonSerializerOptions OriginalCaseJsonOptions = new()
    {
        PropertyNamingPolicy = null, // Use original casing
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Optionally ignore nulls
    };

    /// <summary>
    /// Serializes an object to a JSON string using System.Text.Json.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="useCamelCase">True to use camelCase naming policy (default), False to use original property names.</param>
    /// <returns>The JSON string representation of the object.</returns>
    public static string ToJson(this object? obj, bool useCamelCase = true)
    {
        if (obj is null) return "null"; 

        var options = useCamelCase ? DefaultJsonOptions : OriginalCaseJsonOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an object of type T using System.Text.Json.
    /// Uses default options (case-insensitive matching).
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or default(T) if JSON is null/empty or represents null.</returns>
    public static T? ToObject<T>(this string? json) 
    {
        if (string.IsNullOrWhiteSpace(json))
            return default; 

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"JSON Deserialization Error: {ex.Message}. JSON: {json}");
            return default;
        }
    }
}