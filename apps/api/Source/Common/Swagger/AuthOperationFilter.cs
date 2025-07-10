using System.Reflection;
using GameGuild.Modules.Authentication.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace GameGuild.Common;

/// <summary>
/// Swagger operation filter to automatically add security requirements based on [Public] attribute
/// </summary>
public class AuthOperationFilter : IOperationFilter {
  public void Apply(OpenApiOperation operation, OperationFilterContext context) {
    // Check if the endpoint has the [Public] attribute
    var hasPublicAttribute = context.MethodInfo
                                    .GetCustomAttributes<PublicAttribute>(true)
                                    .Any(attr => attr.IsPublic);

    // Also check if the controller has the [Public] attribute
    var controllerHasPublicAttribute = context.MethodInfo.DeclaringType?
                                              .GetCustomAttributes<PublicAttribute>(true)
                                              .Any(attr => attr.IsPublic) ??
                                       false;

    // If neither the method nor the controller has [Public] attribute, require authentication
    if (!hasPublicAttribute && !controllerHasPublicAttribute)
      operation.Security = new List<OpenApiSecurityRequirement> {
        new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } }
      };
    // If the endpoint is public, explicitly clear any security requirements
    else
      operation.Security?.Clear();
  }
}
