namespace GameGuild.TestingLab;

/// <summary>
/// Service implementation for participant management, registration, and waitlist operations.
/// Extracted from the monolithic TestService for focused responsibility.
/// </summary>
public class TestingParticipantOperationsService(IApplicationDbContext context) : ITestingParticipantOperations
{
    #region Participant Management

    public async Task<TestingParticipant> AddParticipantAsync(Guid testingRequestId, Guid userId)
    {
        var existingParticipant = await context.Set<TestingParticipant>()
            .FirstOrDefaultAsync(tp => tp.TestingRequestId == testingRequestId && tp.UserId == userId);

        if (existingParticipant != null) return existingParticipant;

        var participant = new TestingParticipant
        {
            Id = Guid.NewGuid(),
            TestingRequestId = testingRequestId,
            UserId = userId
        };

        context.Set<TestingParticipant>().Add(participant);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return await context.Set<TestingParticipant>()
            .Include(tp => tp.TestingRequest)
            .Include(tp => tp.User)
            .FirstAsync(tp => tp.Id == participant.Id);
    }

    public async Task<bool> RemoveParticipantAsync(Guid testingRequestId, Guid userId)
    {
        var participant = await context.Set<TestingParticipant>()
            .FirstOrDefaultAsync(tp => tp.TestingRequestId == testingRequestId && tp.UserId == userId);

        if (participant == null) return false;

        context.Set<TestingParticipant>().Remove(participant);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<IEnumerable<TestingParticipant>> GetTestingRequestParticipantsAsync(Guid testingRequestId)
    {
        return await context.Set<TestingParticipant>()
            .Where(tp => tp.TestingRequestId == testingRequestId)
            .Include(tp => tp.User)
            .OrderBy(tp => tp.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsUserParticipantAsync(Guid testingRequestId, Guid userId)
    {
        return await context.Set<TestingParticipant>()
            .AnyAsync(tp => tp.TestingRequestId == testingRequestId && tp.UserId == userId);
    }

    #endregion

    #region Session Registration

    public async Task<SessionRegistration> RegisterForSessionAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null)
    {
        var existingRegistration = await context.Set<SessionRegistration>()
            .FirstOrDefaultAsync(sr => sr.SessionId == sessionId && sr.UserId == userId);

        if (existingRegistration != null) return existingRegistration;

        var registration = new SessionRegistration
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            RegistrationType = registrationType,
            RegistrationNotes = notes
        };

        context.Set<SessionRegistration>().Add(registration);

        var session = await context.Set<TestingSession>().FindAsync(sessionId).ConfigureAwait(false);

        if (session != null)
        {
            if (registrationType == RegistrationType.Tester)
                session.RegisteredTesterCount++;
            else if (registrationType == RegistrationType.ProjectMember)
                session.RegisteredProjectMemberCount++;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        return await context.Set<SessionRegistration>()
            .Include(sr => sr.Session)
            .Include(sr => sr.User)
            .FirstAsync(sr => sr.Id == registration.Id);
    }

    public async Task<bool> UnregisterFromSessionAsync(Guid sessionId, Guid userId)
    {
        var registration = await context.Set<SessionRegistration>()
            .FirstOrDefaultAsync(sr => sr.SessionId == sessionId && sr.UserId == userId);

        if (registration == null) return false;

        var session = await context.Set<TestingSession>().FindAsync(sessionId).ConfigureAwait(false);

        if (session != null)
        {
            if (registration.RegistrationType == RegistrationType.Tester)
                session.RegisteredTesterCount = Math.Max(0, session.RegisteredTesterCount - 1);
            else if (registration.RegistrationType == RegistrationType.ProjectMember)
                session.RegisteredProjectMemberCount = Math.Max(0, session.RegisteredProjectMemberCount - 1);
        }

        context.Set<SessionRegistration>().Remove(registration);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<IEnumerable<SessionRegistration>> GetSessionRegistrationsAsync(Guid sessionId)
    {
        return await context.Set<SessionRegistration>()
            .Where(sr => sr.SessionId == sessionId)
            .Include(sr => sr.User)
            .OrderBy(sr => sr.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Waitlist

    public async Task<SessionWaitlist> AddToWaitlistAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null)
    {
        var existingWaitlist = await context.Set<SessionWaitlist>()
            .FirstOrDefaultAsync(sw => sw.SessionId == sessionId && sw.UserId == userId);

        if (existingWaitlist != null) return existingWaitlist;

        var maxPosition = await context.Set<SessionWaitlist>()
            .Where(sw => sw.SessionId == sessionId)
            .MaxAsync(sw => (int?)sw.Position) ?? 0;

        var waitlistEntry = new SessionWaitlist
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            RegistrationType = registrationType,
            Position = maxPosition + 1,
            RegistrationNotes = notes,
        };

        context.Set<SessionWaitlist>().Add(waitlistEntry);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return await context.Set<SessionWaitlist>()
            .Include(sw => sw.Session)
            .Include(sw => sw.User)
            .FirstAsync(sw => sw.Id == waitlistEntry.Id);
    }

    public async Task<bool> RemoveFromWaitlistAsync(Guid sessionId, Guid userId)
    {
        var waitlistEntry = await context.Set<SessionWaitlist>()
            .FirstOrDefaultAsync(sw => sw.SessionId == sessionId && sw.UserId == userId);

        if (waitlistEntry == null) return false;

        var removedPosition = waitlistEntry.Position;

        context.Set<SessionWaitlist>().Remove(waitlistEntry);

        var remainingEntries = await context.Set<SessionWaitlist>()
            .Where(sw => sw.SessionId == sessionId && sw.Position > removedPosition)
            .ToListAsync();

        foreach (var entry in remainingEntries) entry.Position--;

        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<IEnumerable<SessionWaitlist>> GetSessionWaitlistAsync(Guid sessionId)
    {
        return await context.Set<SessionWaitlist>()
            .Where(sw => sw.SessionId == sessionId)
            .Include(sw => sw.User)
            .OrderBy(sw => sw.Position)
            .ToListAsync();
    }

    #endregion

    #region User Activity & Attendance

    public async Task<object> GetUserTestingActivityAsync(Guid userId)
    {
        var participationCount = await context.Set<TestingParticipant>().CountAsync(tp => tp.UserId == userId);
        var sessionRegistrationCount = await context.Set<SessionRegistration>().CountAsync(sr => sr.UserId == userId);
        var feedbackCount = await context.Set<TestingFeedback>().CountAsync(tf => tf.UserId == userId);
        var managedSessionCount = await context.Set<TestingSession>().CountAsync(ts => ts.ManagerUserId == userId && ts.DeletedAt == null);
        var createdRequestCount = await context.Set<TestingRequest>().CountAsync(tr => tr.CreatedById == userId && tr.DeletedAt == null);

        return new
        {
            ParticipationCount = participationCount,
            SessionRegistrationCount = sessionRegistrationCount,
            FeedbackCount = feedbackCount,
            ManagedSessionCount = managedSessionCount,
            CreatedRequestCount = createdRequestCount
        };
    }

    public Task<object> GetStudentAttendanceReportAsync()
    {
        var mockData = new[]
        {
            new
            {
                Id = "1",
                Name = "John Developer",
                Email = "john.dev@mymail.champlain.edu",
                Team = "fa23-capstone-2023-24-t01",
                Block1Sessions = 2,
                Block2Sessions = 1,
                Block3Sessions = 0,
                Block4Sessions = 0,
                TotalSessions = 3,
                GamesTested = 8,
                Status = "onTrack",
            },
            new
            {
                Id = "2",
                Name = "Jane Smith",
                Email = "jane.smith@mymail.champlain.edu",
                Team = "fa23-capstone-2023-24-t02",
                Block1Sessions = 1,
                Block2Sessions = 1,
                Block3Sessions = 0,
                Block4Sessions = 0,
                TotalSessions = 2,
                GamesTested = 4,
                Status = "atRisk",
            },
        };

        return Task.FromResult<object>(mockData);
    }

    #endregion
}
