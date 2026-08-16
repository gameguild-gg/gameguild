using GameGuild.Identity.Users;
using GameGuild.Learning.Enrollments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service implementation for course group set management and membership rules.
/// </summary>
public class GroupSetService : IGroupSetService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GroupSetService> _logger;

    public GroupSetService(IApplicationDbContext context, ILogger<GroupSetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ===== SET / GROUP MANAGEMENT =====

    public async Task<Result<CourseGroupSet>> CreateGroupSetAsync(Guid courseId, string name)
    {
        try
        {
            var set = CourseGroupSet.Create(courseId, name);

            _context.Set<CourseGroupSet>().Add(set);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Group set created: {GroupSetId} for course {CourseId}", set.Id, courseId);
            return Result.Success(set);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CourseGroupSet>(Error.Validation("GroupSet.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group set for course {CourseId}", courseId);
            return Result.Failure<CourseGroupSet>(Error.Failure("CreateGroupSet", "Failed to create group set"));
        }
    }

    public async Task<Result<CourseGroup>> CreateGroupAsync(Guid courseId, Guid setId, string name, int capacity)
    {
        try
        {
            var set = await FindSetAsync(courseId, setId).ConfigureAwait(false);
            if (set == null)
            {
                return Result.Failure<CourseGroup>(Error.NotFound("GroupSet", "Group set not found"));
            }

            var group = CourseGroup.Create(setId, name, capacity);

            _context.Set<CourseGroup>().Add(group);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Group created: {GroupId} in set {GroupSetId}", group.Id, setId);
            return Result.Success(group);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CourseGroup>(Error.Validation("Group.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group in set {GroupSetId}", setId);
            return Result.Failure<CourseGroup>(Error.Failure("CreateGroup", "Failed to create group"));
        }
    }

    // ===== READS =====

    public async Task<IReadOnlyList<GroupSetSummaryDto>> GetCourseGroupSetsAsync(Guid courseId)
    {
        var sets = await _context.Set<CourseGroupSet>()
            .Where(s => s.CourseId == courseId && s.DeletedAt == null)
            .OrderBy(s => s.Name)
            .ToListAsync().ConfigureAwait(false);

        var setIds = sets.Select(s => s.Id).ToList();
        var groups = await _context.Set<CourseGroup>()
            .Where(g => setIds.Contains(g.GroupSetId) && g.DeletedAt == null)
            .OrderBy(g => g.Name)
            .ToListAsync().ConfigureAwait(false);

        var groupIds = groups.Select(g => g.Id).ToList();
        var memberCounts = await _context.Set<CourseGroupMember>()
            .Where(m => groupIds.Contains(m.GroupId) && m.DeletedAt == null)
            .GroupBy(m => m.GroupId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count).ConfigureAwait(false);

        return sets
            .Select(s => new GroupSetSummaryDto(
                s.Id,
                s.Name,
                groups
                    .Where(g => g.GroupSetId == s.Id)
                    .Select(g => new GroupSummaryDto(
                        g.Id,
                        g.Name,
                        g.Capacity,
                        memberCounts.GetValueOrDefault(g.Id)))
                    .ToList()))
            .ToList();
    }

    public async Task<Result<IReadOnlyList<GroupDetailDto>>> GetGroupSetGroupsAsync(Guid courseId, Guid setId)
    {
        try
        {
            var set = await FindSetAsync(courseId, setId).ConfigureAwait(false);
            if (set == null)
            {
                return Result.Failure<IReadOnlyList<GroupDetailDto>>(Error.NotFound("GroupSet", "Group set not found"));
            }

            var groups = await _context.Set<CourseGroup>()
                .Where(g => g.GroupSetId == setId && g.DeletedAt == null)
                .OrderBy(g => g.Name)
                .ToListAsync().ConfigureAwait(false);

            var groupIds = groups.Select(g => g.Id).ToList();
            var members = await _context.Set<CourseGroupMember>()
                .Where(m => groupIds.Contains(m.GroupId) && m.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false);

            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = await _context.Set<User>()
                .Where(u => userIds.Contains(u.Id) && u.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false);
            var namesById = users.ToDictionary(u => u.Id, u => u.Name);

            return Result.Success<IReadOnlyList<GroupDetailDto>>(groups
                .Select(g => new GroupDetailDto(
                    g.Id,
                    g.Name,
                    g.Capacity,
                    members.Count(m => m.GroupId == g.Id),
                    members
                        .Where(m => m.GroupId == g.Id)
                        .Select(m => new GroupMemberDto(
                            m.UserId,
                            namesById.TryGetValue(m.UserId, out var name) && !string.IsNullOrWhiteSpace(name)
                                ? name
                                : m.UserId.ToString()))
                        .ToList()))
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing groups for set {GroupSetId}", setId);
            return Result.Failure<IReadOnlyList<GroupDetailDto>>(Error.Failure("GetGroupSetGroups", "Failed to list groups"));
        }
    }

    // ===== MEMBERSHIP =====

    public Task<Result<CourseGroupMember>> JoinAsync(Guid courseId, Guid groupId, Guid userId) =>
        AddMemberCoreAsync(courseId, groupId, userId, bypassLock: false);

    public Task<Result<CourseGroupMember>> AddMemberAsync(Guid courseId, Guid groupId, Guid userId) =>
        AddMemberCoreAsync(courseId, groupId, userId, bypassLock: true);

    public Task<Result> LeaveAsync(Guid courseId, Guid groupId, Guid userId) =>
        RemoveMemberCoreAsync(courseId, groupId, userId, bypassLock: false);

    public Task<Result> RemoveMemberAsync(Guid courseId, Guid groupId, Guid userId) =>
        RemoveMemberCoreAsync(courseId, groupId, userId, bypassLock: true);

    public async Task<bool> HasActiveEnrollmentAsync(Guid courseId, Guid userId)
    {
        return await _context.Set<Enrollment>()
            .AnyAsync(e => e.CourseId == courseId &&
                           e.UserId == userId &&
                           e.Status == EnrollmentStatus.Active &&
                           e.DeletedAt == null)
            .ConfigureAwait(false);
    }

    private async Task<Result<CourseGroupMember>> AddMemberCoreAsync(Guid courseId, Guid groupId, Guid userId, bool bypassLock)
    {
        try
        {
            var (group, set) = await FindGroupAndSetAsync(courseId, groupId).ConfigureAwait(false);
            if (group == null || set == null)
            {
                return Result.Failure<CourseGroupMember>(Error.NotFound("Group", "Group not found"));
            }

            if (!await HasActiveEnrollmentAsync(courseId, userId).ConfigureAwait(false))
            {
                return Result.Failure<CourseGroupMember>(Error.Validation(
                    "GroupMembership.EnrollmentRequired",
                    "You must have an active enrollment in the course to join a group."));
            }

            var setGroupIds = await _context.Set<CourseGroup>()
                .Where(g => g.GroupSetId == set.Id && g.DeletedAt == null)
                .Select(g => g.Id)
                .ToListAsync().ConfigureAwait(false);
            var alreadyInSet = await _context.Set<CourseGroupMember>()
                .AnyAsync(m => m.UserId == userId && m.DeletedAt == null && setGroupIds.Contains(m.GroupId))
                .ConfigureAwait(false);
            if (alreadyInSet)
            {
                return Result.Failure<CourseGroupMember>(Error.Validation(
                    "GroupMembership.AlreadyInSet",
                    "You are already in a group in this set."));
            }

            if (!bypassLock && await IsSetLockedAsync(set.Id).ConfigureAwait(false))
            {
                return Result.Failure<CourseGroupMember>(Error.Validation(
                    "GroupMembership.Locked",
                    "Group membership is locked because a linked assessment is due."));
            }

            var memberCount = await _context.Set<CourseGroupMember>()
                .CountAsync(m => m.GroupId == group.Id && m.DeletedAt == null)
                .ConfigureAwait(false);
            if (memberCount >= group.Capacity)
            {
                return Result.Failure<CourseGroupMember>(Error.Validation(
                    "GroupMembership.GroupFull",
                    "This group is full."));
            }

            // ponytail: count-then-insert leaves a small race window for concurrent joins at capacity-1;
            // transient over-capacity is acceptable v1 — upgrade path is a DB constraint or serializable transaction.
            var member = CourseGroupMember.Create(group.Id, userId);
            _context.Set<CourseGroupMember>().Add(member);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Group membership added: user {UserId} joined group {GroupId}", userId, groupId);
            return Result.Success(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to group {GroupId}", userId, groupId);
            return Result.Failure<CourseGroupMember>(Error.Failure("JoinGroup", "Failed to join group"));
        }
    }

    private async Task<Result> RemoveMemberCoreAsync(Guid courseId, Guid groupId, Guid userId, bool bypassLock)
    {
        try
        {
            var (group, set) = await FindGroupAndSetAsync(courseId, groupId).ConfigureAwait(false);
            if (group == null || set == null)
            {
                return Result.Failure(Error.NotFound("Group", "Group not found"));
            }

            var membership = await _context.Set<CourseGroupMember>()
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId && m.DeletedAt == null)
                .ConfigureAwait(false);
            if (membership == null)
            {
                return Result.Failure(Error.NotFound("GroupMembership", "Membership not found"));
            }

            if (!bypassLock && await IsSetLockedAsync(set.Id).ConfigureAwait(false))
            {
                return Result.Failure(Error.Validation(
                    "GroupMembership.Locked",
                    "Group membership is locked because a linked assessment is due."));
            }

            // ponytail: hard delete — the unique (GroupId, UserId) index is unfiltered, so a soft-deleted row would block rejoining.
            _context.Set<CourseGroupMember>().Remove(membership);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Group membership removed: user {UserId} left group {GroupId}", userId, groupId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from group {GroupId}", userId, groupId);
            return Result.Failure(Error.Failure("LeaveGroup", "Failed to leave group"));
        }
    }

    private async Task<bool> IsSetLockedAsync(Guid setId)
    {
        var now = SystemClock.UtcNow;
        // Lock-at-due: deliberately excludes LateSubmissionDeadline — joining closes at the due date
        // even while late submissions are still open (plan-documented asymmetry; peer review runs later).
        return await _context.Set<Assessment>()
            .AnyAsync(a => a.GroupSetId == setId &&
                           a.DeletedAt == null &&
                           (a.DueAt ?? a.AvailableUntil) <= now)
            .ConfigureAwait(false);
    }

    private async Task<CourseGroupSet?> FindSetAsync(Guid courseId, Guid setId)
    {
        return await _context.Set<CourseGroupSet>()
            .FirstOrDefaultAsync(s => s.Id == setId && s.CourseId == courseId && s.DeletedAt == null)
            .ConfigureAwait(false);
    }

    private async Task<(CourseGroup? Group, CourseGroupSet? Set)> FindGroupAndSetAsync(Guid courseId, Guid groupId)
    {
        var group = await _context.Set<CourseGroup>()
            .FirstOrDefaultAsync(g => g.Id == groupId && g.DeletedAt == null)
            .ConfigureAwait(false);
        if (group == null)
        {
            return (null, null);
        }

        var set = await FindSetAsync(courseId, group.GroupSetId).ConfigureAwait(false);
        return (group, set);
    }
}
