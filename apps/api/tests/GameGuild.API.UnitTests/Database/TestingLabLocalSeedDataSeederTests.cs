using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.UnitTests.Database;

public sealed class TestingLabLocalSeedDataSeederTests
{
    [Fact]
    public async Task SeedAsync_PersistsApprovedGamesForTheEvent_AndIsIdempotent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var managerId = Guid.NewGuid();
        var startsAt = new DateTime(2026, 9, 22, 2, 43, 0, DateTimeKind.Utc);
        var eventEntity = TestingEvent.Create(
            "Game Jam Sprint Playtest",
            TestingEventMode.Online,
            managerId,
            startsAt.AddDays(-2),
            startsAt.AddDays(-1),
            startsAt,
            startsAt.AddHours(3),
            requiresFeedback: true,
            approvalMode: TestingEventApprovalMode.ManagerOnly,
            tenantId: null,
            description: "Community playtest of the latest game jam builds.");
        context.Set<TestingEvent>().Add(eventEntity);
        await context.SaveChangesAsync();

        await TestingLabLocalSeedDataSeeder.SeedAsync(context, eventId: eventEntity.Id);
        await TestingLabLocalSeedDataSeeder.SeedAsync(context, eventId: eventEntity.Id);

        var projects = await context.Set<Project>().ToListAsync();
        projects.Should().HaveCount(3);
        projects.Should().OnlyContain(project =>
            project.Status == ContentStatus.Published &&
            project.Visibility == ContentVisibility.Public &&
            project.ImageUrl != null);

        var applications = await context.Set<TestingProjectApplication>().ToListAsync();
        applications.Should().HaveCount(3);
        applications.Should().OnlyContain(application =>
            application.EventId == eventEntity.Id &&
            application.Status == TestingApplicationStatus.Approved &&
            application.AssignedSlotId != null);

        (await context.Set<TestingEventSlot>().CountAsync(slot => slot.EventId == eventEntity.Id)).Should().Be(1);
    }
}
