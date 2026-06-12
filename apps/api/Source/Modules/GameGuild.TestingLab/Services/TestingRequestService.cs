

namespace GameGuild.TestingLab;

public class TestingRequestService : ITestingRequestService {
  private readonly IApplicationDbContext _context;

  public TestingRequestService(IApplicationDbContext context) { _context = context; }

  public async Task<IEnumerable<TestingRequest>> GetAllAsync() {
    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).OrderByDescending(r => r.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetWithPaginationAsync(int skip = 0, int take = 50) {
    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).OrderByDescending(r => r.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<TestingRequest?> GetByIdAsync(Guid id) { return await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).FirstOrDefaultAsync(r => r.Id == id); }

  public async Task<TestingRequest?> GetByIdWithDetailsAsync(Guid id) { return await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).FirstOrDefaultAsync(r => r.Id == id); }

  public async Task<TestingRequest> CreateAsync(TestingRequest testingRequest) {
    testingRequest.Id = Guid.NewGuid();
    testingRequest.Touch();

    _context.Set<TestingRequest>().Add(testingRequest);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return testingRequest;
  }

  public async Task<TestingRequest> UpdateAsync(TestingRequest testingRequest) {
    testingRequest.Touch();

    _context.Set<TestingRequest>().Update(testingRequest);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return testingRequest;
  }

  public async Task<bool> DeleteAsync(Guid id) {
    var testingRequest = await _context.Set<TestingRequest>().FirstOrDefaultAsync(r => r.Id == id);

    if (testingRequest == null) return false;

    _context.Set<TestingRequest>().Remove(testingRequest);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  public async Task<bool> RestoreAsync(Guid id) {
    // Assuming there's a soft delete mechanism, but based on the entities I saw,
    // there doesn't seem to be one. For now, returning false.
    await Task.CompletedTask;

    return false;
  }

  public async Task<IEnumerable<TestingRequest>> GetByProjectVersionAsync(Guid projectVersionId) {
    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).Where(r => r.ProjectVersionId == projectVersionId).OrderByDescending(r => r.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status) {
    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).Where(r => r.Status == status).OrderByDescending(r => r.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetActiveRequestsAsync() {
    var now = SystemClock.UtcNow;

    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy)
                         .Include(r => r.ProjectVersion)
                         .ThenInclude(pv => pv!.Project)
                         .Where(r => r.Status == TestingRequestStatus.Open && r.StartDate <= now && r.EndDate >= now)
                         .OrderBy(r => r.EndDate)
                         .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetRequestsNeedingClosureAsync() {
    var now = SystemClock.UtcNow;

    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy)
                         .Include(r => r.ProjectVersion)
                         .ThenInclude(pv => pv!.Project)
                         .Where(r => (r.Status == TestingRequestStatus.Open || r.Status == TestingRequestStatus.InProgress) && r.EndDate < now)
                         .OrderBy(r => r.EndDate)
                         .ToListAsync();
  }

  public async Task<bool> CanUserJoinTestingAsync(Guid userId, Guid testingRequestId) {
    var request = await _context.Set<TestingRequest>().FirstOrDefaultAsync(r => r.Id == testingRequestId);

    if (request == null || request.Status != TestingRequestStatus.Open) return false;

    // Check if user is already participating
    var existingParticipant = await _context.Set<TestingParticipant>().FirstOrDefaultAsync(p => p.TestingRequestId == testingRequestId && p.UserId == userId);

    if (existingParticipant != null) return false; // Already participating

    // Check if there's space
    if (request.MaxTesters.HasValue && request.CurrentTesterCount >= request.MaxTesters.Value) return false;

    return true;
  }

  public async Task<TestingRequest> JoinTestingAsync(Guid userId, Guid testingRequestId) {
    var request = await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).FirstOrDefaultAsync(r => r.Id == testingRequestId);

    if (request == null || request.Status != TestingRequestStatus.Open) throw new InvalidOperationException("Testing request is not available for joining");

    // Check if user is already participating
    var existingParticipant = await _context.Set<TestingParticipant>().FirstOrDefaultAsync(p => p.TestingRequestId == testingRequestId && p.UserId == userId);

    if (existingParticipant != null) throw new InvalidOperationException("User is already participating in this testing request");

    // Check if there's space
    if (request.MaxTesters.HasValue && request.CurrentTesterCount >= request.MaxTesters.Value) throw new InvalidOperationException("Testing request has reached maximum testers");

    var participant = new TestingParticipant { TestingRequestId = testingRequestId, UserId = userId, StartedAt = SystemClock.UtcNow };

    _context.Set<TestingParticipant>().Add(participant);
    request.CurrentTesterCount++;

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return request;
  }

  public async Task<TestingRequest> LeaveTestingAsync(Guid userId, Guid testingRequestId) {
    var request = await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).FirstOrDefaultAsync(r => r.Id == testingRequestId);

    if (request == null) throw new ArgumentException("Testing request not found");

    var participant = await _context.Set<TestingParticipant>().FirstOrDefaultAsync(p => p.TestingRequestId == testingRequestId && p.UserId == userId);

    if (participant == null) throw new InvalidOperationException("User is not participating in this testing request");

    _context.Set<TestingParticipant>().Remove(participant);
    request.CurrentTesterCount = Math.Max(0, request.CurrentTesterCount - 1);

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return request;
  }

  public async Task<TestingRequest> CloseTestingRequestAsync(Guid testingRequestId) {
    var request = await _context.Set<TestingRequest>().Include(r => r.CreatedBy).Include(r => r.ProjectVersion).ThenInclude(pv => pv!.Project).FirstOrDefaultAsync(r => r.Id == testingRequestId);

    if (request == null) throw new ArgumentException("Testing request not found");

    request.Status = TestingRequestStatus.Completed;
    request.Touch();

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return request;
  }

  public async Task<IEnumerable<TestingRequest>> SearchAsync(string searchTerm) {
    var lowerSearchTerm = searchTerm.ToLowerInvariant();

    return await _context.Set<TestingRequest>().Include(r => r.CreatedBy)
                         .Include(r => r.ProjectVersion)
                         .ThenInclude(pv => pv!.Project)
                         .Where(r => r.Title.ToLowerInvariant().Contains(lowerSearchTerm) ||
                                     r.Description != null && r.Description.ToLowerInvariant().Contains(lowerSearchTerm) ||
                                     r.ProjectVersion != null && r.ProjectVersion.Project != null && r.ProjectVersion.Project.Title.ToLowerInvariant().Contains(lowerSearchTerm)
                         )
                         .OrderByDescending(r => r.CreatedAt)
                         .ToListAsync();
  }
}
