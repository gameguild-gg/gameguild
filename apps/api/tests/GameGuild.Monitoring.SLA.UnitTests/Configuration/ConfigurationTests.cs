using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void SlaModelConfiguration_ShouldConfigureAllEntities()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new SlaModelConfiguration().Configure(modelBuilder);

        modelBuilder.Model.FindEntityType(typeof(ServiceLevelIndicator)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(ServiceLevelObjective)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(SloViolation)).Should().NotBeNull();
    }

    [Fact]
    public void ServiceLevelIndicatorConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new ServiceLevelIndicatorConfiguration().Configure(modelBuilder.Entity<ServiceLevelIndicator>());

        var entityType = modelBuilder.Model.FindEntityType(typeof(ServiceLevelIndicator));
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("service_level_indicators");
        entityType!.GetSchema().Should().Be("gameguild.sla");
    }

    [Fact]
    public void ServiceLevelObjectiveConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new ServiceLevelObjectiveConfiguration().Configure(modelBuilder.Entity<ServiceLevelObjective>());

        var entityType = modelBuilder.Model.FindEntityType(typeof(ServiceLevelObjective));
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("service_level_objectives");
        entityType!.GetSchema().Should().Be("gameguild.sla");
    }

    [Fact]
    public void SloViolationConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new SloViolationConfiguration().Configure(modelBuilder.Entity<SloViolation>());

        var entityType = modelBuilder.Model.FindEntityType(typeof(SloViolation));
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("slo_violations");
        entityType!.GetSchema().Should().Be("gameguild.sla");
    }
}
