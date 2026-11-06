using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API.Core.OpenApi;

/// <summary>Preserves nullable enum properties when OpenAPI 3.0 serializes referenced schemas.</summary>
internal sealed class NullableEnumSchemaFilter(IOptions<JsonOptions> jsonOptions) : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null || schema.Properties.Count == 0) return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (Nullable.GetUnderlyingType(property.PropertyType)?.IsEnum != true) continue;

            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? jsonOptions.Value.JsonSerializerOptions.PropertyNamingPolicy?.ConvertName(property.Name)
                ?? property.Name;
            if (!schema.Properties.TryGetValue(name, out var propertySchema)) continue;

            // OpenAPI 3.0 ignores siblings of $ref. Put the reference inside allOf
            // so nullable applies to this property without changing the shared enum.
            if (propertySchema.Reference is { } reference)
            {
                propertySchema.Reference = null;
                propertySchema.AllOf = [new OpenApiSchema { Reference = reference }];
            }
            propertySchema.Nullable = true;
        }
    }
}
