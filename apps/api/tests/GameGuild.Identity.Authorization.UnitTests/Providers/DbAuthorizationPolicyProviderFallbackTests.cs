using System.Security.Claims;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using MsAuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;

namespace GameGuild.Identity.Authorization.UnitTests.Providers;

public class DbAuthorizationPolicyProviderFallbackTests
{
    [Fact]
    public async Task GetPolicyAsync_DatabasePolicyTakesPrecedenceOverStaticPolicyWithSameName()
    {
        var tenantId = Guid.NewGuid();
        var services = new ServiceCollection();
        var tenantContext = new Mock<IAuthorizationTenantContext>();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        var versionStore = new Mock<ITenantSecurityVersionStore>();
        versionStore.Setup(store => store.GetVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var policyStore = new Mock<IPolicyDefinitionStore>();
        policyStore.Setup(store => store.GetPolicyAsync(Policies.TenantAdmin, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDefinition
            {
                PolicyName = Policies.TenantAdmin,
                UseRuleBasedEvaluation = true,
                Rules = [new PolicyRule { Type = RuleTypes.RequireMfa }]
            });
        services.AddSingleton(tenantContext.Object);
        services.AddSingleton(versionStore.Object);
        services.AddSingleton(policyStore.Object);
        await using var providerRoot = services.BuildServiceProvider();
        var staticOptions = new MsAuthorizationOptions();
        staticOptions.AddPolicy(Policies.TenantAdmin, policy => policy.RequireAssertion(_ => true));
        var provider = new DbAuthorizationPolicyProvider(
            Options.Create(staticOptions),
            Mock.Of<IPolicyCache>(),
            new DefaultPolicyMerger(),
            providerRoot.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenancyOptions()),
            NullLogger<DbAuthorizationPolicyProvider>.Instance);

        var policy = await provider.GetPolicyAsync(Policies.TenantAdmin);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle(requirement => requirement is RulesetRequirement);
        policy.Requirements.Should().NotContain(requirement => requirement is AssertionRequirement);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsDenyPolicy_ForRegisteredPolicyMissingFromStore()
    {
        var tenantId = Guid.NewGuid();
        var services = new ServiceCollection();

        var tenantContext = new Mock<IAuthorizationTenantContext>();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);

        var versionStore = new Mock<ITenantSecurityVersionStore>();
        versionStore.Setup(store => store.GetVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var policyStore = new Mock<IPolicyDefinitionStore>();
        policyStore.Setup(store => store.GetPolicyAsync(Policies.EmployeesCreate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyDefinition?)null);

        services.AddSingleton(tenantContext.Object);
        services.AddSingleton(versionStore.Object);
        services.AddSingleton(policyStore.Object);

        await using var providerRoot = services.BuildServiceProvider();
        var cache = new Mock<IPolicyCache>();
        cache.Setup(c => c.Get(Policies.EmployeesCreate, tenantId.ToString(), 7)).Returns((Microsoft.AspNetCore.Authorization.AuthorizationPolicy?)null);

        var provider = new DbAuthorizationPolicyProvider(
            Options.Create(new MsAuthorizationOptions()),
            cache.Object,
            Mock.Of<IPolicyMerger>(),
            providerRoot.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenancyOptions()),
            NullLogger<DbAuthorizationPolicyProvider>.Instance);

        var policy = await provider.GetPolicyAsync(Policies.EmployeesCreate);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().NotContain(requirement => requirement is TenantMatchRequirement);
        var assertion = policy.Requirements.OfType<AssertionRequirement>().Should().ContainSingle().Subject;
        (await EvaluateAsync(assertion, "Owner")).Should().BeFalse();
        (await EvaluateAsync(assertion, "Member")).Should().BeFalse();
        cache.Verify(c => c.Set(Policies.EmployeesCreate, tenantId.ToString(), 7, policy), Times.Once);
    }

    [Fact]
    public async Task GetPolicyAsync_SystemAdminMissingFromStore_ShouldDenyEveryRole()
    {
        var tenantId = Guid.NewGuid();
        var services = new ServiceCollection();
        var tenantContext = new Mock<IAuthorizationTenantContext>();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        var versionStore = new Mock<ITenantSecurityVersionStore>();
        versionStore.Setup(store => store.GetVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var policyStore = new Mock<IPolicyDefinitionStore>();
        policyStore.Setup(store => store.GetPolicyAsync(Policies.SystemAdmin, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyDefinition?)null);
        services.AddSingleton(tenantContext.Object);
        services.AddSingleton(versionStore.Object);
        services.AddSingleton(policyStore.Object);

        await using var providerRoot = services.BuildServiceProvider();
        var cache = new Mock<IPolicyCache>();
        cache.Setup(candidate => candidate.Get(Policies.SystemAdmin, tenantId.ToString(), 1))
            .Returns((AuthorizationPolicy?)null);
        var provider = new DbAuthorizationPolicyProvider(
            Options.Create(new MsAuthorizationOptions()),
            cache.Object,
            Mock.Of<IPolicyMerger>(),
            providerRoot.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenancyOptions()),
            NullLogger<DbAuthorizationPolicyProvider>.Instance);

        var policy = await provider.GetPolicyAsync(Policies.SystemAdmin);

        policy.Should().NotBeNull();
        var assertion = policy!.Requirements.OfType<AssertionRequirement>().Should().ContainSingle().Subject;
        (await EvaluateAsync(assertion, "SystemAdmin")).Should().BeFalse();
        (await EvaluateAsync(assertion, "Admin")).Should().BeFalse();
        (await EvaluateAsync(assertion, "TenantAdmin")).Should().BeFalse();
    }

    private static async Task<bool> EvaluateAsync(AssertionRequirement assertion, string role)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            "Test"));
        var context = new AuthorizationHandlerContext([assertion], principal, resource: null);
        await assertion.HandleAsync(context);
        return context.HasSucceeded;
    }
}
