using GameGuild.Projects;
using ProjectEntity = GameGuild.Projects.Project;
using ProjectReleaseEntity = GameGuild.Projects.ProjectRelease;


namespace GameGuild.TestingLab;

/// <summary>
/// Composite service that implements ITestService by delegating to focused sub-services.
/// Kept for backward compatibility with GraphQL resolvers and other consumers.
/// New controllers should inject the specific sub-interface they need instead.
/// </summary>
public class TestService(
    ITestingRequestOperations requestOps,
    ITestingSessionOperations sessionOps,
    ITestingParticipantOperations participantOps,
    ITestingFeedbackOperations feedbackOps,
    ITestingLocationOperations locationOps) : ITestService
{
    // Testing Request Operations
    public Task<IEnumerable<TestingRequest>> GetAllTestingRequestsAsync() => requestOps.GetAllTestingRequestsAsync();
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50, bool includeArchived = false)
        => requestOps.GetTestingRequestsAsync(skip, take, includeArchived);
    public Task<TestingRequest?> GetTestingRequestByIdAsync(Guid id) => requestOps.GetTestingRequestByIdAsync(id);
    public Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id) => requestOps.GetTestingRequestByIdWithDetailsAsync(id);
    public Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest) => requestOps.CreateTestingRequestAsync(testingRequest);
    public Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest) => requestOps.UpdateTestingRequestAsync(testingRequest);
    public Task<bool> DeleteTestingRequestAsync(Guid id) => requestOps.DeleteTestingRequestAsync(id);
    public Task<bool> RestoreTestingRequestAsync(Guid id) => requestOps.RestoreTestingRequestAsync(id);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId) => requestOps.GetTestingRequestsByProjectVersionAsync(projectVersionId);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId) => requestOps.GetTestingRequestsByCreatorAsync(creatorId);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status) => requestOps.GetTestingRequestsByStatusAsync(status);
    public Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm) => requestOps.SearchTestingRequestsAsync(searchTerm);
    public Task<IEnumerable<TestingRequest>> GetActiveTestingRequestsAsync() => requestOps.GetActiveTestingRequestsAsync();
    public Task<TestingRequest> CreateSimpleTestingRequestAsync(CreateSimpleTestingRequestDto requestDto, Guid userId) => requestOps.CreateSimpleTestingRequestAsync(requestDto, userId);

    // Testing Session Operations
    public Task<IEnumerable<TestingSession>> GetAllTestingSessionsAsync() => sessionOps.GetAllTestingSessionsAsync();
    public Task<IEnumerable<TestingSession>> GetTestingSessionsAsync(int skip = 0, int take = 50) => sessionOps.GetTestingSessionsAsync(skip, take);
    public Task<TestingSession?> GetTestingSessionByIdAsync(Guid id) => sessionOps.GetTestingSessionByIdAsync(id);
    public Task<TestingSession?> GetTestingSessionByIdWithDetailsAsync(Guid id) => sessionOps.GetTestingSessionByIdWithDetailsAsync(id);
    public Task<TestingSession> CreateTestingSessionAsync(TestingSession testingSession) => sessionOps.CreateTestingSessionAsync(testingSession);
    public Task<TestingSession> UpdateTestingSessionAsync(TestingSession testingSession) => sessionOps.UpdateTestingSessionAsync(testingSession);
    public Task<bool> DeleteTestingSessionAsync(Guid id) => sessionOps.DeleteTestingSessionAsync(id);
    public Task<bool> RestoreTestingSessionAsync(Guid id) => sessionOps.RestoreTestingSessionAsync(id);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByRequestAsync(Guid testingRequestId) => sessionOps.GetTestingSessionsByRequestAsync(testingRequestId);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByLocationAsync(Guid locationId) => sessionOps.GetTestingSessionsByLocationAsync(locationId);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByStatusAsync(SessionStatus status) => sessionOps.GetTestingSessionsByStatusAsync(status);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByManagerAsync(Guid managerId) => sessionOps.GetTestingSessionsByManagerAsync(managerId);
    public Task<IEnumerable<TestingSession>> SearchTestingSessionsAsync(string searchTerm) => sessionOps.SearchTestingSessionsAsync(searchTerm);
    public Task<IEnumerable<TestingSession>> GetPublicTestingSessionsAsync(int take = 100) => sessionOps.GetPublicTestingSessionsAsync(take);
    public Task<object> GetTestingSessionStatisticsAsync(Guid testingSessionId) => sessionOps.GetTestingSessionStatisticsAsync(testingSessionId);
    public Task<object> GetSessionAttendanceReportAsync() => sessionOps.GetSessionAttendanceReportAsync();
    public Task UpdateSessionAttendanceAsync(Guid sessionId, Guid userId, AttendanceStatus status, Guid updatedByUserId) => sessionOps.UpdateSessionAttendanceAsync(sessionId, userId, status, updatedByUserId);

    // Participant Operations
    public Task<TestingParticipant> AddParticipantAsync(Guid testingRequestId, Guid userId) => participantOps.AddParticipantAsync(testingRequestId, userId);
    public Task<bool> RemoveParticipantAsync(Guid testingRequestId, Guid userId) => participantOps.RemoveParticipantAsync(testingRequestId, userId);
    public Task<IEnumerable<TestingParticipant>> GetTestingRequestParticipantsAsync(Guid testingRequestId) => participantOps.GetTestingRequestParticipantsAsync(testingRequestId);
    public Task<bool> IsUserParticipantAsync(Guid testingRequestId, Guid userId) => participantOps.IsUserParticipantAsync(testingRequestId, userId);
    public Task<SessionRegistration> RegisterForSessionAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null) => participantOps.RegisterForSessionAsync(sessionId, userId, registrationType, notes);
    public Task<bool> UnregisterFromSessionAsync(Guid sessionId, Guid userId) => participantOps.UnregisterFromSessionAsync(sessionId, userId);
    public Task<IEnumerable<SessionRegistration>> GetSessionRegistrationsAsync(Guid sessionId) => participantOps.GetSessionRegistrationsAsync(sessionId);
    public Task<SessionWaitlist> AddToWaitlistAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null) => participantOps.AddToWaitlistAsync(sessionId, userId, registrationType, notes);
    public Task<bool> RemoveFromWaitlistAsync(Guid sessionId, Guid userId) => participantOps.RemoveFromWaitlistAsync(sessionId, userId);
    public Task<IEnumerable<SessionWaitlist>> GetSessionWaitlistAsync(Guid sessionId) => participantOps.GetSessionWaitlistAsync(sessionId);
    public Task<object> GetUserTestingActivityAsync(Guid userId) => participantOps.GetUserTestingActivityAsync(userId);
    public Task<object> GetStudentAttendanceReportAsync() => participantOps.GetStudentAttendanceReportAsync();

    // Feedback Operations
    public Task<TestingFeedback> AddFeedbackAsync(Guid testingRequestId, Guid userId, Guid feedbackFormId, string feedbackData, TestingContext context, Guid? sessionId = null, string? additionalNotes = null) => feedbackOps.AddFeedbackAsync(testingRequestId, userId, feedbackFormId, feedbackData, context, sessionId, additionalNotes);
    public Task<IEnumerable<TestingFeedback>> GetTestingRequestFeedbackAsync(Guid testingRequestId) => feedbackOps.GetTestingRequestFeedbackAsync(testingRequestId);
    public Task<IEnumerable<TestingFeedback>> GetFeedbackByUserAsync(Guid userId) => feedbackOps.GetFeedbackByUserAsync(userId);
    public Task SubmitFeedbackAsync(SubmitFeedbackDto feedbackDto, Guid userId) => feedbackOps.SubmitFeedbackAsync(feedbackDto, userId);
    public Task<object> GetTestingRequestStatisticsAsync(Guid testingRequestId) => feedbackOps.GetTestingRequestStatisticsAsync(testingRequestId);
    public Task ReportFeedbackAsync(Guid feedbackId, string reason, Guid reportedByUserId) => feedbackOps.ReportFeedbackAsync(feedbackId, reason, reportedByUserId);
    public Task RateFeedbackQualityAsync(Guid feedbackId, FeedbackQuality quality, Guid ratedByUserId) => feedbackOps.RateFeedbackQualityAsync(feedbackId, quality, ratedByUserId);

    // Location Operations
    public Task<IEnumerable<TestingLocation>> GetAllTestingLocationsAsync() => locationOps.GetAllTestingLocationsAsync();
    public Task<IEnumerable<TestingLocation>> GetTestingLocationsAsync(int skip = 0, int take = 50, bool includeArchived = false) => locationOps.GetTestingLocationsAsync(skip, take, includeArchived);
    public Task<TestingLocation?> GetTestingLocationByIdAsync(Guid id) => locationOps.GetTestingLocationByIdAsync(id);
    public Task<TestingLocation> CreateTestingLocationAsync(TestingLocation location) => locationOps.CreateTestingLocationAsync(location);
    public Task<TestingLocation> UpdateTestingLocationAsync(TestingLocation location) => locationOps.UpdateTestingLocationAsync(location);
    public Task<bool> DeleteTestingLocationAsync(Guid id) => locationOps.DeleteTestingLocationAsync(id);
    public Task<bool> RestoreTestingLocationAsync(Guid id) => locationOps.RestoreTestingLocationAsync(id);
}

