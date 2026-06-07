using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
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
    public async Task GetPolicyAsync_ReturnsStaticFallback_ForRegisteredEmployeePolicyMissingFromStore()
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
        policy!.Requirements.Should().Contain(requirement => requirement is TenantMatchRequirement);
        policy.Requirements.Should().Contain(requirement => requirement is AssertionRequirement);
        cache.Verify(c => c.Set(Policies.EmployeesCreate, tenantId.ToString(), 7, policy), Times.Once);
    }
}
