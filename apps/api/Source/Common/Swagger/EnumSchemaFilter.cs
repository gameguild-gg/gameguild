using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.Common.Swagger;

/// <summary>
/// Swagger schema filter that enhances enum documentation by adding descriptions and value mappings.
/// This filter extracts information from XML documentation comments and Description attributes
/// to provide meaningful documentation for enum values when exported as numbers.
/// </summary>
public class EnumSchemaFilter : ISchemaFilter {
  public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
    if (!context.Type.IsEnum)
      return;

    // Clear the default enum values to replace with enhanced documentation
    schema.Enum?.Clear();
    schema.Type = "integer";
    schema.Format = "int32";

    var enumValues = new List<IOpenApiAny>();
    var enumDescriptions = new List<string>();
    var enumNames = Enum.GetNames(context.Type);
    var enumValuesArray = Enum.GetValues(context.Type);

    // Build comprehensive enum documentation
    var enumDocumentation = new List<string>
    {
            $"**{context.Type.Name} Enum Values:**",
            ""
        };

    for (int i = 0; i < enumNames.Length; i++) {
      var enumName = enumNames[i];
      var enumValue = enumValuesArray.GetValue(i);
      var numericValue = Convert.ToInt32(enumValue);

      // Add the numeric value to the schema
      enumValues.Add(new OpenApiInteger(numericValue));

      // Get description from multiple sources
      var description = GetEnumDescription(context.Type, enumName);

      // Format: "0: Planning - Project is in planning phase"
      var formattedDescription = $"{numericValue}: {enumName}";
      if (!string.IsNullOrEmpty(description)) {
        formattedDescription += $" - {description}";
      }

      enumDescriptions.Add(formattedDescription);
      enumDocumentation.Add($"- **{numericValue}**: `{enumName}` - {description ?? "No description available"}");
    }

    // Set the enum values
    schema.Enum = enumValues;

    // Enhanced description with value mappings
    var baseDescription = GetTypeDescription(context.Type) ?? $"Enumeration of {context.Type.Name} values";
    var fullDescription = $"{baseDescription}\n\n{string.Join("\n", enumDocumentation)}";

    schema.Description = fullDescription;

    // Add extension for better tooling support
    var descriptionsArray = new OpenApiArray();
    descriptionsArray.AddRange(enumDescriptions.Select(desc => new OpenApiString(desc)));

    var namesArray = new OpenApiArray();
    namesArray.AddRange(enumNames.Select(name => new OpenApiString(name)));

    // Add x-enum-varnames for TypeScript enum generation with proper member names
    schema.Extensions.Add("x-enum-varnames", namesArray);

    // Add x-enum-descriptions for enhanced documentation
    schema.Extensions.Add("x-enum-descriptions", descriptionsArray);

    // Add example value (first enum value)
    if (enumValues.Count > 0) {
      schema.Example = enumValues[0];
    }
  }

  /// <summary>
  /// Gets the description for an enum value from multiple sources:
  /// 1. Description attribute
  /// 2. XML documentation comments
  /// </summary>
  private static string? GetEnumDescription(Type enumType, string enumName) {
    var field = enumType.GetField(enumName);
    if (field == null)
      return null;

    // Try Description attribute first
    var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
    if (descriptionAttribute != null) {
      return descriptionAttribute.Description;
    }

    // Try to get XML documentation (this would require additional setup to read XML docs)
    // For now, we'll return null if no Description attribute is found
    return null;
  }

  /// <summary>
  /// Gets the description for the enum type itself from XML documentation or attributes
  /// </summary>
  private static string? GetTypeDescription(Type enumType) {
    // Try Description attribute on the type
    var descriptionAttribute = enumType.GetCustomAttribute<DescriptionAttribute>();
    if (descriptionAttribute != null) {
      return descriptionAttribute.Description;
    }

    // Could be extended to read XML documentation for the type
    return null;
  }
}