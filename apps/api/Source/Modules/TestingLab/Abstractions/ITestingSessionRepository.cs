using GameGuild.Common;

namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Repository abstraction for Testing Session operations
/// </summary>
public interface ITestingSessionRepository : IRepository<TestingSession>
{
    /// <summary>
    /// Get testing sessions with pagination
    /// </summary>
    Task<IEnumerable<TestingSession>> GetWithPaginationAsync(int skip = 0, int take = 50);

    /// <summary>
    /// Get a testing session by ID with all related details
    /// </summary>
    Task<TestingSession?> GetByIdWithDetailsAsync(Guid id);

    /// <summary>
    /// Get testing sessions by testing request
    /// </summary>
    Task<IEnumerable<TestingSession>> GetByTestingRequestAsync(Guid testingRequestId);

    /// <summary>
    /// Get testing sessions by status
    /// </summary>
    Task<IEnumerable<TestingSession>> GetByStatusAsync(SessionStatus status);

    /// <summary>
    /// Get testing sessions by location
    /// </summary>
    Task<IEnumerable<TestingSession>> GetByLocationAsync(Guid locationId);

    /// <summary>
    /// Get upcoming testing sessions
    /// </summary>
    Task<IEnumerable<TestingSession>> GetUpcomingSessionsAsync();

    /// <summary>
    /// Get active testing sessions
    /// </summary>
    Task<IEnumerable<TestingSession>> GetActiveSessionsAsync();

    /// <summary>
    /// Get completed testing sessions
    /// </summary>
    Task<IEnumerable<TestingSession>> GetCompletedSessionsAsync();

    /// <summary>
    /// Get testing sessions by date range
    /// </summary>
    Task<IEnumerable<TestingSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get testing sessions for a participant
    /// </summary>
    Task<IEnumerable<TestingSession>> GetByParticipantAsync(Guid participantId);
}
