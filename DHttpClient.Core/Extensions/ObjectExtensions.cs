using System; // For ArgumentNullException
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DHttpClient.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Converts the public instance properties of an object into a dictionary of string key/value pairs.
        /// Null property values are excluded.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>A dictionary mapping property names to their string representations, or an empty dictionary if obj is null.</returns>
        public static Dictionary<string, string> ToKeyValue(this object? obj)
        {
            if (obj is null)
                return new Dictionary<string, string>();

            return obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetValue(obj) != null) 
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(obj)?.ToString() ?? string.Empty 
                );
        }
    }
}