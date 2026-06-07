using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Cohorts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public class CohortModuleAndContractTests
{
    [Fact]
    public void AddCohortsModule_ShouldRegisterCohortService()
    {
        var services = new ServiceCollection();

        var returned = services.AddCohortsModule();

        returned.Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICohortService) &&
            descriptor.ImplementationType == typeof(CohortService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void MapCohortsEndpoints_ShouldReturnSameEndpointBuilder()
    {
        var endpoints = new TestEndpointRouteBuilder();

        var returned = endpoints.MapCohortsEndpoints();

        returned.Should().BeSameAs(endpoints);
    }

    [Fact]
    public void CohortService_Constructor_ShouldCreateInstance()
    {
        var context = new Mock<IApplicationDbContext>();

        var service = new CohortService(context.Object, NullLogger<CohortService>.Instance);

        service.Should().BeAssignableTo<ICohortService>();
    }

    [Fact]
    public void CohortsController_Constructor_ShouldCreateInstance()
    {
        var service = new Mock<ICohortService>();
        var actorContextAccessor = new Mock<IActorContextAccessor>();

        var controller = new CohortsController(
            service.Object,
            actorContextAccessor.Object,
            NullLogger<CohortsController>.Instance);

        controller.Should().NotBeNull();
    }

    [Fact]
    public void CreateAndUpdateRequests_ShouldExposeAllInputs()
    {
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 1, 17, 0, 0, DateTimeKind.Utc);

        var create = new CreateCohortRequest(
            courseId,
            "Winter group",
            start,
            end,
            24,
            "Remote cohort",
            tenantId,
            instructorId,
            "Tuesdays");
        var update = new UpdateCohortRequest(
            "Updated group",
            "Updated description",
            start.AddDays(1),
            end.AddDays(1),
            30,
            instructorId,
            "Fridays");

        create.CourseId.Should().Be(courseId);
        create.Name.Should().Be("Winter group");
        create.StartDate.Should().Be(start);
        create.EndDate.Should().Be(end);
        create.MaxCapacity.Should().Be(24);
        create.Description.Should().Be("Remote cohort");
        create.TenantId.Should().Be(tenantId);
        create.InstructorId.Should().Be(instructorId);
        create.MeetingSchedule.Should().Be("Tuesdays");
        update.Name.Should().Be("Updated group");
        update.Description.Should().Be("Updated description");
        update.StartDate.Should().Be(start.AddDays(1));
        update.EndDate.Should().Be(end.AddDays(1));
        update.MaxCapacity.Should().Be(30);
        update.InstructorId.Should().Be(instructorId);
        update.MeetingSchedule.Should().Be("Fridays");
    }

    [Fact]
    public void CohortDto_FromEntity_ShouldMapAllFields()
    {
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var cohort = Cohort.Create(
            courseId,
            "Mapped cohort",
            new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 1, 17, 0, 0, DateTimeKind.Utc),
            2,
            tenantId,
            instructorId);
        cohort.SetDescription("Mapped description");
        cohort.SetMeetingSchedule("Daily");
        cohort.Open();
        cohort.IncrementEnrollment();

        var dto = CohortDto.FromEntity(cohort);

        dto.Id.Should().Be(cohort.Id);
        dto.CourseId.Should().Be(courseId);
        dto.TenantId.Should().Be(tenantId);
        dto.Name.Should().Be("Mapped cohort");
        dto.Description.Should().Be("Mapped description");
        dto.StartDate.Should().Be(cohort.StartDate);
        dto.EndDate.Should().Be(cohort.EndDate);
        dto.MaxCapacity.Should().Be(2);
        dto.CurrentEnrollmentCount.Should().Be(1);
        dto.AvailableSpots.Should().Be(1);
        dto.Status.Should().Be(CohortStatus.Active);
        dto.IsOpen.Should().BeTrue();
        dto.CanEnroll.Should().BeTrue();
        dto.InstructorId.Should().Be(instructorId);
        dto.MeetingSchedule.Should().Be("Daily");
        dto.CreatedAt.Should().Be(cohort.CreatedAt);
    }

    private sealed class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }
}
