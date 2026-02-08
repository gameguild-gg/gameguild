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
        return await context.TestingRequests
            .Where(tr => tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50)
    {
        return await context.TestingRequests
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
        return await context.TestingRequests
            .Where(tr => tr.Id == id && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .FirstOrDefaultAsync();
    }

    public async Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id)
    {
        return await context.TestingRequests
            .Where(tr => tr.Id == id && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .FirstOrDefaultAsync();
    }

    public async Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest)
    {
        testingRequest.Id = Guid.NewGuid();
        testingRequest.Touch();

        context.TestingRequests.Add(testingRequest);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return await GetTestingRequestByIdAsync(testingRequest.Id) ?? testingRequest.ConfigureAwait(false);
    }

    public async Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest)
    {
        var existingRequest = await context.TestingRequests.FindAsync(testingRequest.Id).ConfigureAwait(false);

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

        return await GetTestingRequestByIdAsync(existingRequest.Id) ?? existingRequest.ConfigureAwait(false);
    }

    public async Task<bool> DeleteTestingRequestAsync(Guid id)
    {
        var testingRequest = await context.TestingRequests.FindAsync(id).ConfigureAwait(false);

        if (testingRequest == null) return false;

        testingRequest.SoftDelete();
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<bool> RestoreTestingRequestAsync(Guid id)
    {
        var testingRequest = await context.TestingRequests.IgnoreQueryFilters().FirstOrDefaultAsync(tr => tr.Id == id);

        if (testingRequest == null) return false;

        testingRequest.Restore();
        testingRequest.Touch();
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId)
    {
        return await context.TestingRequests
            .Where(tr => tr.ProjectVersionId == projectVersionId && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId)
    {
        return await context.TestingRequests
            .Where(tr => tr.CreatedById == creatorId && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status)
    {
        return await context.TestingRequests
            .Where(tr => tr.Status == status && tr.DeletedAt == null)
            .Include(tr => tr.ProjectVersion)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await context.TestingRequests
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
        return await context.TestingRequests
            .Where(tr => tr.DeletedAt == null && tr.Status == TestingRequestStatus.Open)
            .Include(tr => tr.ProjectVersion)
            .ThenInclude(pv => pv.Project)
            .Include(tr => tr.CreatedBy)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();
    }

    public async Task<TestingRequest> CreateSimpleTestingRequestAsync(CreateSimpleTestingRequestDto requestDto, Guid userId)
    {
        var existingProject = await context.Projects.FirstOrDefaultAsync(p => p.Title == requestDto.TeamIdentifier && p.DeletedAt == null);

        Guid projectId;

        if (existingProject == null)
        {
            var newProject = new ProjectEntity
            {
                Id = Guid.NewGuid(),
                Title = requestDto.TeamIdentifier,
                ShortDescription = $"Capstone project for {requestDto.TeamIdentifier}",
                Description = $"Capstone project repository for team {requestDto.TeamIdentifier}",
                Status = ContentStatus.Published,
                Visibility = ContentVisibility.Public,
                DevelopmentStatus = DevelopmentStatus.InDevelopment,
                Type = ProjectType.Game,
                CreatedById = userId,
            };

            context.Projects.Add(newProject);
            await context.SaveChangesAsync().ConfigureAwait(false);
            projectId = newProject.Id;
        }
        else
        {
            projectId = existingProject.Id;
        }

        var projectRelease = new ProjectReleaseEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ReleaseVersion = requestDto.VersionNumber,
            ReleaseNotes = requestDto.Description ?? "",
            DownloadUrl = requestDto.DownloadUrl,
            IsPrerelease = true,
            ReleaseType = "testing",
            ReleasedAt = DateTime.UtcNow,
        };

        context.ProjectReleases.Add(projectRelease);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var testingRequest = new TestingRequest
        {
            Id = Guid.NewGuid(),
            ProjectVersionId = Guid.NewGuid(),
            Title = requestDto.Title,
            Description = requestDto.Description,
            DownloadUrl = requestDto.DownloadUrl,
            InstructionsType = requestDto.InstructionsType,
            InstructionsContent = requestDto.InstructionsContent,
            InstructionsUrl = requestDto.InstructionsUrl,
            FeedbackFormContent = requestDto.FeedbackFormContent,
            MaxTesters = requestDto.MaxTesters,
            StartDate = requestDto.StartDate ?? DateTime.UtcNow,
            EndDate = requestDto.EndDate ?? DateTime.UtcNow.AddDays(30),
            Status = TestingRequestStatus.Draft,
            CreatedById = userId,
        };

        context.TestingRequests.Add(testingRequest);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return await GetTestingRequestByIdAsync(testingRequest.Id) ?? testingRequest.ConfigureAwait(false);
    }
}
