using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.Core;

/// <summary>
///     Schema filter to generate enums with integer values and named keys.
///     Uses x-enum-varnames extension for proper TypeScript enum generation.
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            schema.Type = "integer";
            schema.Format = "int32";

            var values = Enum.GetValues(context.Type);
            var enumNames = new OpenApiArray();
            var seenValues = new HashSet<int>();

            foreach (var value in values)
            {
                var intValue = Convert.ToInt32(value);
                if (seenValues.Add(intValue))
                {
                    schema.Enum.Add(new OpenApiInteger(intValue));
                    enumNames.Add(new OpenApiString(value.ToString()));
                }
            }

            // Add x-enum-varnames extension for proper naming in generated code
            schema.Extensions["x-enum-varnames"] = enumNames;
        }
    }
}
