using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild;

/// <summary>
/// Operation filter to set default values for Swagger operations.
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var apiDescription = context.ApiDescription;

        // Set operation ID based on method name
        if (string.IsNullOrEmpty(operation.OperationId))
        {
            operation.OperationId = apiDescription.ActionDescriptor.RouteValues["action"] ??
                                    context.MethodInfo.Name;
        }

        // Add default response for 401 Unauthorized
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add(
                "401",
                new OpenApiResponse
                {
                    Description = "Unauthorized - Authentication is required"
                }
            );
        }

        // Add default response for 403 Forbidden  
        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add(
                "403",
                new OpenApiResponse
                {
                    Description = "Forbidden - Insufficient permissions"
                }
            );
        }

        // Add default response for 500 Internal Server Error
        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses.Add(
                "500",
                new OpenApiResponse
                {
                    Description = "Internal Server Error"
                }
            );
        }

        // Add descriptions from DisplayName attributes
        foreach (var parameter in operation.Parameters ?? new List<OpenApiParameter>())
        {
            var parameterInfo = context.MethodInfo.GetParameters()
                .FirstOrDefault(p => p.Name?.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) == true);

            if (parameterInfo != null)
            {
                var displayName = parameterInfo.GetCustomAttribute<DisplayNameAttribute>();
                if (displayName != null && string.IsNullOrEmpty(parameter.Description))
                {
                    parameter.Description = displayName.DisplayName;
                }
            }
        }

        // Set deprecated flag
        var obsoleteAttribute = context.MethodInfo.GetCustomAttribute<ObsoleteAttribute>();
        if (obsoleteAttribute != null)
        {
            operation.Deprecated = true;
        }
    }
}
