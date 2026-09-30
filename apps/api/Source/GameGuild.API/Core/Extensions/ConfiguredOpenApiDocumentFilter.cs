using System.Text.Json;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API;

/// <summary>Applies configured extension data and schema documentation to generated documents.</summary>
internal sealed class ConfiguredOpenApiDocumentFilter(OpenApiOptions options) : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var (name, json) in options.Extensions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            document.Extensions[name] = ParseJson(json);
        }

        if (document.Components?.Schemas is not { } schemas)
        {
            return;
        }

        foreach (var (schemaId, configured) in options.Schemas)
        {
            if (!schemas.TryGetValue(schemaId, out var schema))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(configured.Description))
            {
                schema.Description = configured.Description;
            }

            if (!string.IsNullOrWhiteSpace(configured.ExampleJson))
            {
                schema.Example = ParseJson(configured.ExampleJson);
            }

            foreach (var (propertyName, propertyOptions) in configured.Properties)
            {
                if (schema.Properties?.TryGetValue(propertyName, out var property) != true || property is null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(propertyOptions.Description))
                {
                    property.Description = propertyOptions.Description;
                }

                if (!string.IsNullOrWhiteSpace(propertyOptions.ExampleJson))
                {
                    property.Example = ParseJson(propertyOptions.ExampleJson);
                }
            }
        }
    }

    private static IOpenApiAny ParseJson(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return ConvertValue(parsed.RootElement);
    }

    private static IOpenApiAny ConvertValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var objectValue = new OpenApiObject();
                foreach (var property in element.EnumerateObject())
                {
                    objectValue[property.Name] = ConvertValue(property.Value);
                }
                return objectValue;
            case JsonValueKind.Array:
                var arrayValue = new OpenApiArray();
                foreach (var item in element.EnumerateArray())
                {
                    arrayValue.Add(ConvertValue(item));
                }
                return arrayValue;
            case JsonValueKind.String:
                return new OpenApiString(element.GetString());
            case JsonValueKind.Number when element.TryGetInt32(out var integer):
                return new OpenApiInteger(integer);
            case JsonValueKind.Number when element.TryGetInt64(out var longInteger):
                return new OpenApiLong(longInteger);
            case JsonValueKind.Number:
                return new OpenApiDouble(element.GetDouble());
            case JsonValueKind.True:
                return new OpenApiBoolean(true);
            case JsonValueKind.False:
                return new OpenApiBoolean(false);
            case JsonValueKind.Null:
                return new OpenApiNull();
            default:
                throw new InvalidOperationException("Unsupported OpenAPI extension JSON value.");
        }
    }
}
