using System.Security.Claims;
using System.Text;
using GameGuild.Configuration;
using GameGuild.Configuration.ApplicationLayer;
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

        var isDevelopmentOrTesting = IsDevelopmentOrTesting(configuration);
        IdentityModelEventSource.ShowPII = isDevelopmentOrTesting;

        var resolvedJwtOptions = JwtOptionsResolver.CreateValidated(configuration);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(resolvedJwtOptions.SecretKey))
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
                    jwtOptions.MapInboundClaims = false;
                    jwtOptions.RequireHttpsMetadata = !isDevelopmentOrTesting;

                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = resolvedJwtOptions.ValidateIssuerSigningKey,
                        IssuerSigningKey = securityKey,
                        ValidateIssuer = resolvedJwtOptions.ValidateIssuer,
                        ValidIssuer = resolvedJwtOptions.Issuer,
                        ValidateAudience = resolvedJwtOptions.ValidateAudience,
                        ValidAudience = resolvedJwtOptions.Audience,
                        ValidateLifetime = resolvedJwtOptions.ValidateLifetime,
                        ClockSkew = TimeSpan.FromSeconds(resolvedJwtOptions.ClockSkewSeconds),
                        NameClaimType = "sub",
                        RoleClaimType = "role",
                        TryAllIssuerSigningKeys = true
                    };

                    jwtOptions.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                                .CreateLogger("GameGuild.API")
                                .LogWarning("JWT authentication failed: {Message}", context.Exception.Message);

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                                .CreateLogger("GameGuild.API")
                                .LogDebug("JWT token validated for subject {Subject}",
                                    context.Principal?.FindFirst("sub")?.Value);

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
            authzOptions.AddPolicy("RequireAdminRole", policy =>
                policy.RequireAssertion(context => IsAdministrator(context.User)));
            authzOptions.AddPolicy("RequireUserRole", policy =>
                policy.RequireAssertion(context => IsUser(context.User)));
            authzOptions.AddPolicy("RequireTenantAccess", policy =>
                policy.RequireAssertion(context => HasTenantClaim(context.User)));

            // Authentication policies
            authzOptions.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Anonymous", policy => policy.RequireAssertion(_ => true)); // Always allow

            // Tenant policies (require authenticated user and tenant context)
            authzOptions.AddPolicy("TenantMember", policy =>
                policy.RequireAuthenticatedUser().RequireAssertion(context => HasTenantClaim(context.User)));
            authzOptions.AddPolicy("SystemAdmin", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HasRole(context.User, "SystemAdmin")));
            authzOptions.AddPolicy("TenantAdmin", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => HasTenantClaim(context.User) && IsTenantAdministrator(context.User)));

            authzOptions.AddPolicy("Admin", policy =>
                policy.RequireAssertion(context => IsAdministrator(context.User)));
            authzOptions.AddPolicy("SecureAdmin", policy => policy
                .RequireAssertion(context => IsAdministrator(context.User))
                .RequireClaim("mfa_verified", "true"));

            // User management policies (require authenticated user)
            authzOptions.AddPolicy("Users.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.Create", policy =>
                policy.RequireAssertion(context => IsTenantAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Update", policy =>
                policy.RequireAssertion(context => IsTenantAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Delete", policy =>
                policy.RequireAssertion(context => IsTenantAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Admin", policy =>
                policy.RequireAssertion(context => IsTenantAdministrator(context.User)));
            authzOptions.AddPolicy("Users.Purge", policy =>
                policy.RequireAssertion(context => IsTenantAdministrator(context.User)));
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

    private static bool HasTenantClaim(ClaimsPrincipal user) =>
        !string.IsNullOrWhiteSpace(ClaimsExtractor.GetTenantId(user));

    private static bool HasRole(ClaimsPrincipal user, string role) =>
        ClaimsExtractor.GetRoles(user).Contains(role);

    private static bool IsAdministrator(ClaimsPrincipal user) =>
        HasRole(user, "Admin") || HasRole(user, "SystemAdmin");

    private static bool IsTenantAdministrator(ClaimsPrincipal user) =>
        IsAdministrator(user) || HasRole(user, "TenantAdmin");

    private static bool IsUser(ClaimsPrincipal user) =>
        HasRole(user, "User") || IsTenantAdministrator(user);

    private static bool IsDevelopmentOrTesting(IConfiguration configuration)
    {
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
                              ?? configuration["DOTNET_ENVIRONMENT"];

        return string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase)
               || string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase)
               || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }
}
