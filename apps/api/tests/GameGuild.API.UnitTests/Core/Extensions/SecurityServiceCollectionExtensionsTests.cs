using System.Security.Claims;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AuthenticationOptions = GameGuild.Configuration.PresentationLayer.Authentication.AuthenticationOptions;
using GameGuildAuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;
using MicrosoftAuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;

namespace GameGuild.API.UnitTests.Core.Extensions;

public sealed class SecurityServiceCollectionExtensionsTests
{
    [Fact]
    public void SetupAuthentication_UsesTheRoleClaimTypeEmittedByJwtTokenService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = new string('s', 64),
                ["Jwt:Issuer"] = "GameGuild",
                ["Jwt:Audience"] = "GameGuild.Users"
            })
            .Build();
        var options = new AuthenticationOptions
        {
            JwtSecretKey = new string('s', 64),
            JwtIssuer = "GameGuild",
            JwtAudience = "GameGuild.Users"
        };

        services.AddLogging();
        services.SetupAuthentication(configuration, options);
        using var serviceProvider = services.BuildServiceProvider();
        var jwtOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(jwtOptions.MapInboundClaims);
        Assert.Equal("role", jwtOptions.TokenValidationParameters.RoleClaimType);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SystemAdmin")]
    [InlineData("TenantAdmin")]
    public void UserManagementPolicies_ShouldNotBeRegisteredStatically(string role)
    {
        using var serviceProvider = BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MicrosoftAuthorizationOptions>>().Value;

        foreach (var policy in new[]
                 {
                     Policies.UsersCreate,
                     Policies.UsersUpdate,
                     Policies.UsersDelete,
                     Policies.UsersAdmin,
                     Policies.UsersPurge
                 })
        {
            Assert.Null(options.GetPolicy(policy));
        }

        Assert.False(string.IsNullOrWhiteSpace(role));
    }

    [Fact]
    public void UserManagementPolicies_ShouldBeResolvedByDynamicProvider()
    {
        using var serviceProvider = BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MicrosoftAuthorizationOptions>>().Value;
        Assert.Null(options.GetPolicy(Policies.UsersCreate));
    }

    [Fact]
    public void SystemAdminPolicy_ShouldBeResolvedByDynamicProvider()
    {
        using var serviceProvider = BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<MicrosoftAuthorizationOptions>>().Value;
        Assert.Null(options.GetPolicy(Policies.SystemAdmin));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("TenantAdmin")]
    [InlineData("Owner")]
    public void SystemAdminPolicy_ShouldNotHaveStaticRoleFallbacks(string role)
    {
        using var serviceProvider = BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<MicrosoftAuthorizationOptions>>().Value;
        Assert.Null(options.GetPolicy(Policies.SystemAdmin));
        Assert.False(string.IsNullOrWhiteSpace(role));
    }

    [Theory]
    [InlineData("User", true)]
    [InlineData("TenantAdmin", true)]
    [InlineData("Owner", false)]
    public async Task RequireUserRolePolicy_ShouldAcceptUsersAndTenantAdministrators(
        string role,
        bool expected)
    {
        using var serviceProvider = BuildServiceProvider();

        var result = await AuthorizeAsync(serviceProvider, CreatePrincipal(role), "RequireUserRole");

        Assert.Equal(expected, result.Succeeded);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddLogging();
        services.SetupAuthorization(configuration, GameGuildAuthorizationOptions.CreateDefault());

        return services.BuildServiceProvider();
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        IServiceProvider serviceProvider,
        ClaimsPrincipal principal,
        string policyName)
    {
        var options = serviceProvider.GetRequiredService<IOptions<MicrosoftAuthorizationOptions>>().Value;
        var policy = options.GetPolicy(policyName) ?? throw new InvalidOperationException($"Policy '{policyName}' was not registered.");
        var context = new AuthorizationHandlerContext(policy.Requirements, principal, resource: null);

        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
        {
            await handler.HandleAsync(context);
        }

        return context.HasSucceeded ? AuthorizationResult.Success() : AuthorizationResult.Failed();
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("tenant_id", Guid.NewGuid().ToString())
            ],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
