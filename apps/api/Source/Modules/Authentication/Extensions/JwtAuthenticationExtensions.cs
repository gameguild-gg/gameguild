using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Modules.Authentication;

/// <summary> JWT authentication configuration extensions </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary> Configures JWT Bearer authentication </summary>
    public static IServiceCollection AddAuthJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "development-fallback-key-that-is-at-least-32-characters-long-for-testing";
        var issuer = jwtSettings["Issuer"] ?? "GameGuild";
        var audience = jwtSettings["Audience"] ?? "GameGuild-API";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        return services;
    }
}
