using System.Text.Json;
using Train.Solver.Common.Serialization;

namespace Train.Solver.Common.Extensions;
public static class ObjectExtensions
{
    public static string ToJson(this object obj)
    {
        if (obj == null)
        {
            return string.Empty;
        }
        try
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                Converters = { new BigIntegerConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to serialize object to JSON.", ex);
        }
    }

    public static T FromJson<T>(this string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return default!;
        }
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                Converters = { new BigIntegerConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            })!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize JSON to object.", ex);
        }
    }
}
