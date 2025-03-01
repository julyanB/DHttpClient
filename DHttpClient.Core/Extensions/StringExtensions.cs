using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DHttpClient.Extensions;

public static class StringExtensions
{
    public static string ToBase64(this string value)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string FromBase64(this string value)
    {
        var base64EncodedBytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
    
    public static string Format(this string value, params object[] args)
    {
        return string.Format(value, args);
    }
    
    public static string Join(this IEnumerable<string> values, string separator = " ")
    {
        return string.Join(separator, values);
    }
    
    public static string ToQueryString(this object obj)
    {
        if (obj is null)
            return string.Empty;
        
        if (obj is Dictionary<string, string> kvps)
        {
            
            var query2 = string.Join("&", kvps.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return "?" + query2;
        }
        
        var keyValuePairs = obj.ToKeyValue();
        var query = string.Join("&", keyValuePairs.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return "?" + query;
    }
    
    // toJson
    public static string ToJson(this object obj, bool originalCasing = false)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented
        };

        if (!originalCasing)
        {
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        }

        return JsonConvert.SerializeObject(obj, settings);
    }

    // fromJson
    public static T ToObject<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json)!;
    }
}