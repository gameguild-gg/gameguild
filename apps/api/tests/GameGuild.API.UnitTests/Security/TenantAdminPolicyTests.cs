using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using GameGuild.API;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization;
using Moq;
using PresentationAuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;
using RuntimeAuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;
using Xunit;

namespace GameGuild.API.UnitTests.Security;

public sealed class TenantAdminPolicyTests
{
    [Fact]
    public async Task DynamicProvider_ReturnsCanonicalTenantAdminPolicy_BeforeDatabaseOverrides()
    {
        var services = new ServiceCollection();
        await using var providerRoot = services.BuildServiceProvider();
        var provider = new DbAuthorizationPolicyProvider(
            Options.Create(new RuntimeAuthorizationOptions()),
            Mock.Of<IPolicyCache>(),
            Mock.Of<IPolicyMerger>(),
            providerRoot.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenancyOptions()),
            NullLogger<DbAuthorizationPolicyProvider>.Instance);

        var policy = await provider.GetPolicyAsync(Policies.TenantAdmin);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().Contain(requirement => requirement is TenantMatchRequirement);
        policy.Requirements.Should().Contain(requirement => requirement is AssertionRequirement);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SystemAdmin")]
    [InlineData("TenantAdmin")]
    public async Task SetupAuthorization_AdministrativeRoleWithJwtTenantClaimHasTenantAdminAccess(string role)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.SetupAuthorization(configuration, PresentationAuthorizationOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value;
        var policy = options.GetPolicy("TenantAdmin");

        policy.Should().NotBeNull();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("tenant_id", Guid.NewGuid().ToString())
                ],
                "test"));
        var context = new AuthorizationHandlerContext(policy!.Requirements, principal, resource: null);

        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
        {
            await handler.HandleAsync(context);
        }

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SetupAuthorization_ProductOwnerWithJwtTenantClaimDoesNotHaveTenantAdminAccess()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.SetupAuthorization(configuration, PresentationAuthorizationOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value;
        var policy = options.GetPolicy(Policies.TenantAdmin);

        policy.Should().NotBeNull();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Owner"),
                    new Claim("tenant_id", Guid.NewGuid().ToString())
                ],
                "test"));
        var context = new AuthorizationHandlerContext(policy!.Requirements, principal, resource: null);

        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
        {
            await handler.HandleAsync(context);
        }

        context.HasSucceeded.Should().BeFalse();
    }
}
