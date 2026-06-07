using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using GameGuild.API.Database;
using Xunit;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// Integration tests for authentication features
/// Tests JWT token generation, refresh, and validation
/// </summary>
public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private static readonly string DatabaseName = $"AuthTestDb_{Guid.NewGuid()}";

    public AuthenticationIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        // Set environment variable before factory initialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "ThisIsASecretKeyForIntegrationTestingThatIsLongEnoughToProhibitErrors");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GameGuild");
        Environment.SetEnvironmentVariable("Jwt__Audience", "GameGuild.Users");

        _factory = factory.WithWebHostBuilder(builder => {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database with shared name for all requests
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(o => { });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();

        // Ensure the database is created
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }
    [Fact]
    public async Task LocalSignUp_ShouldCreateUser_WhenValidDataProvided() {
        // Arrange
        var signUpRequest = new LocalSignUpRequest {
            Email = "integration.test@example.com",
            Username = "integrationtest",
            Password = "IntegrationTest123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/auth/sign-up", signUpRequest);

        // Assert
        response.Should().NotBeNull();

        // Check response status
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"Response status: {response.StatusCode}, Content: {content}");

        // Verify auth user was created in database
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Set<GameGuild.Identity.Users.User>().FirstOrDefaultAsync(u => u.Email == signUpRequest.Email);

        user.Should().NotBeNull();
        user!.Email.Should().Be(signUpRequest.Email);
    }

    [Fact]
    public async Task LocalSignIn_ShouldReturnToken_WhenValidCredentialsProvided() {
        // Arrange - First create a user
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();

        var signUpRequest = new LocalSignUpRequest {
            Email = "signin.test@example.com",
            Username = "signintest",
            Password = "SignInTest123!"
        };

        await authService.LocalSignUpAsync(signUpRequest);

        var signInCommand = new LocalSignInRequest {
            Email = signUpRequest.Email,
            Password = signUpRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/auth/sign-in", signInCommand);

        // Assert
        response.Should().NotBeNull();
        var signInContent = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"Response status: {response.StatusCode}, Content: {signInContent}");

        var signInResponse = await response.Content.ReadFromJsonAsync<SignInResponse>();
        signInResponse.Should().NotBeNull();
        signInResponse!.AccessToken.Should().NotBeNullOrEmpty();
        signInResponse.RefreshToken.Should().NotBeNullOrEmpty();
        signInResponse.UserId.Should().NotBeEmpty();
        signInResponse.Email.Should().Be(signInCommand.Email);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewToken_WhenValidRefreshTokenProvided() {
        // Arrange - Create user and get refresh token
        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();

        var signUpRequest = new LocalSignUpRequest {
            Email = "refresh.test@example.com",
            Username = "refreshtest",
            Password = "RefreshTest123!"
        };

        var signUpResult = await authService.LocalSignUpAsync(signUpRequest);
        var refreshToken = signUpResult.RefreshToken;

        var refreshCommand = new RefreshTokenRequest {
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/auth/tokens:refresh", refreshCommand);

        // Assert
        response.Should().NotBeNull();
        var refreshContent = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"Response status: {response.StatusCode}, Content: {refreshContent}");

        var refreshResponse = await response.Content.ReadFromJsonAsync<SignInResponse>();
        refreshResponse.Should().NotBeNull();
        refreshResponse!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResponse.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RevokeToken_ShouldInvalidateRefreshToken() {
        // Arrange - Create user and get refresh token
        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();

        var signUpRequest = new LocalSignUpRequest {
            Email = "revoke.test@example.com",
            Username = "revoketest",
            Password = "RevokeTest123!"
        };

        var signUpResult = await authService.LocalSignUpAsync(signUpRequest);
        var refreshToken = signUpResult.RefreshToken;

        var revokeCommand = new RevokeRefreshTokenRequest {
            Token = refreshToken
        };

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signUpResult.AccessToken);
        var response = await _client.PostAsJsonAsync("/v1/auth/tokens:revoke", revokeCommand);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();

        // Try to use the revoked token - should fail
        var refreshCommand = new RefreshTokenRequest {
            RefreshToken = refreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("/v1/auth/tokens:refresh", refreshCommand);
        refreshResponse.IsSuccessStatusCode.Should().BeFalse();
    }

    public void Dispose() {
        _scope.Dispose();
        _client.Dispose();
    }
}
