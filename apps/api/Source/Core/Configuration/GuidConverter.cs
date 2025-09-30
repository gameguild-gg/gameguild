using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Custom GUID converter that handles both string and GUID formats.
/// </summary>
public class GuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrEmpty(value)) { return default; }

        if (Guid.TryParse(value, out var result)) { return result; }

        throw new JsonException($"Unable to parse '{value}' as Guid.");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) { writer.WriteStringValue(value.ToString()); }
}
