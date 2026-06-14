using GameGuild.Projects;
using ProjectEntity = GameGuild.Projects.Project;
using ProjectReleaseEntity = GameGuild.Projects.ProjectRelease;

namespace GameGuild.TestingLab;

/// <summary>
/// Service implementation for testing request operations.
/// Extracted from the monolithic TestService for focused responsibility.
/// </summary>
public class TestingRequestOperationsService(IApplicationDbContext context) : ITestingRequestOperations
{
    public async Task<IEnumerable<TestingRequest>> GetAllTestingRequestsAsync()
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50)
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<TestingRequest?> GetTestingRequestByIdAsync(Guid id)
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.Id == id && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .FirstOrDefaultAsync();
    }

    public async Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id)
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.Id == id && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .FirstOrDefaultAsync();
    }

    public async Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest)
    {
        testingRequest.Id = Guid.NewGuid();
        testingRequest.Touch();

        context.Set<TestingRequest>().Add(testingRequest);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return (await GetTestingRequestByIdAsync(testingRequest.Id).ConfigureAwait(false)) ?? testingRequest;
    }

    public async Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest)
    {
        var existingRequest = await context.Set<TestingRequest>().FindAsync(testingRequest.Id).ConfigureAwait(false);

        if (existingRequest == null)
            throw new InvalidOperationException($"Testing request with ID {testingRequest.Id} not found.");

        existingRequest.Title = testingRequest.Title;
        existingRequest.Description = testingRequest.Description;
        existingRequest.InstructionsType = testingRequest.InstructionsType;
        existingRequest.InstructionsContent = testingRequest.InstructionsContent;
        existingRequest.InstructionsUrl = testingRequest.InstructionsUrl;
        existingRequest.InstructionsFileId = testingRequest.InstructionsFileId;
        existingRequest.MaxTesters = testingRequest.MaxTesters;
        existingRequest.StartDate = testingRequest.StartDate;
        existingRequest.EndDate = testingRequest.EndDate;
        existingRequest.Status = testingRequest.Status;
        existingRequest.Touch();

        await context.SaveChangesAsync().ConfigureAwait(false);

        return (await GetTestingRequestByIdAsync(existingRequest.Id).ConfigureAwait(false)) ?? existingRequest;
    }

    public async Task<bool> DeleteTestingRequestAsync(Guid id)
    {
        var testingRequest = await context.Set<TestingRequest>().FindAsync(id).ConfigureAwait(false);

        if (testingRequest == null) return false;

        testingRequest.SoftDelete();
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<bool> RestoreTestingRequestAsync(Guid id)
    {
        var testingRequest = await context.Set<TestingRequest>().IgnoreQueryFilters().FirstOrDefaultAsync(tr => tr.Id == id);

        if (testingRequest == null) return false;

        testingRequest.Restore();
        testingRequest.Touch();
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId)
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.ProjectVersionId == projectVersionId && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId)
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.CreatedById == creatorId && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status)
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.Status == status && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await context.Set<TestingRequest>()
            .Where(tr => tr.DeletedAt == null &&
                (tr.Title.ToLower().Contains(lowerSearchTerm) ||
                 tr.Description != null && tr.Description.ToLower().Contains(lowerSearchTerm)))
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetActiveTestingRequestsAsync()
    {
        return await context.Set<TestingRequest>()
            .Where(tr => tr.DeletedAt == null && tr.Status == TestingRequestStatus.Open)
            .Include(tr => tr.ProjectVersion)
            .ThenInclude(pv => pv!.Project)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<TestingRequest> CreateSimpleTestingRequestAsync(CreateSimpleTestingRequestDto requestDto, Guid userId)
    {
        var existingProject = await context.Set<GameGuild.Projects.Project>().FirstOrDefaultAsync(p => p.Title == requestDto.TeamIdentifier && p.DeletedAt == null);

        Guid projectId;

        if (existingProject == null)
        {
            var newProject = new ProjectEntity
            {
                Id = Guid.NewGuid(),
                Title = requestDto.TeamIdentifier,
                Slug = ProjectEntity.GenerateSlug(requestDto.TeamIdentifier),
                ShortDescription = $"Capstone project for {requestDto.TeamIdentifier}",
                Description = $"Capstone project repository for team {requestDto.TeamIdentifier}",
                Status = ContentStatus.Published,
                Visibility = ContentVisibility.Public,
                DevelopmentStatus = GameGuild.Projects.DevelopmentStatus.InDevelopment,
                Type = GameGuild.Projects.ProjectType.Game,
                CreatedById = userId,
            };

            context.Set<GameGuild.Projects.Project>().Add(newProject);
            await context.SaveChangesAsync().ConfigureAwait(false);
            projectId = newProject.Id;
        }
        else
        {
            projectId = existingProject.Id;
        }

        var projectVersion = await context.Set<GameGuild.Projects.ProjectVersion>()
            .FirstOrDefaultAsync(version =>
                version.ProjectId == projectId &&
                version.VersionNumber == requestDto.VersionNumber)
            .ConfigureAwait(false);

        if (projectVersion == null)
        {
            projectVersion = new GameGuild.Projects.ProjectVersion
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                VersionNumber = requestDto.VersionNumber,
                ReleaseNotes = requestDto.Description,
                Status = "testing",
                CreatedById = userId,
            };

            context.Set<GameGuild.Projects.ProjectVersion>().Add(projectVersion);
        }

        var projectRelease = await context.Set<GameGuild.Projects.ProjectRelease>()
            .FirstOrDefaultAsync(release =>
                release.ProjectId == projectId &&
                release.ReleaseVersion == requestDto.VersionNumber)
            .ConfigureAwait(false);

        if (projectRelease == null)
        {
            projectRelease = new ProjectReleaseEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = $"{requestDto.TeamIdentifier} {requestDto.VersionNumber}",
                ReleaseVersion = requestDto.VersionNumber,
                ReleaseNotes = requestDto.Description ?? "",
                DownloadUrl = requestDto.DownloadUrl,
                IsPrerelease = true,
                ReleaseType = "testing",
                ReleasedAt = SystemClock.UtcNow,
            };

            context.Set<GameGuild.Projects.ProjectRelease>().Add(projectRelease);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        var testingRequest = new TestingRequest
        {
            Id = Guid.NewGuid(),
            ProjectVersionId = projectVersion.Id,
            Title = requestDto.Title,
            Description = requestDto.Description,
            DownloadUrl = requestDto.DownloadUrl,
            InstructionsType = requestDto.InstructionsType,
            InstructionsContent = requestDto.InstructionsContent,
            InstructionsUrl = requestDto.InstructionsUrl,
            FeedbackFormContent = requestDto.FeedbackFormContent,
            MaxTesters = requestDto.MaxTesters,
            StartDate = requestDto.StartDate ?? SystemClock.UtcNow,
            EndDate = requestDto.EndDate ?? SystemClock.UtcNow.AddDays(30),
            Status = TestingRequestStatus.Draft,
            CreatedById = userId,
        };

        context.Set<TestingRequest>().Add(testingRequest);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return (await GetTestingRequestByIdAsync(testingRequest.Id).ConfigureAwait(false)) ?? testingRequest;
    }
}
