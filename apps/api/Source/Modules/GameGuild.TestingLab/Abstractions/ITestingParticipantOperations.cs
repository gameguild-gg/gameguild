namespace GameGuild.TestingLab;

/// <summary>
/// Service interface for participant management, session registration, and waitlist operations.
/// Extracted from ITestService for focused responsibility.
/// </summary>
public interface ITestingParticipantOperations
{
    // Participant management
    Task<TestingParticipant> AddParticipantAsync(Guid testingRequestId, Guid userId);
    Task<bool> RemoveParticipantAsync(Guid testingRequestId, Guid userId);
    Task<IEnumerable<TestingParticipant>> GetTestingRequestParticipantsAsync(Guid testingRequestId);
    Task<bool> IsUserParticipantAsync(Guid testingRequestId, Guid userId);

    // Session registration
    Task<SessionRegistration> RegisterForSessionAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null);
    Task<bool> UnregisterFromSessionAsync(Guid sessionId, Guid userId);
    Task<IEnumerable<SessionRegistration>> GetSessionRegistrationsAsync(Guid sessionId);

    // Waitlist
    Task<SessionWaitlist> AddToWaitlistAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null);
    Task<bool> RemoveFromWaitlistAsync(Guid sessionId, Guid userId);
    Task<IEnumerable<SessionWaitlist>> GetSessionWaitlistAsync(Guid sessionId);

    // User activity
    Task<object> GetUserTestingActivityAsync(Guid userId);

    // Attendance reports
    Task<object> GetStudentAttendanceReportAsync();
}
