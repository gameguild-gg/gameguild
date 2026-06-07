using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace GameGuild;

/// <summary>
///     Converts between flexible JSON payload dictionaries and runtime object dictionaries.
/// </summary>
public static class JsonValueDictionary
{
    public static Dictionary<string, JsonElement> ToJsonElements(IReadOnlyDictionary<string, object?>? source)
    {
        if (source == null || source.Count == 0)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, JsonElement>(source.Count, StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            result[key] = value is JsonElement element
                ? element.Clone()
                : JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object), SharedJsonOptions.Api);
        }

        return result;
    }

    public static Dictionary<string, object?> ToObjects(IReadOnlyDictionary<string, JsonElement>? source)
    {
        if (source == null || source.Count == 0)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, object?>(source.Count, StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            result[key] = ToObject(value);
        }

        return result;
    }

    public static Dictionary<string, JsonElement> GetObjectMap(IReadOnlyDictionary<string, JsonElement>? source, string key)
    {
        if (!TryGetValue(source, key, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            result[property.Name] = property.Value.Clone();
        }

        return result;
    }

    public static string? GetString(IReadOnlyDictionary<string, JsonElement>? source, string key, string? defaultValue = null)
    {
        if (!TryGetValue(source, key, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool TryGetValue(IReadOnlyDictionary<string, JsonElement>? source, string key, out JsonElement value)
    {
        if (source != null)
        {
            if (source.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var pair in source)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    [ExcludeFromCodeCoverage]
    private static object? ToObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => ToNumber(value),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => value.EnumerateObject()
                .ToDictionary(property => property.Name, property => ToObject(property.Value), StringComparer.Ordinal),
            JsonValueKind.Array => value.EnumerateArray().Select(ToObject).ToList(),
            _ => value.ToString()
        };
    }

    private static object ToNumber(JsonElement value)
    {
        if (value.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        if (value.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        if (value.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return value.GetDouble();
    }
}
