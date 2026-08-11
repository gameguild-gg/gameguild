using System.Text;
using System.Security.Claims;
using GameGuild.Configuration;
using GameGuild.Configuration.PresentationLayer.Authentication;
using GameGuild.Configuration.PresentationLayer.CORS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using AuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring security-related services
///     (Authentication, Authorization, CORS).
/// </summary>
public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection SetupAuthentication(this IServiceCollection services, IConfiguration configuration,
        AuthenticationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Authentication",
            AuthenticationOptions.CreateDefault);
        options.Validate();

        if (!options.EnableAuthentication) return services;

        // Enable PII logging for development/testing to see detailed errors
        IdentityModelEventSource.ShowPII = true;

        // Configure JWT Bearer authentication with fallback for missing config
        var jwtSecret = configuration["Jwt:Secret"]
            ?? configuration["Jwt:SecretKey"]
            ?? configuration["JwtSettings:SecretKey"]
            ?? configuration["Authentication:JwtSecretKey"]
            ?? throw new InvalidOperationException("JWT secret is not configured. Set 'Jwt:Secret' or 'JwtSettings:SecretKey' in configuration.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"] ?? "GameGuild";
        var jwtAudience = configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"] ?? "GameGuild";

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");
        logger.LogInformation(
            "JWT Authentication Setup: Secret length: {SecretLength}, Issuer: {Issuer}, Audience: {Audience}", 
            jwtSecret.Length, jwtIssuer, jwtAudience);

        // Create symmetric security key with a KeyId
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            { KeyId = "GameGuild-jwt-key" };

        services.AddAuthentication(authOptions =>
                {
                    authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    authOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            )
            .AddJwtBearer(jwtOptions =>
                {
                    jwtOptions.SaveToken = true;
                    jwtOptions.RequireHttpsMetadata = false; // Allow HTTP in development/testing

                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = securityKey,
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        // Map JWT claim names to .NET claim types
                        NameClaimType = "sub",
                        RoleClaimType = ClaimTypes.Role,
                        // Try all keys even if kid doesn't match
                        TryAllIssuerSigningKeys = true
                    };

                    // Add event handlers for debugging
                    jwtOptions.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            using var lf = LoggerFactory.Create(b => b.AddConsole());
                            lf.CreateLogger("GameGuild.API").LogWarning("JWT Authentication Failed: {Message}", context.Exception.Message);

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            using var lf = LoggerFactory.Create(b => b.AddConsole());
                            lf.CreateLogger("GameGuild.API").LogInformation("JWT Token Validated for user: {User}", context.Principal?.Identity?.Name);

                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            // Skip logging for non-API paths to reduce noise
                            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                            if (path == "/" || path.StartsWith("/documentation") || path.StartsWith("/swagger") || 
                                path.StartsWith("/openapi") || path.StartsWith("/health") ||
                                path.StartsWith("/ready") || path.StartsWith("/live") ||
                                path.EndsWith(".ico") || path.EndsWith(".js") || path.EndsWith(".css"))
                            {
                                return Task.CompletedTask;
                            }
                            
                            using var lf = LoggerFactory.Create(b => b.AddConsole());
                            lf.CreateLogger("GameGuild.API").LogDebug("JWT Token Received: {TokenPrefix}...", 
                                context.Token?.Substring(0, Math.Min(20, context.Token?.Length ?? 0)));

                            return Task.CompletedTask;
                        }
                    };
                }
            );

        // Add authorization if enabled
        if (options.EnableAuthorization)
        {
            services.AddAuthorization();
        }

        return services;
    }

    public static IServiceCollection SetupCors(this IServiceCollection services, IConfiguration configuration,
        CorsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Cors", CorsOptions.CreateDefault);
        options.Validate();

        services.AddCors(corsOptions =>
            {
                corsOptions.AddDefaultPolicy(policyBuilder =>
                    {
                        if (options.AllowedOrigins.Length > 0)
                        {
                            policyBuilder.WithOrigins(options.AllowedOrigins);
                        }
                        else
                        {
                            policyBuilder.AllowAnyOrigin();
                        }

                        if (options.AllowedMethods.Length > 0)
                        {
                            policyBuilder.WithMethods(options.AllowedMethods);
                        }
                        else
                        {
                            policyBuilder.AllowAnyMethod();
                        }

                        if (options.AllowedHeaders.Length > 0)
                        {
                            policyBuilder.WithHeaders(options.AllowedHeaders);
                        }
                        else
                        {
                            policyBuilder.AllowAnyHeader();
                        }
                    }
                );
            }
        );

        return services;
    }

    public static IServiceCollection SetupAuthorization(this IServiceCollection services, IConfiguration configuration,
        AuthorizationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Authorization",
            AuthorizationOptions.CreateDefault);
        options.Validate();

        // ===== Configuration Options =====
        services.AddAuthorizationOptions(configuration);

        // ===== Presentation Layer (handlers, tenant context, policy provider) =====
        services.AddAuthorizationPresentation();

        // ===== Rule-Based Authorization (DB-driven, tenant-configurable policies) =====
        services.AddRuleBasedAuthorization();

        // ===== Core ASP.NET Authorization with Built-in Policies =====
        services.AddAuthorization(authzOptions =>
        {
            // Core role-based policies (fallback for common use cases)
            authzOptions.AddPolicy("RequireAdminRole", policy => policy.RequireAssertion(context =>
            {
                var roles = ClaimsExtractor.GetRoles(context.User);
                return roles.Contains("Admin") || roles.Contains("SystemAdmin");
            }));
            authzOptions.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin", "SystemAdmin"));
            authzOptions.AddPolicy("RequireTenantAccess", policy => policy.RequireClaim("TenantId"));
            
            // Authentication policies
            authzOptions.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Anonymous", policy => policy.RequireAssertion(_ => true)); // Always allow
            
            // Tenant policies (require authenticated user and tenant context)
            authzOptions.AddPolicy("TenantMember", policy => policy.RequireAuthenticatedUser().RequireClaim("TenantId"));
            authzOptions.AddPolicy("SystemAdmin", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                {
                    var roles = ClaimsExtractor.GetRoles(context.User);
                    return roles.Contains("SystemAdmin");
                }));
            authzOptions.AddPolicy("TenantAdmin", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                {
                    var roles = ClaimsExtractor.GetRoles(context.User);

                    return ClaimsExtractor.GetTenantId(context.User) is not null &&
                           (roles.Contains("Admin") ||
                            roles.Contains("SystemAdmin") ||
                            roles.Contains("TenantAdmin") ||
                            roles.Contains("Owner"));
                }));
            
            // Admin policies
            authzOptions.AddPolicy("Admin", policy => policy.RequireAssertion(context =>
            {
                var roles = ClaimsExtractor.GetRoles(context.User);
                return roles.Contains("Admin") || roles.Contains("SystemAdmin");
            }));
            authzOptions.AddPolicy("SecureAdmin", policy => policy
                .RequireAssertion(context =>
                {
                    var roles = ClaimsExtractor.GetRoles(context.User);
                    return roles.Contains("Admin") || roles.Contains("SystemAdmin");
                })
                .RequireClaim("mfa_verified", "true"));
            
            // User management policies (require authenticated user)
            authzOptions.AddPolicy("Users.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.Create", policy => policy.RequireAssertion(context => IsUserAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Update", policy => policy.RequireAssertion(context => IsUserAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Delete", policy => policy.RequireAssertion(context => IsUserAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Admin", policy => policy.RequireAssertion(context => IsUserAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Purge", policy => policy.RequireAssertion(context => IsUserAdministrator(context.User)));
            authzOptions.AddPolicy("Users.ReadSelf", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.EditSelf", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.DeleteSelf", policy => policy.RequireAuthenticatedUser());
            
            // Project policies (require authenticated user)
            authzOptions.AddPolicy("Project.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Project.Edit", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Project.Delete", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Project.Owner", policy => policy.RequireAuthenticatedUser());
            
            // Content policies
            authzOptions.AddPolicy("Content.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Content.Edit", policy => policy.RequireAuthenticatedUser());
            
            // Course policies
            authzOptions.AddPolicy("Course.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Course.Manage", policy => policy.RequireAuthenticatedUser());
            
            // Document policies
            authzOptions.AddPolicy("Document.Edit", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }

    private static bool IsUserAdministrator(System.Security.Claims.ClaimsPrincipal user)
    {
        var roles = ClaimsExtractor.GetRoles(user);

        return roles.Contains("Admin") ||
               roles.Contains("SystemAdmin") ||
               roles.Contains("TenantAdmin") ||
               roles.Contains("Owner");
    }
}
