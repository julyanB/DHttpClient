using System.Reflection;

namespace DHttpClient.Extensions
{
    public static class ObjectExtensions
    {
        public static Dictionary<string, string> ToKeyValue(this object obj)
        {
            if (obj is null)
                return new Dictionary<string, string>();

            return obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetValue(obj) != null)
                .ToDictionary(p => p.Name, p => p.GetValue(obj)?.ToString() ?? string.Empty);
        }
    }
}