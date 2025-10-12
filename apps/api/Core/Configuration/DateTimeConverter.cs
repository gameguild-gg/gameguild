using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Custom DateTime converter that handles various date formats consistently.
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrEmpty(value)) { return default; }

        // Try parsing ISO 8601 format first, then fallback to other formats
        if (DateTime.TryParse(value, out var result)) { return result; }

        throw new JsonException($"Unable to parse '{value}' as DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always write in ISO 8601 format
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
