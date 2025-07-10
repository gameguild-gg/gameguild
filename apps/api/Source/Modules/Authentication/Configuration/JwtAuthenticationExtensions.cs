using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// JWT authentication configuration extensions following security best practices.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication with comprehensive security settings.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddAuthJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() 
                        ?? throw new InvalidOperationException("JWT configuration is required");
        
        jwtOptions.Validate();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true; // Enforce HTTPS in production
                options.TokenValidationParameters = CreateTokenValidationParameters(jwtOptions);
                options.Events = CreateJwtBearerEvents();
            });

        return services;
    }

    /// <summary>
    /// Creates comprehensive token validation parameters.
    /// </summary>
    private static TokenValidationParameters CreateTokenValidationParameters(JwtOptions jwtOptions)
    {
        return new TokenValidationParameters
        {
            // Issuer validation
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            
            // Audience validation
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            
            // Lifetime validation
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
            
            // Signing key validation
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            
            // Security requirements
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            
            // Algorithm validation
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
    }

    /// <summary>
    /// Creates JWT Bearer event handlers for enhanced security and logging.
    /// </summary>
    private static JwtBearerEvents CreateJwtBearerEvents()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = OnMessageReceived,
            OnAuthenticationFailed = OnAuthenticationFailed,
            OnTokenValidated = OnTokenValidated,
            OnChallenge = OnChallenge
        };
    }

    /// <summary>
    /// Handles token retrieval from various sources (query string for SignalR, etc.).
    /// </summary>
    private static Task OnMessageReceived(MessageReceivedContext context)
    {
        // Allow token in query string for SignalR connections
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) && 
            (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/graphql")))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles authentication failures with proper logging.
    /// </summary>
    private static Task OnAuthenticationFailed(AuthenticationFailedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<AuthenticationFailedContext>>();
        
        logger.LogWarning("JWT authentication failed: {Exception} for path {Path}", 
            context.Exception.Message, 
            context.HttpContext.Request.Path);

        // Don't expose detailed error information in production
        if (context.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            context.Response.Headers.Append("Auth-Error", context.Exception.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles successful token validation.
    /// </summary>
    private static Task OnTokenValidated(TokenValidatedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<TokenValidatedContext>>();
        
        var userId = context.Principal?.FindFirst("sub")?.Value ?? "Unknown";
        logger.LogDebug("JWT token validated successfully for user {UserId}", userId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles authentication challenges.
    /// </summary>
    private static Task OnChallenge(JwtBearerChallengeContext context)
    {
        // Customize challenge response if needed
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds authorization policies for different authentication scenarios.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Policy for public endpoints
            options.AddPolicy("Public", policy => policy.RequireAssertion(_ => true));
            
            // Policy for tenant-specific access
            options.AddPolicy("TenantAccess", policy => 
                policy.RequireAuthenticatedUser()
                      .RequireClaim("tenant_id"));
            
            // Policy for administrative access
            options.AddPolicy("AdminAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "Admin"));
                      
            // Policy for Web3 authenticated users
            options.AddPolicy("Web3Access", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("auth_method", "web3"));
        });

        return services;
    }
}
