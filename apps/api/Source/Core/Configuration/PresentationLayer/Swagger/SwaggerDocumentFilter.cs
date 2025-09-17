using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace GameGuild;

/// <summary> Document filter to customize the OpenAPI document. </summary>
public class SwaggerDocumentFilter : IDocumentFilter {
  public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context) {
    ArgumentNullException.ThrowIfNull(swaggerDoc);
    ArgumentNullException.ThrowIfNull(context);

    // Add servers configuration
    swaggerDoc.Servers = new List<OpenApiServer> {
      new OpenApiServer { Url = "https://api.GameGuild.com", Description = "Production API Server" },
      new OpenApiServer { Url = "https://staging-api.GameGuild.com", Description = "Staging API Server" },
      new OpenApiServer { Url = "http://localhost:5000", Description = "Development API Server" },
    };

    // Add common tags
    swaggerDoc.Tags = new List<OpenApiTag> {
      new OpenApiTag { Name = "Health", Description = "Health check endpoints" },
      new OpenApiTag { Name = "Tenants", Description = "Tenant management operations" },
      new OpenApiTag { Name = "Plans", Description = "Subscription plan operations" },
      new OpenApiTag { Name = "Features", Description = "Feature flag operations" },
      new OpenApiTag { Name = "Authentication", Description = "Authentication and authorization" },
    };

    // Remove any paths that don't have public operations
    var pathsToRemove = swaggerDoc.Paths.Where(path => !path.Value.Operations.Any()).Select(path => path.Key).ToList();

    foreach (var pathToRemove in pathsToRemove) { swaggerDoc.Paths.Remove(pathToRemove); }
  }
}
