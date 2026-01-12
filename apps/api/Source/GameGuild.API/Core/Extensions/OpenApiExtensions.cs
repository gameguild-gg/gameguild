using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using GameGuild.Configuration;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using ApiVersioningOptions = GameGuild.Configuration.PresentationLayer.ApiVersioning.ApiVersioningOptions;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring OpenAPI/Swagger services.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    ///     Sets up OpenAPI/Swagger with configurable options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="options">OpenAPI options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection SetupOpenApi(this IServiceCollection services, IConfiguration configuration,
        OpenApiOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "OpenApi", OpenApiOptions.CreateDefault);
        options.Validate();

        // Add native .NET 9 OpenAPI support
        services.AddOpenApi();

        // Add Swashbuckle for Swagger UI
        services.AddSwaggerGen(c =>
            {
                // If API Versioning is enabled, register a Swagger document per discovered API version
                using var providerScope = services.BuildServiceProvider();
                var provider = providerScope.GetService<IApiVersionDescriptionProvider>();

                if (provider is not null)
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        c.SwaggerDoc(
                            description.GroupName,
                            new OpenApiInfo
                            {
                                Title = options.Title,
                                Version = description.ApiVersion.ToString(),
                                Description = options.Description,
                                Contact = new OpenApiContact
                                {
                                    Name = options.ContactName, Email = options.ContactEmail,
                                    Url = !string.IsNullOrEmpty(options.ContactUrl) ? new Uri(options.ContactUrl) : null
                                }
                            }
                        );
                    }

                    // Ensure only endpoints from the corresponding API version are included in each document
                    // Check API version instead of GroupName to allow custom ApiExplorerSettings GroupName
                    c.DocInclusionPredicate((docName, apiDesc) =>
                        {
                            if (apiDesc.ActionDescriptor is not ControllerActionDescriptor cad)
                                return string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase);

                            if (cad.ControllerTypeInfo.GetCustomAttributes(typeof(ApiVersionAttribute), false)
                                    .FirstOrDefault() is not ApiVersionAttribute apiVersionAttr)
                                return string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase);
                            var version = apiVersionAttr.Versions.FirstOrDefault();

                            return version != null && docName.Equals($"v{version.MajorVersion}",
                                StringComparison.OrdinalIgnoreCase);
                        }
                    );
                }
                else
                {
                    // Fallback to single document when versioning is not configured
                    c.SwaggerDoc(
                        options.Version,
                        new OpenApiInfo
                        {
                            Title = options.Title,
                            Version = options.Version,
                            Description = options.Description,
                            Contact = new OpenApiContact
                            {
                                Name = options.ContactName, Email = options.ContactEmail,
                                Url = !string.IsNullOrEmpty(options.ContactUrl) ? new Uri(options.ContactUrl) : null
                            }
                        }
                    );
                }

                // Configure schema ID generator to use module + type name for guaranteed uniqueness
                // e.g., "GameGuild.Identity.Tenants.TenantSettingsDto" -> "Tenants_TenantSettingsDto"
                // e.g., "GameGuild.Identity.Users.UserDto" -> "Users_UserDto"
                // e.g., "GameGuild.Identity.Authentication.UserDto" -> "Authentication_UserDto"
                c.CustomSchemaIds(type =>
                {
                    var fullName = type.FullName ?? type.Name;
                    var parts = fullName.Split('.');
                    
                    if (type.IsGenericType)
                    {
                        // For generic types, include module + generic type + args
                        var genericTypeName = type.Name.Split('`')[0];
                        var genericArgs = string.Join("", type.GetGenericArguments().Select(t => t.Name));
                        
                        // Find module name (the segment after "GameGuild")
                        var moduleIndex = Array.IndexOf(parts, "GameGuild") + 1;
                        var moduleName = moduleIndex < parts.Length ? parts[moduleIndex] : "";
                        
                        return !string.IsNullOrEmpty(moduleName) 
                            ? $"{moduleName}_{genericTypeName}{genericArgs}" 
                            : $"{genericTypeName}{genericArgs}";
                    }
                    
                    // For GameGuild types, use Module_TypeName pattern
                    // e.g., GameGuild.Identity.Users.UserDto -> Users_UserDto
                    if (parts.Length >= 3 && parts[0] == "GameGuild")
                    {
                        var moduleName = parts[1]; // e.g., "Users", "Authentication", "Tenants"
                        var typeName = parts[^1];  // Last part is the type name
                        return $"{moduleName}_{typeName}";
                    }
                    
                    // For non-GameGuild types (external libraries, etc.)
                    if (parts.Length >= 2)
                    {
                        return $"{parts[^2]}_{parts[^1]}";
                    }
                    
                    return type.Name;
                });

                // Add security definition for JWT Bearer token
                c.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description =
                            "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    }
                );

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                                Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header
                            },
                            new List<string>()
                        }
                    }
                );
            }
        );

        return services;
    }
    
    /// <summary>
    ///     Helper function to normalize schema names.
    ///     Note: We intentionally keep the "Dto" suffix to avoid schema ID conflicts
    ///     between entities (e.g., TenantSettings) and their DTOs (e.g., TenantSettingsDto).
    /// </summary>
    private static string NormalizeSchemaName(string name)
    {
        // DO NOT strip "Dto" suffix - keeping it prevents conflicts between
        // entities and DTOs with similar names (e.g., TenantSettings vs TenantSettingsDto)
        
        // Convert "Result" suffix to "Response" for consistency
        if (name.EndsWith("Result", StringComparison.Ordinal))
            name = name[..^6] + "Response";
        
        return name;
    }

    /// <summary>
    ///     Sets up API versioning with configurable options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="options">API versioning options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection SetupApiVersioning(this IServiceCollection services, IConfiguration configuration,
        ApiVersioningOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ApiVersioning",
            ApiVersioningOptions.CreateDefault);
        options.Validate();

        services.AddApiVersioning(setup =>
                {
                    setup.AssumeDefaultVersionWhenUnspecified = options.AssumeDefaultVersionWhenUnspecified;
                    // Parse DefaultVersion (e.g., "1.0") into ApiVersion
                    var versionParts = options.DefaultVersion.Split('.');
                    var major = int.TryParse(versionParts.ElementAtOrDefault(0) ?? "1", out var mj) ? mj : 1;
                    var minor = int.TryParse(versionParts.ElementAtOrDefault(1) ?? "0", out var mn) ? mn : 0;
                    setup.DefaultApiVersion = new ApiVersion(major, minor);
                    setup.ApiVersionReader = ApiVersioningOptionsBuilder.CreateReader(options.ReadingStrategy, options);
                }
            )
            .AddApiExplorer(setup =>
                {
                    setup.GroupNameFormat = options.GroupNameFormat;
                    setup.SubstituteApiVersionInUrl = options.SubstituteApiVersionInUrl;
                }
            );

        return services;
    }

    /// <summary>
    ///     Sets up API Explorer.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="options">API versioning options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection SetupApiExplorer(this IServiceCollection services, IConfiguration configuration,
        ApiVersioningOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ApiVersioning",
            ApiVersioningOptions.CreateDefault);
        options.Validate();

        // API Explorer is now configured as part of API Versioning setup
        services.AddEndpointsApiExplorer();

        return services;
    }
}
