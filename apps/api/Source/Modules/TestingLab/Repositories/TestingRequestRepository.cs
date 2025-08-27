using GameGuild.Common;
using Microsoft.EntityFrameworkCore;
using GameGuild.Database;

namespace GameGuild.Modules.TestingLab;

public class TestingRequestRepository : ITestingRequestRepository
{
  private readonly ApplicationDbContext _context;

  public TestingRequestRepository(ApplicationDbContext context) {
    _context = context;
  }

  public async Task<IEnumerable<TestingRequest>> GetWithPaginationAsync(int skip = 0, int take = 50) {
    return await _context.TestingRequests.Where(r => r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .OrderByDescending(r => r.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<TestingRequest?> GetByIdWithDetailsAsync(Guid id) {
    return await _context.TestingRequests.Where(r => r.Id == id && r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .FirstOrDefaultAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetByProjectVersionAsync(Guid projectVersionId) {
    return await _context.TestingRequests.Where(r => r.ProjectVersionId == projectVersionId && r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status) {
    return await _context.TestingRequests.Where(r => r.Status == status && r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status, int skip = 0, int take = 50) {
    return await _context.TestingRequests.Where(r => r.Status == status && r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .OrderByDescending(r => r.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetActiveRequestsAsync() {
    var now = DateTimeOffset.UtcNow;
    return await _context.TestingRequests.Where(r => r.Status == TestingRequestStatus.Open &&
                                                     r.StartDate <= now &&
                                                     r.EndDate >= now &&
                                                     r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetExpiredRequestsAsync() {
    var now = DateTimeOffset.UtcNow;
    return await _context.TestingRequests.Where(r => r.EndDate < now &&
                                                     r.Status != TestingRequestStatus.Completed &&
                                                     r.Status != TestingRequestStatus.Cancelled &&
                                                     r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetByCreatedByAsync(Guid userId) {
    return await _context.TestingRequests.Where(r => r.CreatedById == userId && r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> GetRequestsNeedingClosureAsync() {
    var now = DateTimeOffset.UtcNow;
    return await _context.TestingRequests.Where(r => r.EndDate < now &&
                                                     (r.Status == TestingRequestStatus.Open || r.Status == TestingRequestStatus.InProgress) &&
                                                     r.DeletedAt == null)
                        .Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .OrderBy(r => r.EndDate)
                        .ToListAsync();
  }

  public async Task<IEnumerable<TestingRequest>> SearchAsync(string searchTerm) {
    var lowerSearchTerm = searchTerm.ToLowerInvariant();

    return await _context.TestingRequests.Include(r => r.CreatedBy)
                        .Include(r => r.ProjectVersion)
                        .ThenInclude(pv => pv.Project)
                        .Where(r => r.Title.ToLowerInvariant().Contains(lowerSearchTerm) ||
                                    (r.Description != null && r.Description.ToLowerInvariant().Contains(lowerSearchTerm)) ||
                                    r.ProjectVersion.Project.Title.ToLowerInvariant().Contains(lowerSearchTerm))
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
  }
}
