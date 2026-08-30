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
    public async Task DynamicProvider_MissingTenantAdminPolicy_Denies()
    {
        var services = new ServiceCollection();
        var versionStore = new Mock<ITenantSecurityVersionStore>();
        versionStore.Setup(store => store.GetVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var policyStore = new Mock<IPolicyDefinitionStore>();
        policyStore.Setup(store => store.GetPolicyAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyDefinition?)null);
        services.AddSingleton(versionStore.Object);
        services.AddSingleton(policyStore.Object);
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
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "TenantAdmin")], "test"));
        var context = new AuthorizationHandlerContext(policy!.Requirements, principal, resource: null);
        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
            await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SystemAdmin")]
    [InlineData("TenantAdmin")]
    public void SetupAuthorization_DoesNotRegisterTenantAdminStatically(string role)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.SetupAuthorization(configuration, PresentationAuthorizationOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value;
        var policy = options.GetPolicy("TenantAdmin");

        role.Should().NotBeNullOrWhiteSpace();
        policy.Should().BeNull();
    }

    [Fact]
    public void SetupAuthorization_ProductOwnerCannotUseAStaticTenantAdminPolicy()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.SetupAuthorization(configuration, PresentationAuthorizationOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value;
        var policy = options.GetPolicy(Policies.TenantAdmin);

        policy.Should().BeNull();
    }
}
