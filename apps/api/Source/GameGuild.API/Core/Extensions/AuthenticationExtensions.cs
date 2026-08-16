using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring authentication services
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    ///     Adds JWT authentication and Identity services
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add JWT settings
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

        // Add ASP.NET Core Identity
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequiredUniqueChars = 1;

                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    // User settings
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    options.User.RequireUniqueEmail = true;

                    // Sign in settings
                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                }
            )
            // .AddEntityFrameworkStores<ApplicationDbContext>() // PLANNED: Add Microsoft.AspNetCore.Identity.EntityFrameworkCore package
            .AddDefaultTokenProviders();

        // Add JWT authentication
        services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            )
            .AddJwtBearer(options =>
                {
                    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)) { KeyId = "GameGuild-jwt-key" };

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                }
            );

        // Add authorization
        services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
                options.AddPolicy("RequireTenantAccess", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(claim =>
                            string.Equals(claim.Type, "tenant_id", StringComparison.Ordinal) ||
                            string.Equals(claim.Type, "TenantId", StringComparison.Ordinal))));
            }
        );

        // PLANNED: Register these services once their implementations are finalized.
        // services.AddScoped<IJwtService, JwtService>();
        // services.AddScoped<IAuthService, AuthService>();
        // services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
