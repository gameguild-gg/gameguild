using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTenantsModule_Should_Register_Services()
    {
        var services = new ServiceCollection();

        var result = services.AddTenantsModule();

        result.Should().BeSameAs(services);
        services.Should().Contain(s => s.ServiceType == typeof(ITenantRepository));
        services.Should().Contain(s => s.ServiceType == typeof(ITenantDomainsRepository));
        services.Should().Contain(s => s.ServiceType == typeof(ITenantMemberRepository));
        services.Should().Contain(s => s.ServiceType == typeof(ITenantSettingsRepository));
        services.Should().Contain(s => s.ServiceType == typeof(ITenantService));
        services.Should().Contain(s => s.ServiceType == typeof(IUsageTrackingService));
    }
}
