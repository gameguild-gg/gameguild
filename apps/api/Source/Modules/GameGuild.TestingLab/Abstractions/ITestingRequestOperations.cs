namespace GameGuild.TestingLab;

/// <summary>
/// Service interface for testing request operations.
/// Extracted from ITestService for focused responsibility.
/// </summary>
public interface ITestingRequestOperations
{
    // CRUD
    Task<IEnumerable<TestingRequest>> GetAllTestingRequestsAsync();
    Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50, bool includeArchived = false);
    Task<TestingRequest?> GetTestingRequestByIdAsync(Guid id);
    Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id);
    Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest);
    Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest);
    Task<bool> DeleteTestingRequestAsync(Guid id);
    Task<bool> RestoreTestingRequestAsync(Guid id);

    // Filtered queries
    Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId);
    Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId);
    Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status);
    Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm);
    Task<IEnumerable<TestingRequest>> GetActiveTestingRequestsAsync();

    // Simplified workflow
    Task<TestingRequest> CreateSimpleTestingRequestAsync(CreateSimpleTestingRequestDto requestDto, Guid userId);
}
