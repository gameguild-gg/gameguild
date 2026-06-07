

namespace GameGuild.TestingLab;

public class TestingSessionService : ITestingSessionService {
  private readonly IApplicationDbContext _context;

  public TestingSessionService(IApplicationDbContext context) { _context = context; }

  public async Task<IEnumerable<TestingSession>> GetAllAsync() {
    return await _context.Set<TestingSession>().Where(ts => ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderByDescending(ts => ts.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetWithPaginationAsync(int skip = 0, int take = 50) {
    return await _context.Set<TestingSession>().Where(ts => ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderByDescending(ts => ts.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<TestingSession?> GetByIdAsync(Guid id) { return await _context.Set<TestingSession>().Where(ts => ts.Id == id && ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).FirstOrDefaultAsync(); }

  public async Task<TestingSession?> GetByIdWithDetailsAsync(Guid id) {
    return await _context.Set<TestingSession>().Where(ts => ts.Id == id && ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).Include(ts => ts.Registrations).FirstOrDefaultAsync();
  }

  public async Task<TestingSession> CreateAsync(TestingSession testingSession) {
    testingSession.Id = Guid.NewGuid();
    testingSession.Touch();

    _context.Set<TestingSession>().Add(testingSession);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return testingSession;
  }

  public async Task<TestingSession> UpdateAsync(TestingSession testingSession) {
    var existingSession = await _context.Set<TestingSession>().FindAsync(testingSession.Id).ConfigureAwait(false);

    if (existingSession == null) throw new InvalidOperationException($"Testing session with ID {testingSession.Id} not found.");

    // Update properties
    existingSession.SessionName = testingSession.SessionName;
    existingSession.SessionDate = testingSession.SessionDate;
    existingSession.StartTime = testingSession.StartTime;
    existingSession.EndTime = testingSession.EndTime;
    existingSession.MaxTesters = testingSession.MaxTesters;
    existingSession.Status = testingSession.Status;
    existingSession.ManagerUserId = testingSession.ManagerUserId;
    existingSession.Touch();

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return (await GetByIdAsync(existingSession.Id).ConfigureAwait(false)) ?? existingSession;
  }

  public async Task<bool> DeleteAsync(Guid id) {
    var session = await _context.Set<TestingSession>().FindAsync(id).ConfigureAwait(false);

    if (session == null) return false;

    session.SoftDelete();
    session.Touch();
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  public async Task<bool> RestoreAsync(Guid id) {
    var session = await _context.Set<TestingSession>().FindAsync(id).ConfigureAwait(false);

    if (session == null) return false;

    session.Restore();
    session.Touch();
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  public async Task<IEnumerable<TestingSession>> GetByTestingRequestAsync(Guid testingRequestId) {
    return await _context.Set<TestingSession>().Where(ts => ts.TestingRequestId == testingRequestId && ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderBy(ts => ts.SessionDate).ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetByStatusAsync(SessionStatus status) {
    return await _context.Set<TestingSession>().Where(ts => ts.Status == status && ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderBy(ts => ts.SessionDate).ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetUpcomingSessionsAsync() {
    var now = SystemClock.UtcNow;

    return await _context.Set<TestingSession>().Where(ts => ts.StartTime > now && ts.DeletedAt == null && ts.Status == SessionStatus.Scheduled).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderBy(ts => ts.StartTime).ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetActiveSessionsAsync() {
    var now = SystemClock.UtcNow;

    return await _context.Set<TestingSession>().Where(ts => ts.StartTime <= now && ts.EndTime >= now && ts.DeletedAt == null && ts.Status == SessionStatus.Active)
                         .Include(ts => ts.TestingRequest)
                         .Include(ts => ts.Location)
                         .OrderBy(ts => ts.StartTime)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetByLocationAsync(Guid locationId) {
    return await _context.Set<TestingSession>().Where(ts => ts.LocationId == locationId && ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderBy(ts => ts.SessionDate).ToListAsync();
  }

  public async Task<IEnumerable<TestingSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) {
    return await _context.Set<TestingSession>().Where(ts => ts.SessionDate >= startDate && ts.SessionDate <= endDate && ts.DeletedAt == null).Include(ts => ts.TestingRequest).Include(ts => ts.Location).OrderBy(ts => ts.SessionDate).ToListAsync();
  }

  public async Task<bool> CanUserJoinSessionAsync(Guid userId, Guid testingSessionId) {
    var session = await GetByIdAsync(testingSessionId).ConfigureAwait(false);

    if (session == null) return false;

    var registrationCount = await _context.Set<SessionRegistration>().CountAsync(sr => sr.SessionId == testingSessionId);

    return registrationCount < session.MaxTesters;
  }

  public async Task<TestingSession> JoinSessionAsync(Guid userId, Guid testingSessionId) {
    // Check if user is already registered
    var existingRegistration = await _context.Set<SessionRegistration>().FirstOrDefaultAsync(sr => sr.SessionId == testingSessionId && sr.UserId == userId);

    if (existingRegistration != null) throw new InvalidOperationException("User is already registered for this session");

    // Check if session has available slots
    if (!await CanUserJoinSessionAsync(userId, testingSessionId)) throw new InvalidOperationException("Session is full");

    var registration = new SessionRegistration { SessionId = testingSessionId, UserId = userId, RegistrationType = RegistrationType.Tester, AttendanceStatus = AttendanceStatus.Registered };

    _context.Set<SessionRegistration>().Add(registration);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return await GetByIdAsync(testingSessionId) ?? throw new InvalidOperationException("Session not found after joining");
  }

  public async Task<TestingSession> LeaveSessionAsync(Guid userId, Guid testingSessionId) {
    var registration = await _context.Set<SessionRegistration>().FirstOrDefaultAsync(sr => sr.SessionId == testingSessionId && sr.UserId == userId);

    if (registration == null) throw new InvalidOperationException("User is not registered for this session");

    _context.Set<SessionRegistration>().Remove(registration);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return await GetByIdAsync(testingSessionId) ?? throw new InvalidOperationException("Session not found after leaving");
  }

  public async Task<TestingSession> StartSessionAsync(Guid testingSessionId) {
    var session = await GetByIdAsync(testingSessionId).ConfigureAwait(false);

    if (session == null) throw new InvalidOperationException("Session not found");

    session.Status = SessionStatus.Active;
    session.Touch();

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return session;
  }

  public async Task<TestingSession> EndSessionAsync(Guid testingSessionId) {
    var session = await GetByIdAsync(testingSessionId).ConfigureAwait(false);

    if (session == null) throw new InvalidOperationException("Session not found");

    session.Status = SessionStatus.Completed;
    session.Touch();

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return session;
  }

  public async Task<TestingSession> CancelSessionAsync(Guid testingSessionId) {
    var session = await GetByIdAsync(testingSessionId).ConfigureAwait(false);

    if (session == null) throw new InvalidOperationException("Session not found");

    session.Status = SessionStatus.Cancelled;
    session.Touch();

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return session;
  }
}
