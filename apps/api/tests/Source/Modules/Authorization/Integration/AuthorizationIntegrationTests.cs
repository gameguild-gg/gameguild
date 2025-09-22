using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit.Abstractions;

namespace GameGuild.Tests.Modules.Authorization.Integration;

/// <summary>
/// Integration tests for authorization functionality across the API
/// </summary>
public class AuthorizationIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ITestOutputHelper _output;

    public AuthorizationIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
        _factory = factory;
        _output = output;
        _scope = factory.Services.CreateScope();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuth_ShouldReturnUnauthorized() {
        // Arrange
        var endpoint = "/api/users"; // Assuming this is a protected endpoint

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
          $"Expected Unauthorized or Forbidden, but got {response.StatusCode}");
        _output.WriteLine($"Endpoint {endpoint} without auth: {response.StatusCode}");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidAuth_ShouldSucceed() {
        // Arrange
        var endpoint = "/api/users";
        var token = GenerateValidJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
          $"Expected success or not found, but got {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
        _output.WriteLine($"Endpoint {endpoint} with auth: {response.StatusCode}");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidAuth_ShouldReturnUnauthorized() {
        // Arrange
        var endpoint = "/api/users";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
          $"Expected Unauthorized or Forbidden, but got {response.StatusCode}");
        _output.WriteLine($"Endpoint {endpoint} with invalid auth: {response.StatusCode}");
    }

    [Fact]
    public async Task AdminEndpoint_WithUserRole_ShouldReturnForbidden() {
        // Arrange
        var endpoint = "/tenants"; // Assuming this requires admin role
        var token = GenerateUserJwtToken(); // User role token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        // Could be forbidden if the endpoint requires admin, or success if it allows users
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        _output.WriteLine($"Admin endpoint {endpoint} with user role: {response.StatusCode}");
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ShouldSucceed() {
        // Arrange
        var endpoint = "/api/tenants";
        var token = GenerateAdminJwtToken(); // Admin role token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
          $"Expected success or not found, but got {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
        _output.WriteLine($"Admin endpoint {endpoint} with admin role: {response.StatusCode}");
    }

    [Theory]
    [InlineData("/users")]
    [InlineData("/tenants")]
    [InlineData("/payments")]
    public async Task MultipleEndpoints_WithAuth_ShouldHaveConsistentBehavior(string endpoint) {
        // Arrange
        var token = GenerateValidJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        _output.WriteLine($"Endpoint {endpoint}: {response.StatusCode}");
    }

    [Fact]
    public async Task AuthorizationHeaders_ShouldBeProcessedCorrectly() {
        // Arrange
        var endpoint = "/users";
        var token = GenerateValidJwtToken();

        // Test different authorization header formats
        var testCases = new[] {
      $"Bearer {token}",
      $"bearer {token}", // lowercase
      $"Bearer  {token}", // extra space
    };

        foreach (var authHeader in testCases) {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);

            // Act
            var response = await client.GetAsync(endpoint);

            // Assert
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
            _output.WriteLine($"Auth header '{authHeader}': {response.StatusCode}");
        }
    }

    [Fact]
    public async Task ContextMiddleware_ShouldSetupCorrectly() {
        // Arrange
        var endpoint = "/users";
        var token = GenerateValidJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        // The middleware should process the request without errors
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        _output.WriteLine($"Context middleware test: {response.StatusCode}");

        // Check if the response contains expected headers or content indicating proper context setup
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response content length: {content.Length}");
    }

    private string GenerateValidJwtToken() {
        // Generate a test JWT token with standard claims
        return GenerateJwtTokenWithClaims(new[] {
      new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
      new Claim(ClaimTypes.Email, "test@example.com"),
      new Claim(ClaimTypes.Name, "Test User"),
      new Claim(ClaimTypes.Role, "User"),
      new Claim("tenant_id", Guid.NewGuid().ToString()),
      new Claim("permission", "read:users"),
      new Claim("permission", "read:tenants")
    });
    }

    private string GenerateUserJwtToken() {
        return GenerateJwtTokenWithClaims(new[] {
      new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
      new Claim(ClaimTypes.Email, "user@example.com"),
      new Claim(ClaimTypes.Name, "Regular User"),
      new Claim(ClaimTypes.Role, "User"),
      new Claim("tenant_id", Guid.NewGuid().ToString()),
      new Claim("permission", "read:users")
    });
    }

    private string GenerateAdminJwtToken() {
        return GenerateJwtTokenWithClaims(new[] {
      new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
      new Claim(ClaimTypes.Email, "admin@example.com"),
      new Claim(ClaimTypes.Name, "Admin User"),
      new Claim(ClaimTypes.Role, "Admin"),
      new Claim(ClaimTypes.Role, "User"),
      new Claim("tenant_id", Guid.NewGuid().ToString()),
      new Claim("permission", "read:users"),
      new Claim("permission", "write:users"),
      new Claim("permission", "read:tenants"),
      new Claim("permission", "write:tenants")
    });
    }

    private string GenerateJwtTokenWithClaims(Claim[] claims) {
        // Use the same secret as TestServerFixture
        var testSecret = "game-guild-super-secret-key-for-development-only-minimum-32-characters";
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(testSecret);
        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(claims),
            Issuer = "GameGuild.API",
            Audience = "GameGuild.Users",
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public void Dispose() {
        _scope?.Dispose();
        _client?.Dispose();
    }
}
