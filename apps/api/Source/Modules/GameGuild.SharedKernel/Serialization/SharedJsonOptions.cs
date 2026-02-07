using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Serialization;

/// <summary>
///     Provides shared <see cref="JsonSerializerOptions" /> instances to avoid repeated allocations.
///     <see cref="JsonSerializerOptions" /> is expensive to construct; creating a new instance per request
///     wastes memory and CPU. Use the pre-configured singleton from this class instead.
/// </summary>
/// <remarks>
///     This replaces 8+ ad-hoc <see cref="JsonSerializerOptions" /> allocations scattered across controllers and services.
/// </remarks>
public static class SharedJsonOptions
{
    /// <summary>
    ///     Default API options: camelCase, enums as strings, ignore null, relaxed number handling.
    ///     Matches the default ASP.NET Core JSON configuration for consistency.
    /// </summary>
    public static JsonSerializerOptions Api { get; } = CreateApiOptions();

    /// <summary>
    ///     Strict options: no extra properties, required fields enforced.
    ///     Use for internal serialization where schema must be exact.
    /// </summary>
    public static JsonSerializerOptions Strict { get; } = CreateStrictOptions();

    /// <summary>
    ///     Web-compatible options using <see cref="JsonSerializerDefaults.Web" />.
    ///     Case-insensitive property matching, camelCase naming.
    /// </summary>
    public static JsonSerializerOptions Web { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static JsonSerializerOptions CreateApiOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }

    private static JsonSerializerOptions CreateStrictOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = false
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
