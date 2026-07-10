using System.Security.Claims;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using GameGuildAuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;
using MicrosoftAuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;

namespace GameGuild.API.UnitTests.Core.Extensions;

public sealed class SecurityServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("SystemAdmin")]
    [InlineData("TenantAdmin")]
    public async Task UserManagementPolicies_ShouldAuthorizeAdministrativeRoles(string role)
    {
        using var serviceProvider = BuildServiceProvider();
        var principal = CreatePrincipal(role);

        foreach (var policy in new[]
                 {
                     Policies.UsersCreate,
                     Policies.UsersUpdate,
                     Policies.UsersDelete,
                     Policies.UsersAdmin,
                     Policies.UsersPurge
                 })
        {
            var result = await AuthorizeAsync(serviceProvider, principal, policy);

            Assert.True(result.Succeeded, $"Role '{role}' should satisfy policy '{policy}'.");
        }
    }

    [Fact]
    public async Task UserManagementPolicies_ShouldRejectRegularMembers()
    {
        using var serviceProvider = BuildServiceProvider();
        var principal = CreatePrincipal("User");

        var result = await AuthorizeAsync(serviceProvider, principal, Policies.UsersCreate);

        Assert.False(result.Succeeded);
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
