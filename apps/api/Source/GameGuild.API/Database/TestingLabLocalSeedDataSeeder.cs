using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.Database;

/// <summary>
/// Adds persisted game and approved application records to the local Game Jam Sprint playtest.
/// This seed is invoked only by the API composition in Development environments.
/// </summary>
public static class TestingLabLocalSeedDataSeeder
{
    public static readonly Guid GameJamSprintEventId = Guid.Parse("490dc4af-4480-41cb-89a1-41dd5e555d62");

    private static readonly SeedGame[] Games =
    [
        new(
            Guid.Parse("27c87b9a-eebf-4a13-85c1-000000000001"),
            "Mothlight",
            "A tiny forest spirit restores light to a moonlit valley.",
            "Explore a hand-painted forest, help lost creatures, and bring color back to a quiet valley.",
            "/testing-lab/seeded-games/mothlight.svg"),
        new(
            Guid.Parse("27c87b9a-eebf-4a13-85c1-000000000002"),
            "Hollow Signal",
            "Trace a broken radio signal through an abandoned station.",
            "Tune a field receiver, follow clues through a remote outpost, and uncover the source of a strange transmission.",
            "/testing-lab/seeded-games/hollow-signal.svg"),
        new(
            Guid.Parse("27c87b9a-eebf-4a13-85c1-000000000003"),
            "Tidebreak",
            "Race the tide in a fast, wave-powered island run.",
            "Chain turns and jumps across a coastal course before the next wave reshapes the track.",
            "/testing-lab/seeded-games/tidebreak.svg"),
    ];

    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger? logger = null,
        CancellationToken cancellationToken = default,
        Guid? eventId = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var targetEventId = eventId ?? GameJamSprintEventId;
        var testingEvent = await context.Set<TestingEvent>()
            .FirstOrDefaultAsync(candidate => candidate.Id == targetEventId, cancellationToken)
            .ConfigureAwait(false);

        if (testingEvent is null)
        {
            logger?.LogInformation(
                "Skipping local Testing Lab game seed because event {EventId} is not present.",
                targetEventId);
            return;
        }

        var slot = await context.Set<TestingEventSlot>()
            .Where(candidate => candidate.EventId == testingEvent.Id)
            .OrderBy(candidate => candidate.StartsAt)
            .FirstOrDefaultAsync(
                candidate => candidate.MaxProjects == null || candidate.MaxProjects >= Games.Length,
                cancellationToken)
            .ConfigureAwait(false);

        if (slot is null)
        {
            slot = TestingEventSlot.Create(
                testingEvent.Id,
                testingEvent.Mode,
                testingEvent.StartsAt,
                testingEvent.EndsAt,
                maxTesters: 30,
                maxProjects: Games.Length,
                campusName: testingEvent.Mode == TestingEventMode.InPerson ? "Local Testing Lab" : null,
                roomName: testingEvent.Mode == TestingEventMode.InPerson ? "Game Jam room" : null,
                meetingUrl: testingEvent.Mode == TestingEventMode.Online
                    ? $"http://localhost:3000/testing-lab/events/{testingEvent.Id}"
                    : null,
                tenantId: testingEvent.TenantId);
            context.Set<TestingEventSlot>().Add(slot);
        }

        var seededProjects = 0;
        var seededVersions = 0;
        var seededApplications = 0;
        foreach (var game in Games)
        {
            var project = await context.Set<Project>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(candidate => candidate.Id == game.Id, cancellationToken)
                .ConfigureAwait(false);

            if (project is null)
            {
                project = new Project
                {
                    Id = game.Id,
                    TenantId = testingEvent.TenantId,
                    Title = game.Title,
                    Slug = $"local-testing-lab-{game.Title.ToLowerInvariant().Replace(' ', '-')}",
                    ShortDescription = game.ShortDescription,
                    Description = game.Description,
                    Type = global::GameGuild.Projects.ProjectType.Game,
                    DevelopmentStatus = global::GameGuild.Projects.DevelopmentStatus.Beta,
                    Status = global::GameGuild.ContentStatus.Published,
                    Visibility = global::GameGuild.ContentVisibility.Public,
                    ImageUrl = game.ImageUrl,
                    FeaturedImageUrl = game.ImageUrl,
                    CreatedById = testingEvent.ManagerUserId,
                    PublishedAt = SystemClock.UtcNow,
                };
                context.Set<Project>().Add(project);
                seededProjects++;
            }
            else if (project.DeletedAt is not null)
            {
                logger?.LogWarning(
                    "Skipping local Testing Lab game {ProjectId} because its project is soft-deleted.",
                    game.Id);
                continue;
            }

            var projectVersion = await context.Set<ProjectVersion>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    candidate => candidate.ProjectId == game.Id && candidate.VersionNumber == "0.1.0-local" && candidate.DeletedAt == null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (projectVersion is null)
            {
                projectVersion = ProjectVersion.Create(
                    game.Id,
                    "0.1.0-local",
                    "Local playable build for Testing Lab enrollment review.",
                    testingEvent.ManagerUserId,
                    testingEvent.TenantId);
                projectVersion.MarkReadyForTesting();
                context.Set<ProjectVersion>().Add(projectVersion);
                seededVersions++;
            }

            var applicationExists = await context.Set<TestingProjectApplication>()
                .IgnoreQueryFilters()
                .AnyAsync(
                    application => application.EventId == testingEvent.Id && application.ProjectId == game.Id,
                    cancellationToken)
                .ConfigureAwait(false);
            if (applicationExists) continue;

            var application = TestingProjectApplication.Submit(
                testingEvent.Id,
                game.Id,
                projectVersionId: null,
                submittedByUserId: testingEvent.ManagerUserId,
                preferredAvailability: "Available for the local playtest window",
                tenantId: testingEvent.TenantId);
            application.Approve(
                testingEvent.ManagerUserId,
                slot.Id,
                "Approved local seed game for Testing Lab layout review.");
            context.Set<TestingProjectApplication>().Add(application);
            seededApplications++;
        }

        // Build-only repairs must commit before the next event's seed queries the database.
        if (seededProjects > 0 || seededVersions > 0 || seededApplications > 0 || context.Entry(slot).State == EntityState.Added)
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger?.LogInformation(
            "Seeded {ProjectCount} local Testing Lab games, {VersionCount} builds, and {ApplicationCount} approved applications for event {EventId}.",
            seededProjects,
            seededVersions,
            seededApplications,
            testingEvent.Id);
    }

    private sealed record SeedGame(Guid Id, string Title, string ShortDescription, string Description, string ImageUrl);
}
