using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Configuration;

public class EntityConfigurationTests
{
    [Fact]
    public void TenantConfiguration_Should_Throw_On_Null_Builder()
    {
        var config = new TenantConfiguration();
        var act = () => config.Configure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TenantDomainConfiguration_Should_Throw_On_Null_Builder()
    {
        var config = new TenantDomainConfiguration();
        var act = () => config.Configure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TenantMemberConfiguration_Should_Throw_On_Null_Builder()
    {
        var config = new TenantMemberConfiguration();
        var act = () => config.Configure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TenantSettingsConfiguration_Should_Throw_On_Null_Builder()
    {
        var config = new TenantSettingsConfiguration();
        var act = () => config.Configure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TenantStatisticsConfiguration_Should_Throw_On_Null_Builder()
    {
        var config = new TenantStatisticsConfiguration();
        var act = () => config.Configure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UsageTrackingConfiguration_Should_Throw_On_Null_Builder()
    {
        var config = new UsageTrackingConfiguration();
        var act = () => config.Configure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configure_All_EntityConfigurations_Should_Build_Model()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
        new TenantDomainConfiguration().Configure(modelBuilder.Entity<TenantDomain>());
        new TenantMemberConfiguration().Configure(modelBuilder.Entity<TenantMember>());
        new TenantSettingsConfiguration().Configure(modelBuilder.Entity<TenantSettings>());
        new TenantStatisticsConfiguration().Configure(modelBuilder.Entity<TenantStatistics>());
        new UsageTrackingConfiguration().Configure(modelBuilder.Entity<UsageTracking>());
        new TenantMetadataConfiguration().Configure(modelBuilder.Entity<TenantMetadata>());
        new TenantAuditLogConfiguration().Configure(modelBuilder.Entity<TenantAuditLog>());

        modelBuilder.Model.GetEntityTypes().Should().NotBeEmpty();
    }

    [Fact]
    public void TenantsModelConfiguration_Should_Apply_Module_Entities()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new TenantsModelConfiguration().Configure(modelBuilder);

        modelBuilder.Model.FindEntityType(typeof(Tenant)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(TenantDomain)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(TenantMember)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(TenantMetadata)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(TenantSettings)).Should().NotBeNull();
    }
}
