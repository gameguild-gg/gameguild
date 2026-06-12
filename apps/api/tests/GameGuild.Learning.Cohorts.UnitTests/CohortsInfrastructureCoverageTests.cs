using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public sealed class CohortsInfrastructureCoverageTests
{
    [Fact]
    public void CohortsModelConfiguration_AppliesCohortMapping()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Cohort));

        entity.Should().NotBeNull();
        var cohort = entity!;
        cohort.GetTableName().Should().Be("learning_cohorts");
        cohort.FindPrimaryKey()!.Properties.Single().Name.Should().Be(nameof(Cohort.Id));
        cohort.FindProperty(nameof(Cohort.Name))!.GetMaxLength().Should().Be(250);
        cohort.FindProperty(nameof(Cohort.Name))!.IsNullable.Should().BeFalse();
        cohort.FindProperty(nameof(Cohort.Description))!.GetMaxLength().Should().Be(2000);
        cohort.FindProperty(nameof(Cohort.MeetingSchedule))!.GetMaxLength().Should().Be(4000);
        cohort.FindProperty(nameof(Cohort.Status))!.GetMaxLength().Should().Be(40);
        var courseStatusOpenIndex = new[] { nameof(Cohort.CourseId), nameof(Cohort.Status), nameof(Cohort.IsOpen) };
        cohort.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Cohort.CourseId));
        cohort.GetIndexes().Should().Contain(index => index.Properties.Select(property => property.Name).SequenceEqual(courseStatusOpenIndex));
        cohort.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Cohort.InstructorId));
        cohort.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Cohort.TenantId));
    }

    [Fact]
    public void CohortsModule_RegistersServiceAndReturnsEndpointBuilder()
    {
        var services = new ServiceCollection();
        var endpoints = new Mock<IEndpointRouteBuilder>().Object;

        var configured = services.AddCohortsModule();
        var mapped = endpoints.MapCohortsEndpoints();

        configured.Should().BeSameAs(services);
        mapped.Should().BeSameAs(endpoints);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ICohortService) && descriptor.ImplementationType == typeof(CohortService));
    }

    private static CohortsConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CohortsConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CohortsConfigurationDbContext(options);
    }

    private sealed class CohortsConfigurationDbContext(DbContextOptions<CohortsConfigurationDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new CohortsModelConfiguration().Configure(modelBuilder);
        }
    }
}
