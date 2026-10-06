using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using IAuthService = GameGuild.Identity.Authentication.IAuthService;
using LocalSignUpRequest = GameGuild.Identity.Authentication.LocalSignUpRequest;
using SignInResponse = GameGuild.Identity.Authentication.SignInResponse;
using User = GameGuild.Identity.Users.User;

namespace GameGuild.API.UnitTests.Endpoints;

public sealed class AuthenticationEndpointBindingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SignIn_UsesBCryptAndRecordsOnlySuccessfulLogins(bool correctPassword)
    {
        await using var app = await CreateAppAsync();
        var password = Guid.NewGuid().ToString("N");
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var user = User.CreateWithPassword("legacy@example.test", "Legacy User", passwordHash, "legacy-user");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
        }

        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync("/auth/sign-in",
            new SignInRequest(user.Email, correctPassword ? password : password + "-incorrect"));

        response.StatusCode.Should().Be(correctPassword ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        if (correctPassword)
        {
            var body = await response.Content.ReadFromJsonAsync<SignInResponseDto>();
            body!.User.Id.Should().Be(user.Id);
            body.User.Email.Should().Be(user.Email);
            body.AccessToken.Should().NotBeNullOrWhiteSpace();
            body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        }

        await using var verificationScope = app.Services.CreateAsyncScope();
        var persisted = await verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<User>().SingleAsync(candidate => candidate.Id == user.Id);
        persisted.LastLoginAt.HasValue.Should().Be(correctPassword);
        persisted.PasswordHash.Should().Be(passwordHash);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SignIn_RejectsPasswordlessAndUnknownAccounts(bool existingAccount)
    {
        await using var app = await CreateAppAsync();
        const string email = "passwordless@example.test";
        if (existingAccount)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Set<User>().Add(new User { Email = email, Name = "Passwordless User", Username = "passwordless-user" });
            await context.SaveChangesAsync();
        }

        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(email, Guid.NewGuid().ToString("N")));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task SignUp_BindsTheRequestAndPreservesTheServiceResponse()
    {
        var request = new SignUpRequest("created@example.test", Guid.NewGuid().ToString("N"), "created-user");
        var expected = new SignInResponse
        {
            UserId = Guid.NewGuid(), Email = request.Email, ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            AccessToken = Guid.NewGuid().ToString("N"), RefreshToken = Guid.NewGuid().ToString("N")
        };
        var auth = new Mock<IAuthService>();
        auth.Setup(service => service.LocalSignUpAsync(It.IsAny<LocalSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        await using var app = await CreateAppAsync(auth.Object);
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/auth/sign-up", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.Should().Be($"/users/{expected.UserId}");
        var body = await response.Content.ReadFromJsonAsync<SignInResponseDto>();
        body!.User.Id.Should().Be(expected.UserId);
        body.User.Username.Should().Be(request.Username);
        body.AccessToken.Should().Be(expected.AccessToken);
        body.RefreshToken.Should().Be(expected.RefreshToken);
        auth.Verify(service => service.LocalSignUpAsync(It.Is<LocalSignUpRequest>(input =>
            input.Email == request.Email && input.Password == request.Password && input.Username == request.Username),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("invalid", "valid-length-password")]
    [InlineData("valid@example.test", "short")]
    public async Task SignUp_RejectsInvalidInputBeforeCallingTheService(string email, string password)
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        await using var app = await CreateAppAsync(auth.Object);
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/auth/sign-up", new SignUpRequest(email, password, "invalid-user"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        auth.VerifyNoOtherCalls();
    }

    private static async Task<WebApplication> CreateAppAsync(IAuthService? auth = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration["Jwt:SecretKey"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var databaseName = Guid.NewGuid().ToString("N");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddSingleton(auth ?? Mock.Of<IAuthService>());
        var app = builder.Build();
        app.MapAuthEndpoints();
        await app.StartAsync();
        return app;
    }
}
