using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Centralized JSON serializer configuration for the entire application.
/// Ensures consistent camelCase naming and serialization behavior across all components.
/// </summary>
public static class JsonSerializerConfiguration {
    /// <summary>
    /// Gets the standard JSON serializer options used throughout the application.
    /// </summary>
    public static JsonSerializerOptions StandardOptions => new() {
        // Global camelCase naming policy for all JSON serialization/deserialization
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,

        // Enhanced JSON options for better API consistency
        WriteIndented = true,
        PropertyNameCaseInsensitive = true, // Allow flexible input (both camelCase and PascalCase)
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,

        // Allow trailing commas and comments for better developer experience
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,

        // Add converters
        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new DateTimeConverter(),
            new GuidConverter()
        }
    };

    /// <summary>
    /// Gets production-optimized JSON serializer options (no indentation, faster performance).
    /// </summary>
    public static JsonSerializerOptions ProductionOptions => new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        WriteIndented = false, // No indentation for production

        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new DateTimeConverter(),
            new GuidConverter()
        }
    };

    /// <summary>
    /// Configures MVC JSON options with standard settings.
    /// </summary>
    public static void ConfigureMvcJsonOptions(Microsoft.AspNetCore.Mvc.JsonOptions options) {
        var standardOptions = StandardOptions;

        options.JsonSerializerOptions.PropertyNamingPolicy = standardOptions.PropertyNamingPolicy;
        options.JsonSerializerOptions.DictionaryKeyPolicy = standardOptions.DictionaryKeyPolicy;
        options.JsonSerializerOptions.WriteIndented = standardOptions.WriteIndented;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = standardOptions.PropertyNameCaseInsensitive;
        options.JsonSerializerOptions.DefaultIgnoreCondition = standardOptions.DefaultIgnoreCondition;
        options.JsonSerializerOptions.NumberHandling = standardOptions.NumberHandling;
        options.JsonSerializerOptions.ReadCommentHandling = standardOptions.ReadCommentHandling;
        options.JsonSerializerOptions.AllowTrailingCommas = standardOptions.AllowTrailingCommas;

        // Add converters
        foreach (var converter in standardOptions.Converters) {
            options.JsonSerializerOptions.Converters.Add(converter);
        }
    }

    /// <summary>
    /// Configures HttpClient JSON options for consistent external API communication.
    /// </summary>
    public static void ConfigureHttpClientJsonOptions(IServiceCollection services) {
        services.ConfigureHttpJsonOptions(options => {
            var standardOptions = StandardOptions;

            options.SerializerOptions.PropertyNamingPolicy = standardOptions.PropertyNamingPolicy;
            options.SerializerOptions.DictionaryKeyPolicy = standardOptions.DictionaryKeyPolicy;
            options.SerializerOptions.PropertyNameCaseInsensitive = standardOptions.PropertyNameCaseInsensitive;
            options.SerializerOptions.DefaultIgnoreCondition = standardOptions.DefaultIgnoreCondition;
            options.SerializerOptions.NumberHandling = standardOptions.NumberHandling;

            // Add converters
            foreach (var converter in standardOptions.Converters) {
                options.SerializerOptions.Converters.Add(converter);
            }
        });
    }
}

/// <summary>
/// Custom DateTime converter that handles various date formats consistently.
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime> {
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) {
            return default;
        }

        // Try parsing ISO 8601 format first, then fallback to other formats
        if (DateTime.TryParse(value, out var result)) {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' as DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        // Always write in ISO 8601 format
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>
/// Custom GUID converter that handles both string and GUID formats.
/// </summary>
public class GuidConverter : JsonConverter<Guid> {
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) {
            return default;
        }

        if (Guid.TryParse(value, out var result)) {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' as Guid.");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}
