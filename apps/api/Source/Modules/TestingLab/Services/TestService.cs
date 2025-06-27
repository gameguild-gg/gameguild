using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Modules.TestingLab.Models;


namespace GameGuild.Modules.TestingLab.Services;

/// <summary>
/// Service implementation for Testing operations
/// Comprehensive testing session and request management capabilities
/// </summary>
public class TestService(ApplicationDbContext context) : ITestService {
  #region Testing Request Operations

  public async Task<IEnumerable<TestingRequest>> GetAllTestingRequestsAsync() {
    return await context.TestingRequests.Where(tr => tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .OrderByDescending(tr => tr.CreatedAt)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50) {
    return await context.TestingRequests.Where(tr => tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .OrderByDescending(tr => tr.CreatedAt)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<TestingRequest?> GetTestingRequestByIdAsync(Guid id) {
    return await context.TestingRequests.Where(tr => tr.Id == id && tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .FirstOrDefaultAsync();
  }

  public async Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id) {
    return await context.TestingRequests.Where(tr => tr.Id == id && tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .FirstOrDefaultAsync();
  }

  public async Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest) {
    testingRequest.Id = Guid.NewGuid();
    testingRequest.CreatedAt = DateTime.UtcNow;
    testingRequest.UpdatedAt = DateTime.UtcNow;

    context.TestingRequests.Add(testingRequest);
    await context.SaveChangesAsync();

    return await GetTestingRequestByIdAsync(testingRequest.Id) ?? testingRequest;
  }

  public async Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest) {
    var existingRequest = await context.TestingRequests.FindAsync(testingRequest.Id);

    if (existingRequest == null) throw new InvalidOperationException($"Testing request with ID {testingRequest.Id} not found.");

    // Update properties
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
    existingRequest.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return await GetTestingRequestByIdAsync(existingRequest.Id) ?? existingRequest;
  }

  public async Task<bool> DeleteTestingRequestAsync(Guid id) {
    var testingRequest = await context.TestingRequests.FindAsync(id);

    if (testingRequest == null) return false;

    testingRequest.DeletedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreTestingRequestAsync(Guid id) {
    var testingRequest = await context.TestingRequests.IgnoreQueryFilters().FirstOrDefaultAsync(tr => tr.Id == id);

    if (testingRequest == null) return false;

    testingRequest.DeletedAt = null;
    testingRequest.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return true;
  }

  #endregion

  #region Testing Session Operations

  public async Task<IEnumerable<TestingSession>> GetAllTestingSessionsAsync() {
    return await context.TestingSessions.Where(ts => ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetTestingSessionsAsync(int skip = 0, int take = 50) {
    return await context.TestingSessions.Where(ts => ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<TestingSession?> GetTestingSessionByIdAsync(Guid id) {
    return await context.TestingSessions.Where(ts => ts.Id == id && ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .FirstOrDefaultAsync();
  }

  public async Task<TestingSession?> GetTestingSessionByIdWithDetailsAsync(Guid id) {
    return await context.TestingSessions.Where(ts => ts.Id == id && ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .FirstOrDefaultAsync();
  }

  public async Task<TestingSession> CreateTestingSessionAsync(TestingSession testingSession) {
    testingSession.Id = Guid.NewGuid();
    testingSession.CreatedAt = DateTime.UtcNow;
    testingSession.UpdatedAt = DateTime.UtcNow;

    context.TestingSessions.Add(testingSession);
    await context.SaveChangesAsync();

    return await GetTestingSessionByIdAsync(testingSession.Id) ?? testingSession;
  }

  public async Task<TestingSession> UpdateTestingSessionAsync(TestingSession testingSession) {
    var existingSession = await context.TestingSessions.FindAsync(testingSession.Id);

    if (existingSession == null) throw new InvalidOperationException($"Testing session with ID {testingSession.Id} not found.");

    // Update properties
    existingSession.SessionName = testingSession.SessionName;
    existingSession.SessionDate = testingSession.SessionDate;
    existingSession.StartTime = testingSession.StartTime;
    existingSession.EndTime = testingSession.EndTime;
    existingSession.MaxTesters = testingSession.MaxTesters;
    existingSession.Status = testingSession.Status;
    existingSession.ManagerUserId = testingSession.ManagerUserId;
    existingSession.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return await GetTestingSessionByIdAsync(existingSession.Id) ?? existingSession;
  }

  public async Task<bool> DeleteTestingSessionAsync(Guid id) {
    var testingSession = await context.TestingSessions.FindAsync(id);

    if (testingSession == null) return false;

    testingSession.DeletedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreTestingSessionAsync(Guid id) {
    var testingSession = await context.TestingSessions.IgnoreQueryFilters().FirstOrDefaultAsync(ts => ts.Id == id);

    if (testingSession == null) return false;

    testingSession.DeletedAt = null;
    testingSession.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return true;
  }

  #endregion

  #region Filtered Queries

  public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId) {
    return await context.TestingRequests.Where(tr => tr.ProjectVersionId == projectVersionId && tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .OrderByDescending(tr => tr.CreatedAt)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId) {
    return await context.TestingRequests.Where(tr => tr.CreatedById == creatorId && tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .OrderByDescending(tr => tr.CreatedAt)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status) {
    return await context.TestingRequests.Where(tr => tr.Status == status && tr.DeletedAt == null)
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .OrderByDescending(tr => tr.CreatedAt)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetTestingSessionsByRequestAsync(Guid testingRequestId) {
    return await context.TestingSessions.Where(ts => ts.TestingRequestId == testingRequestId && ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetTestingSessionsByLocationAsync(Guid locationId) {
    return await context.TestingSessions.Where(ts => ts.LocationId == locationId && ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetTestingSessionsByStatusAsync(SessionStatus status) {
    return await context.TestingSessions.Where(ts => ts.Status == status && ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetTestingSessionsByManagerAsync(Guid managerId) {
    return await context.TestingSessions.Where(ts => ts.ManagerUserId == managerId && ts.DeletedAt == null)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm) {
    var lowerSearchTerm = searchTerm.ToLower();

    return await context.TestingRequests.Where(tr =>
                                                  tr.DeletedAt == null &&
                                                  (tr.Title.ToLower().Contains(lowerSearchTerm) ||
                                                   (tr.Description != null && tr.Description.ToLower().Contains(lowerSearchTerm)))
                         )
                         .Include(tr => tr.ProjectVersion)
                         .Include(tr => tr.CreatedBy)
                         .OrderByDescending(tr => tr.CreatedAt)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> SearchTestingSessionsAsync(string searchTerm) {
    var lowerSearchTerm = searchTerm.ToLower();

    return await context.TestingSessions
                         .Where(ts => ts.DeletedAt == null && ts.SessionName.ToLower().Contains(lowerSearchTerm))
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderByDescending(ts => ts.SessionDate)
                         .ToListAsync();
  }

  #endregion

  #region Participant Management

  public async Task<TestingParticipant> AddParticipantAsync(Guid testingRequestId, Guid userId) {
    // Check if already a participant
    var existingParticipant =
      await context.TestingParticipants.FirstOrDefaultAsync(tp =>
                                                               tp.TestingRequestId == testingRequestId && tp.UserId == userId
      );

    if (existingParticipant != null) return existingParticipant;

    var participant = new TestingParticipant {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequestId,
      UserId = userId,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.TestingParticipants.Add(participant);
    await context.SaveChangesAsync();

    return await context.TestingParticipants.Include(tp => tp.TestingRequest)
                         .Include(tp => tp.User)
                         .FirstAsync(tp => tp.Id == participant.Id);
  }

  public async Task<bool> RemoveParticipantAsync(Guid testingRequestId, Guid userId) {
    var participant =
      await context.TestingParticipants.FirstOrDefaultAsync(tp =>
                                                               tp.TestingRequestId == testingRequestId && tp.UserId == userId
      );

    if (participant == null) return false;

    context.TestingParticipants.Remove(participant);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<TestingParticipant>> GetTestingRequestParticipantsAsync(Guid testingRequestId) {
    return await context.TestingParticipants.Where(tp => tp.TestingRequestId == testingRequestId)
                         .Include(tp => tp.User)
                         .OrderBy(tp => tp.CreatedAt)
                         .ToListAsync();
  }

  public async Task<bool> IsUserParticipantAsync(Guid testingRequestId, Guid userId) {
    return await context.TestingParticipants.AnyAsync(tp =>
                                                         tp.TestingRequestId == testingRequestId && tp.UserId == userId
           );
  }

  #endregion

  #region Session Registration Management

  public async Task<SessionRegistration> RegisterForSessionAsync(
    Guid sessionId, Guid userId,
    RegistrationType registrationType, string? notes = null
  ) {
    // Check if already registered
    var existingRegistration =
      await context.SessionRegistrations.FirstOrDefaultAsync(sr => sr.SessionId == sessionId && sr.UserId == userId);

    if (existingRegistration != null) return existingRegistration;

    var registration = new SessionRegistration {
      Id = Guid.NewGuid(),
      SessionId = sessionId,
      UserId = userId,
      RegistrationType = registrationType,
      RegistrationNotes = notes,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.SessionRegistrations.Add(registration);

    // Update session counts
    var session = await context.TestingSessions.FindAsync(sessionId);

    if (session != null) {
      if (registrationType == RegistrationType.Tester)
        session.RegisteredTesterCount++;
      else if (registrationType == RegistrationType.ProjectMember) session.RegisteredProjectMemberCount++;
    }

    await context.SaveChangesAsync();

    return await context.SessionRegistrations.Include(sr => sr.Session)
                         .Include(sr => sr.User)
                         .FirstAsync(sr => sr.Id == registration.Id);
  }

  public async Task<bool> UnregisterFromSessionAsync(Guid sessionId, Guid userId) {
    var registration =
      await context.SessionRegistrations.FirstOrDefaultAsync(sr => sr.SessionId == sessionId && sr.UserId == userId);

    if (registration == null) return false;

    // Update session counts
    var session = await context.TestingSessions.FindAsync(sessionId);

    if (session != null) {
      if (registration.RegistrationType == RegistrationType.Tester)
        session.RegisteredTesterCount = Math.Max(0, session.RegisteredTesterCount - 1);
      else if (registration.RegistrationType == RegistrationType.ProjectMember) session.RegisteredProjectMemberCount = Math.Max(0, session.RegisteredProjectMemberCount - 1);
    }

    context.SessionRegistrations.Remove(registration);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<SessionRegistration>> GetSessionRegistrationsAsync(Guid sessionId) {
    return await context.SessionRegistrations.Where(sr => sr.SessionId == sessionId)
                         .Include(sr => sr.User)
                         .OrderBy(sr => sr.CreatedAt)
                         .ToListAsync();
  }

  public async Task<SessionWaitlist> AddToWaitlistAsync(
    Guid sessionId, Guid userId, RegistrationType registrationType,
    string? notes = null
  ) {
    // Check if already on waitlist
    var existingWaitlist =
      await context.SessionWaitlists.FirstOrDefaultAsync(sw => sw.SessionId == sessionId && sw.UserId == userId);

    if (existingWaitlist != null) return existingWaitlist;

    // Get next position in waitlist
    var maxPosition = await context.SessionWaitlists.Where(sw => sw.SessionId == sessionId)
                                    .MaxAsync(sw => (int?)sw.Position) ??
                      0;

    var waitlistEntry = new SessionWaitlist {
      Id = Guid.NewGuid(),
      SessionId = sessionId,
      UserId = userId,
      RegistrationType = registrationType,
      Position = maxPosition + 1,
      RegistrationNotes = notes,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.SessionWaitlists.Add(waitlistEntry);
    await context.SaveChangesAsync();

    return await context.SessionWaitlists.Include(sw => sw.Session)
                         .Include(sw => sw.User)
                         .FirstAsync(sw => sw.Id == waitlistEntry.Id);
  }

  public async Task<bool> RemoveFromWaitlistAsync(Guid sessionId, Guid userId) {
    var waitlistEntry =
      await context.SessionWaitlists.FirstOrDefaultAsync(sw => sw.SessionId == sessionId && sw.UserId == userId);

    if (waitlistEntry == null) return false;

    var removedPosition = waitlistEntry.Position;

    context.SessionWaitlists.Remove(waitlistEntry);

    // Update positions for remaining waitlist entries
    var remainingEntries = await context.SessionWaitlists
                                         .Where(sw => sw.SessionId == sessionId && sw.Position > removedPosition)
                                         .ToListAsync();

    foreach (var entry in remainingEntries) entry.Position--;

    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<SessionWaitlist>> GetSessionWaitlistAsync(Guid sessionId) {
    return await context.SessionWaitlists.Where(sw => sw.SessionId == sessionId)
                         .Include(sw => sw.User)
                         .OrderBy(sw => sw.Position)
                         .ToListAsync();
  }

  #endregion

  #region Feedback Management

  public async Task<TestingFeedback> AddFeedbackAsync(
    Guid testingRequestId, Guid userId, Guid feedbackFormId,
    string feedbackData, TestingContext context1, Guid? sessionId = null, string? additionalNotes = null
  ) {
    var feedback = new TestingFeedback {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequestId,
      UserId = userId,
      FeedbackFormId = feedbackFormId,
      SessionId = sessionId,
      TestingContext = context1,
      FeedbackData = feedbackData,
      AdditionalNotes = additionalNotes,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    context.TestingFeedback.Add(feedback);
    await context.SaveChangesAsync();

    return await context.TestingFeedback.Include(tf => tf.TestingRequest)
                         .Include(tf => tf.User)
                         .Include(tf => tf.FeedbackForm)
                         .Include(tf => tf.Session)
                         .FirstAsync(tf => tf.Id == feedback.Id);
  }

  public async Task<IEnumerable<TestingFeedback>> GetTestingRequestFeedbackAsync(Guid testingRequestId) {
    return await context.TestingFeedback.Where(tf => tf.TestingRequestId == testingRequestId)
                         .Include(tf => tf.User)
                         .Include(tf => tf.FeedbackForm)
                         .Include(tf => tf.Session)
                         .OrderByDescending(tf => tf.CreatedAt)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingFeedback>> GetFeedbackByUserAsync(Guid userId) {
    return await context.TestingFeedback.Where(tf => tf.UserId == userId)
                         .Include(tf => tf.TestingRequest)
                         .Include(tf => tf.FeedbackForm)
                         .Include(tf => tf.Session)
                         .OrderByDescending(tf => tf.CreatedAt)
                         .ToListAsync();
  }

  #endregion

  #region Statistics and Analytics

  public async Task<object> GetTestingRequestStatisticsAsync(Guid testingRequestId) {
    var participantCount = await context.TestingParticipants.CountAsync(tp => tp.TestingRequestId == testingRequestId);

    var sessionCount =
      await context.TestingSessions.CountAsync(ts => ts.TestingRequestId == testingRequestId && ts.DeletedAt == null);

    var feedbackCount = await context.TestingFeedback.CountAsync(tf => tf.TestingRequestId == testingRequestId);

    var completedSessionCount = await context.TestingSessions.CountAsync(ts =>
                                                                            ts.TestingRequestId == testingRequestId && ts.Status == SessionStatus.Completed && ts.DeletedAt == null
                                );

    return new { ParticipantCount = participantCount, SessionCount = sessionCount, CompletedSessionCount = completedSessionCount, FeedbackCount = feedbackCount };
  }

  public async Task<object> GetTestingSessionStatisticsAsync(Guid testingSessionId) {
    var session = await context.TestingSessions.FindAsync(testingSessionId);

    if (session == null) return new { };

    var registrationCount = await context.SessionRegistrations.CountAsync(sr => sr.SessionId == testingSessionId);

    var waitlistCount = await context.SessionWaitlists.CountAsync(sw => sw.SessionId == testingSessionId);

    var feedbackCount = await context.TestingFeedback.CountAsync(tf => tf.SessionId == testingSessionId);

    return new {
      MaxTesters = session.MaxTesters,
      RegisteredCount = registrationCount,
      WaitlistCount = waitlistCount,
      FeedbackCount = feedbackCount,
      AvailableSlots = Math.Max(0, session.MaxTesters - registrationCount),
    };
  }

  public async Task<object> GetUserTestingActivityAsync(Guid userId) {
    var participationCount = await context.TestingParticipants.CountAsync(tp => tp.UserId == userId);

    var sessionRegistrationCount = await context.SessionRegistrations.CountAsync(sr => sr.UserId == userId);

    var feedbackCount = await context.TestingFeedback.CountAsync(tf => tf.UserId == userId);

    var managedSessionCount =
      await context.TestingSessions.CountAsync(ts => ts.ManagerUserId == userId && ts.DeletedAt == null);

    var createdRequestCount =
      await context.TestingRequests.CountAsync(tr => tr.CreatedById == userId && tr.DeletedAt == null);

    return new {
      ParticipationCount = participationCount,
      SessionRegistrationCount = sessionRegistrationCount,
      FeedbackCount = feedbackCount,
      ManagedSessionCount = managedSessionCount,
      CreatedRequestCount = createdRequestCount,
    };
  }

  #endregion
}
