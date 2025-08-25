using GameGuild.Common;
using Microsoft.EntityFrameworkCore;
using GameGuild.Database;

namespace GameGuild.Modules.TestingLab;

public class TestingSessionService : ITestingSessionService
{
    private readonly ApplicationDbContext _context;

    public TestingSessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TestingSession>> GetAllAsync()
    {
        return await _context.TestingSessions
            .Where(ts => ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetWithPaginationAsync(int skip = 0, int take = 50)
    {
        return await _context.TestingSessions
            .Where(ts => ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderByDescending(ts => ts.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<TestingSession?> GetByIdAsync(Guid id)
    {
        return await _context.TestingSessions
            .Where(ts => ts.Id == id && ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .FirstOrDefaultAsync();
    }

    public async Task<TestingSession?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.TestingSessions
            .Where(ts => ts.Id == id && ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .Include(ts => ts.Registrations)
            .FirstOrDefaultAsync();
    }

    public async Task<TestingSession> CreateAsync(TestingSession testingSession)
    {
        testingSession.Id = Guid.NewGuid();
        testingSession.CreatedAt = DateTime.UtcNow;
        testingSession.UpdatedAt = DateTime.UtcNow;

        _context.TestingSessions.Add(testingSession);
        await _context.SaveChangesAsync();

        return testingSession;
    }

    public async Task<TestingSession> UpdateAsync(TestingSession testingSession)
    {
        var existingSession = await _context.TestingSessions.FindAsync(testingSession.Id);
        
        if (existingSession == null)
            throw new InvalidOperationException($"Testing session with ID {testingSession.Id} not found.");

        // Update properties
        existingSession.SessionName = testingSession.SessionName;
        existingSession.SessionDate = testingSession.SessionDate;
        existingSession.StartTime = testingSession.StartTime;
        existingSession.EndTime = testingSession.EndTime;
        existingSession.MaxTesters = testingSession.MaxTesters;
        existingSession.Status = testingSession.Status;
        existingSession.ManagerUserId = testingSession.ManagerUserId;
        existingSession.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(existingSession.Id) ?? existingSession;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var session = await _context.TestingSessions.FindAsync(id);
        
        if (session == null) 
            return false;

        session.DeletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var session = await _context.TestingSessions.FindAsync(id);
        
        if (session == null) 
            return false;

        session.DeletedAt = null;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<IEnumerable<TestingSession>> GetByTestingRequestAsync(Guid testingRequestId)
    {
        return await _context.TestingSessions
            .Where(ts => ts.TestingRequestId == testingRequestId && ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.SessionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetByStatusAsync(SessionStatus status)
    {
        return await _context.TestingSessions
            .Where(ts => ts.Status == status && ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.SessionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetUpcomingSessionsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.TestingSessions
            .Where(ts => ts.StartTime > now && ts.DeletedAt == null && ts.Status == SessionStatus.Scheduled)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetActiveSessionsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.TestingSessions
            .Where(ts => ts.StartTime <= now && ts.EndTime >= now && ts.DeletedAt == null && ts.Status == SessionStatus.Active)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetByLocationAsync(Guid locationId)
    {
        return await _context.TestingSessions
            .Where(ts => ts.LocationId == locationId && ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.SessionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.TestingSessions
            .Where(ts => ts.SessionDate >= startDate && ts.SessionDate <= endDate && ts.DeletedAt == null)
            .Include(ts => ts.TestingRequest)
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.SessionDate)
            .ToListAsync();
    }

    public async Task<bool> CanUserJoinSessionAsync(Guid userId, Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session == null) return false;

        var registrationCount = await _context.SessionRegistrations
            .CountAsync(sr => sr.SessionId == testingSessionId);

        return registrationCount < session.MaxTesters;
    }

    public async Task<TestingSession> JoinSessionAsync(Guid userId, Guid testingSessionId)
    {
        // Check if user is already registered
        var existingRegistration = await _context.SessionRegistrations
            .FirstOrDefaultAsync(sr => sr.SessionId == testingSessionId && sr.UserId == userId);

        if (existingRegistration != null)
            throw new InvalidOperationException("User is already registered for this session");

        // Check if session has available slots
        if (!await CanUserJoinSessionAsync(userId, testingSessionId))
            throw new InvalidOperationException("Session is full");

        var registration = new SessionRegistration
        {
            SessionId = testingSessionId,
            UserId = userId,
            RegistrationType = RegistrationType.Tester,
            AttendanceStatus = AttendanceStatus.Registered
        };

        _context.SessionRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(testingSessionId) 
            ?? throw new InvalidOperationException("Session not found after joining");
    }

    public async Task<TestingSession> LeaveSessionAsync(Guid userId, Guid testingSessionId)
    {
        var registration = await _context.SessionRegistrations
            .FirstOrDefaultAsync(sr => sr.SessionId == testingSessionId && sr.UserId == userId);

        if (registration == null)
            throw new InvalidOperationException("User is not registered for this session");

        _context.SessionRegistrations.Remove(registration);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(testingSessionId) 
            ?? throw new InvalidOperationException("Session not found after leaving");
    }

    public async Task<TestingSession> StartSessionAsync(Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session == null)
            throw new InvalidOperationException("Session not found");

        session.Status = SessionStatus.Active;
        session.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return session;
    }

    public async Task<TestingSession> EndSessionAsync(Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session == null)
            throw new InvalidOperationException("Session not found");

        session.Status = SessionStatus.Completed;
        session.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return session;
    }

    public async Task<TestingSession> CancelSessionAsync(Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session == null)
            throw new InvalidOperationException("Session not found");

        session.Status = SessionStatus.Cancelled;
        session.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return session;
    }
}
