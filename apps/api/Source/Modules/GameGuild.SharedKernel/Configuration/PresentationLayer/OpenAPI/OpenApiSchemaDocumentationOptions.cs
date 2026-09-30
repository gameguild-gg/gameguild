using System.Text.Json;

namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

/// <summary>
///     Optional documentation applied to a generated OpenAPI schema.
/// </summary>
public sealed class OpenApiSchemaDocumentationOptions
{
    public string Description { get; set; } = string.Empty;

    /// <summary>A JSON value matching the schema's wire representation.</summary>
    public string ExampleJson { get; set; } = string.Empty;

    public Dictionary<string, OpenApiPropertyDocumentationOptions> Properties { get; set; } = new();

    public void Validate(string schemaId)
    {
        ValidateJson(ExampleJson, $"schema '{schemaId}' example", optional: true);

        if (Properties is null)
        {
            throw new InvalidOperationException($"OpenAPI schema '{schemaId}' properties cannot be null.");
        }

        foreach (var (name, property) in Properties)
        {
            if (string.IsNullOrWhiteSpace(name) || property is null)
            {
                throw new InvalidOperationException($"OpenAPI schema '{schemaId}' requires a non-empty property name and options.");
            }

            ValidateJson(property.ExampleJson, $"schema '{schemaId}' property '{name}' example", optional: true);
        }
    }

    internal static void ValidateJson(string? json, string context, bool optional = false)
    {
        if (optional && string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(json) || json.Length > 65_536)
        {
            throw new InvalidOperationException($"OpenAPI {context} must contain at most 65536 characters of JSON.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            ValidateNumbers(document.RootElement, context);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"OpenAPI {context} must contain valid JSON.", exception);
        }
    }

    private static void ValidateNumbers(JsonElement value, string context)
    {
        if (value.ValueKind == JsonValueKind.Number
            && !value.TryGetInt64(out _)
            && (!value.TryGetDouble(out var number) || !double.IsFinite(number)))
        {
            throw new InvalidOperationException($"OpenAPI {context} contains a number outside the supported range.");
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                ValidateNumbers(item, context);
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                ValidateNumbers(property.Value, context);
            }
        }
    }
}

/// <summary>
///     Optional documentation applied to a property in a generated OpenAPI schema.
/// </summary>
public sealed class OpenApiPropertyDocumentationOptions
{
    public string Description { get; set; } = string.Empty;

    /// <summary>A JSON value matching the property's wire representation.</summary>
    public string ExampleJson { get; set; } = string.Empty;
}
