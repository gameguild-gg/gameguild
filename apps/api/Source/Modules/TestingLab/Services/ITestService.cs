using GameGuild.Modules.TestingLab.Models;


namespace GameGuild.Modules.TestingLab.Services;

/// <summary>
/// Service interface for Testing operations
/// Comprehensive testing session and request management capabilities
/// </summary>
public interface ITestService {
  #region Testing Request Operations

  /// <summary>
  /// Get all testing requests (non-deleted only)
  /// </summary>
  Task<IEnumerable<TestingRequest>> GetAllTestingRequestsAsync();

  /// <summary>
  /// Get testing requests with pagination
  /// </summary>
  Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50);

  /// <summary>
  /// Get a testing request by ID
  /// </summary>
  Task<TestingRequest?> GetTestingRequestByIdAsync(Guid id);

  /// <summary>
  /// Get a testing request by ID with all related details
  /// </summary>
  Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id);

  /// <summary>
  /// Create a new testing request
  /// </summary>
  Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest);

  /// <summary>
  /// Update an existing testing request
  /// </summary>
  Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest);

  /// <summary>
  /// Soft delete a testing request
  /// </summary>
  Task<bool> DeleteTestingRequestAsync(Guid id);

  /// <summary>
  /// Restore a soft-deleted testing request
  /// </summary>
  Task<bool> RestoreTestingRequestAsync(Guid id);

  #endregion

  #region Testing Session Operations

  /// <summary>
  /// Get all testing sessions (non-deleted only)
  /// </summary>
  Task<IEnumerable<TestingSession>> GetAllTestingSessionsAsync();

  /// <summary>
  /// Get testing sessions with pagination
  /// </summary>
  Task<IEnumerable<TestingSession>> GetTestingSessionsAsync(int skip = 0, int take = 50);

  /// <summary>
  /// Get a testing session by ID
  /// </summary>
  Task<TestingSession?> GetTestingSessionByIdAsync(Guid id);

  /// <summary>
  /// Get a testing session by ID with all related details
  /// </summary>
  Task<TestingSession?> GetTestingSessionByIdWithDetailsAsync(Guid id);

  /// <summary>
  /// Create a new testing session
  /// </summary>
  Task<TestingSession> CreateTestingSessionAsync(TestingSession testingSession);

  /// <summary>
  /// Update an existing testing session
  /// </summary>
  Task<TestingSession> UpdateTestingSessionAsync(TestingSession testingSession);

  /// <summary>
  /// Soft delete a testing session
  /// </summary>
  Task<bool> DeleteTestingSessionAsync(Guid id);

  /// <summary>
  /// Restore a soft-deleted testing session
  /// </summary>
  Task<bool> RestoreTestingSessionAsync(Guid id);

  #endregion

  #region Filtered Queries

  /// <summary>
  /// Get testing requests by project version
  /// </summary>
  Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId);

  /// <summary>
  /// Get testing requests by creator
  /// </summary>
  Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId);

  /// <summary>
  /// Get testing requests by status
  /// </summary>
  Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status);

  /// <summary>
  /// Get testing sessions by request
  /// </summary>
  Task<IEnumerable<TestingSession>> GetTestingSessionsByRequestAsync(Guid testingRequestId);

  /// <summary>
  /// Get testing sessions by location
  /// </summary>
  Task<IEnumerable<TestingSession>> GetTestingSessionsByLocationAsync(Guid locationId);

  /// <summary>
  /// Get testing sessions by status
  /// </summary>
  Task<IEnumerable<TestingSession>> GetTestingSessionsByStatusAsync(SessionStatus status);

  /// <summary>
  /// Get testing sessions by manager
  /// </summary>
  Task<IEnumerable<TestingSession>> GetTestingSessionsByManagerAsync(Guid managerId);

  /// <summary>
  /// Search testing requests by title and description
  /// </summary>
  Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm);

  /// <summary>
  /// Search testing sessions by name
  /// </summary>
  Task<IEnumerable<TestingSession>> SearchTestingSessionsAsync(string searchTerm);

  #endregion

  #region Participant Management

  /// <summary>
  /// Add a participant to a testing request
  /// </summary>
  Task<TestingParticipant> AddParticipantAsync(Guid testingRequestId, Guid userId);

  /// <summary>
  /// Remove a participant from a testing request
  /// </summary>
  Task<bool> RemoveParticipantAsync(Guid testingRequestId, Guid userId);

  /// <summary>
  /// Get participants for a testing request
  /// </summary>
  Task<IEnumerable<TestingParticipant>> GetTestingRequestParticipantsAsync(Guid testingRequestId);

  /// <summary>
  /// Check if user is participant in testing request
  /// </summary>
  Task<bool> IsUserParticipantAsync(Guid testingRequestId, Guid userId);

  #endregion

  #region Session Registration Management

  /// <summary>
  /// Register user for a testing session
  /// </summary>
  Task<SessionRegistration> RegisterForSessionAsync(
    Guid sessionId, Guid userId, RegistrationType registrationType,
    string? notes = null
  );

  /// <summary>
  /// Unregister user from a testing session
  /// </summary>
  Task<bool> UnregisterFromSessionAsync(Guid sessionId, Guid userId);

  /// <summary>
  /// Get registrations for a testing session
  /// </summary>
  Task<IEnumerable<SessionRegistration>> GetSessionRegistrationsAsync(Guid sessionId);

  /// <summary>
  /// Add user to session waitlist
  /// </summary>
  Task<SessionWaitlist> AddToWaitlistAsync(
    Guid sessionId, Guid userId, RegistrationType registrationType,
    string? notes = null
  );

  /// <summary>
  /// Remove user from session waitlist
  /// </summary>
  Task<bool> RemoveFromWaitlistAsync(Guid sessionId, Guid userId);

  /// <summary>
  /// Get waitlist for a testing session
  /// </summary>
  Task<IEnumerable<SessionWaitlist>> GetSessionWaitlistAsync(Guid sessionId);

  #endregion

  #region Feedback Management

  /// <summary>
  /// Add feedback for a testing request
  /// </summary>
  Task<TestingFeedback> AddFeedbackAsync(
    Guid testingRequestId, Guid userId, Guid feedbackFormId, string feedbackData,
    TestingContext context, Guid? sessionId = null, string? additionalNotes = null
  );

  /// <summary>
  /// Get feedback for a testing request
  /// </summary>
  Task<IEnumerable<TestingFeedback>> GetTestingRequestFeedbackAsync(Guid testingRequestId);

  /// <summary>
  /// Get feedback by user
  /// </summary>
  Task<IEnumerable<TestingFeedback>> GetFeedbackByUserAsync(Guid userId);

  #endregion

  #region Statistics and Analytics

  /// <summary>
  /// Get testing request statistics
  /// </summary>
  Task<object> GetTestingRequestStatisticsAsync(Guid testingRequestId);

  /// <summary>
  /// Get testing session statistics
  /// </summary>
  Task<object> GetTestingSessionStatisticsAsync(Guid testingSessionId);

  /// <summary>
  /// Get user testing activity
  /// </summary>
  Task<object> GetUserTestingActivityAsync(Guid userId);

  #endregion
}
