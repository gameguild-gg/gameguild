using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.Core;

/// <summary>
///     Document filter to customize the OpenAPI document.
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(swaggerDoc);
        ArgumentNullException.ThrowIfNull(context);

        // Add servers configuration
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "https://api.GameGuild.com", Description = "Production API Server" },
            new OpenApiServer { Url = "https://staging-api.GameGuild.com", Description = "Staging API Server" },
            new OpenApiServer { Url = "http://localhost:5000", Description = "Development API Server" }
        };

        // Add common tags
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag { Name = "Health", Description = "Health check endpoints" },
            new OpenApiTag { Name = "Tenants", Description = "Tenant management operations" },
            new OpenApiTag { Name = "Plans", Description = "Subscription plan operations" },
            new OpenApiTag { Name = "Features", Description = "Feature flag operations" },
            new OpenApiTag { Name = "Authentication", Description = "Authentication and authorization" },
            new OpenApiTag { Name = "Users", Description = "User management and CRUD operations" },
            new OpenApiTag { Name = "Metadata", Description = "User metadata and custom fields" },
            new OpenApiTag { Name = "Profiles", Description = "User profiles, avatars, and social links" },
            new OpenApiTag { Name = "Notifications", Description = "User notifications and delivery settings" },
            new OpenApiTag { Name = "Preferences", Description = "User preferences and settings" }
        };

        // Remove any paths that don't have public operations
        var pathsToRemove = swaggerDoc.Paths.Where(path => !path.Value.Operations.Any()).Select(path => path.Key).ToList();

        foreach (var pathToRemove in pathsToRemove) { swaggerDoc.Paths.Remove(pathToRemove); }
    }
}
