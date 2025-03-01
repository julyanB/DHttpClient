using System.Reflection;

namespace DHttpClient.Extensions;

public static class ObjectExtensions
{
    public static Dictionary<string, string> ToKeyValue(this object obj)
    {
        if (obj is null)
            return null!;
        
        var dictionary = new Dictionary<string, string>();
        foreach (var property in obj.GetType().GetProperties())
        {
            dictionary[property.Name] = property.GetValue(obj)?.ToString() ?? string.Empty;
        }

        return obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetValue(obj) != null)
            .Select(p => new KeyValuePair<string, string>(p.Name, p.GetValue(obj)?.ToString()!))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}