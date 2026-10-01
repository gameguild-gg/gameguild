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

public sealed class AuthorizationOptionsConfigurationTests
{
    [Fact]
    public async Task SetupAuthorization_ConfiguresStaticPolicyClaimsSchemesAndInheritedRoles()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authorization:SystemAccountId"] = "11111111-2222-3333-4444-555555555555",
                ["Authorization:RoleHierarchy:Administrator:0"] = "Moderator",
                ["Authorization:RoleHierarchy:SystemAdministrator:0"] = "Administrator",
                ["Authorization:Policies:CanModerate:Roles:0"] = "Moderator",
                ["Authorization:Policies:CanModerate:Claims:0:Type"] = "tenant_access",
                ["Authorization:Policies:CanModerate:Claims:0:AllowedValues:0"] = "write",
                ["Authorization:Policies:CanModerate:AuthenticationSchemes:0"] = "Bearer"
            })
            .Build();
        var options = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptionsBuilder
            .Build(configuration);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPolicyCache>(Mock.Of<IPolicyCache>());
        services.AddSingleton<IPolicyMerger>(Mock.Of<IPolicyMerger>());

        services.SetupAuthorization(configuration, options);

        using var providerRoot = services.BuildServiceProvider();
        providerRoot.GetRequiredService<IOptions<GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions>>()
            .Value.SystemAccountId.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var runtimeOptions = providerRoot.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value;
        var policyProvider = providerRoot.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("CanModerate");

        policy.Should().NotBeNull();
        policy!.AuthenticationSchemes.Should().Contain("Bearer");
        policy.Requirements.OfType<RolesAuthorizationRequirement>().Single().AllowedRoles.Should()
            .BeEquivalentTo("Moderator", "Administrator", "SystemAdministrator");
        var claimRequirement = policy.Requirements.OfType<ClaimsAuthorizationRequirement>().Single();
        claimRequirement.ClaimType.Should().Be("tenant_access");
        claimRequirement.AllowedValues.Should().ContainSingle().Which.Should().Be("write");

        var inheritedRoleUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "SystemAdministrator"), new Claim("tenant_access", "write")], "test"));
        var deniedUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Moderator"), new Claim("tenant_access", "read")], "test"));
        var allowedContext = new AuthorizationHandlerContext(policy.Requirements, inheritedRoleUser, resource: null);
        var deniedContext = new AuthorizationHandlerContext(policy.Requirements, deniedUser, resource: null);

        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
        {
            await handler.HandleAsync(allowedContext);
            await handler.HandleAsync(deniedContext);
        }

        allowedContext.HasSucceeded.Should().BeTrue();
        deniedContext.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public void SetupAuthorization_RejectsStaticOverridesOfDatabasePolicies()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authorization:Policies:TenantAdmin:Roles:0"] = "TenantAdmin"
            })
            .Build();
        var options = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptionsBuilder
            .Build(configuration);
        var services = new ServiceCollection();

        services.SetupAuthorization(configuration, options);

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value;

        act.Should().Throw<InvalidOperationException>().WithMessage("*database-backed*");
    }
}
