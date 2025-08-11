using System.Security.Claims;
using System.Text.Encodings.Web;
using GameGuild.Modules.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace GameGuild.Tests.Helpers
{
    /// <summary>
    /// Authentication handler that always authenticates requests for testing purposes
    /// </summary>
    public class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>();
            
            // Try to extract claims from Bearer token if present
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                try
                {
                    var token = authHeader["Bearer ".Length..].Trim();
                    var jwtService = Context.RequestServices.GetRequiredService<IJwtTokenService>();
                    var tokenPrincipal = jwtService.ValidateToken(token);
                    
                    if (tokenPrincipal?.Identity?.IsAuthenticated == true)
                    {
                        // Use the claims from the JWT token
                        claims.AddRange(tokenPrincipal.Claims);
                    }
                }
                catch
                {
                    // If JWT validation fails, fall back to default test claims
                }
            }
            
            // If no valid JWT claims were found, use default test claims
            if (claims.Count == 0)
            {
                claims.AddRange(new[]
                {
                    new Claim(ClaimTypes.Name, "Test User"),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Email, "test@example.com"),
                    new Claim("UserId", Guid.NewGuid().ToString()),
                });
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            // Return success with the test ticket
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
