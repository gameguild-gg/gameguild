namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service interface for course group set management and membership rules.
/// </summary>
public interface IGroupSetService
{
    /// <summary>
    /// Creates a group set for a course.
    /// </summary>
    Task<Result<CourseGroupSet>> CreateGroupSetAsync(Guid courseId, string name);

    /// <summary>
    /// Creates a group (capacity at least two) inside a course's group set.
    /// </summary>
    Task<Result<CourseGroup>> CreateGroupAsync(Guid courseId, Guid setId, string name, int capacity);

    /// <summary>
    /// Lists a course's group sets with a per-group summary (id, name, capacity, member count).
    /// </summary>
    Task<IReadOnlyList<GroupSetSummaryDto>> GetCourseGroupSetsAsync(Guid courseId);

    /// <summary>
    /// Lists the groups of one group set with member display names and counts.
    /// Fails with NotFound when the set does not belong to the course.
    /// </summary>
    Task<Result<IReadOnlyList<GroupDetailDto>>> GetGroupSetGroupsAsync(Guid courseId, Guid setId);

    /// <summary>
    /// Student self-signup: enforces active enrollment, one group per set,
    /// capacity, and the lock-at-due rule.
    /// </summary>
    Task<Result<CourseGroupMember>> JoinAsync(Guid courseId, Guid groupId, Guid userId);

    /// <summary>
    /// Student leaves their own membership. Subject to the lock-at-due rule.
    /// </summary>
    Task<Result> LeaveAsync(Guid courseId, Guid groupId, Guid userId);

    /// <summary>
    /// Instructor manual add: bypasses the lock-at-due rule but not capacity
    /// or the active-enrollment requirement.
    /// </summary>
    Task<Result<CourseGroupMember>> AddMemberAsync(Guid courseId, Guid groupId, Guid userId);

    /// <summary>
    /// Instructor manual remove: bypasses the lock-at-due rule.
    /// </summary>
    Task<Result> RemoveMemberAsync(Guid courseId, Guid groupId, Guid userId);

    /// <summary>
    /// True when the user holds a non-deleted Active enrollment in the course.
    /// </summary>
    Task<bool> HasActiveEnrollmentAsync(Guid courseId, Guid userId);
}

/// <summary>
/// Request to create a group set.
/// </summary>
public sealed record CreateGroupSetRequest(string Name);

/// <summary>
/// Request to create a group inside a group set.
/// </summary>
public sealed record CreateGroupRequest(string Name, int Capacity);

/// <summary>
/// Group set listing entry with per-group summary.
/// </summary>
public sealed record GroupSetSummaryDto(
    Guid Id,
    string Name,
    IReadOnlyList<GroupSummaryDto> Groups);

/// <summary>
/// Group summary (no member identities).
/// </summary>
public sealed record GroupSummaryDto(
    Guid Id,
    string Name,
    int Capacity,
    int MemberCount);

/// <summary>
/// Group detail with member display names.
/// </summary>
public sealed record GroupDetailDto(
    Guid Id,
    string Name,
    int Capacity,
    int MemberCount,
    IReadOnlyList<GroupMemberDto> Members);

/// <summary>
/// Group member with display name resolved from Users; falls back to the raw user id.
/// </summary>
public sealed record GroupMemberDto(
    Guid UserId,
    string DisplayName);
