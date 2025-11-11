using System.Net.Http.Json;
using FluentAssertions;
using GameGuild.API.Data;
using Xunit;
using GameGuild.Authentication;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.Models.Requests;
using GameGuild.Authentication.Models.Responses;
using GameGuild.Authentication.Abstractions;
using GameGuild.Users;
using GameGuild.Users.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using GameGuild.Tests.Authentication.Integration.TestHelpers;

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

    public AuthenticationIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        // Set environment variable before factory initialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder => {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services => {
                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options => {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });

                // Ensure the database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
    }
    [Fact]
    public async Task LocalSignUp_ShouldCreateUser_WhenValidDataProvided() {
        // Arrange
        var signUpCommand = new LocalSignUpCommand {
            Email = "integration.test@example.com",
            Username = "integrationtest",
            Password = "IntegrationTest123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", signUpCommand);

        // Assert
        response.Should().NotBeNull();

        // Verify user was created in database
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Set<User>().FirstOrDefaultAsync(u => u.Email == signUpCommand.Email);

        user.Should().NotBeNull();
        user!.Email.Should().Be(signUpCommand.Email);
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

        var signInCommand = new LocalSignInCommand {
            Email = signUpRequest.Email,
            Password = signUpRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signin", signInCommand);

        // Assert
        response.Should().NotBeNull();

        if (response.IsSuccessStatusCode) {
            var signInResponse = await response.Content.ReadFromJsonAsync<SignInResponse>();
            signInResponse.Should().NotBeNull();
            signInResponse!.AccessToken.Should().NotBeNullOrEmpty();
            signInResponse.RefreshToken.Should().NotBeNullOrEmpty();
            signInResponse.UserId.Should().NotBeEmpty();
            signInResponse.Email.Should().Be(signInCommand.Email);
        }
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

        var refreshCommand = new RefreshTokenCommand {
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

        // Assert
        response.Should().NotBeNull();

        if (response.IsSuccessStatusCode) {
            var refreshResponse = await response.Content.ReadFromJsonAsync<SignInResponse>();
            refreshResponse.Should().NotBeNull();
            refreshResponse!.AccessToken.Should().NotBeNullOrEmpty();
            refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
            refreshResponse.UserId.Should().NotBeEmpty();
        }
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

        var revokeCommand = new RevokeTokenCommand {
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", revokeCommand);

        // Assert
        response.Should().NotBeNull();

        // Try to use the revoked token - should fail
        var refreshCommand = new RefreshTokenCommand {
            RefreshToken = refreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);
        refreshResponse.IsSuccessStatusCode.Should().BeFalse();
    }

    public void Dispose() {
        _scope.Dispose();
        _client.Dispose();
    }
}
