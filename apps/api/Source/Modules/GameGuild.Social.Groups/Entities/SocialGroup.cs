namespace GameGuild.Social.Groups;

public class SocialGroup : EntityBase
{
    public new Guid? TenantId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public SocialGroupType Type { get; private set; }
    public SocialGroupVisibility Visibility { get; private set; }
    public SocialGroupStatus Status { get; private set; }
    public int MemberCount { get; private set; }
    public int PendingMemberCount { get; private set; }

    private SocialGroup()
    {
    }

    public static SocialGroup Create(
        Guid ownerId,
        string name,
        string slug,
        SocialGroupType type,
        SocialGroupVisibility visibility,
        string? description = null,
        Guid? tenantId = null)
    {
        return new SocialGroup
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            TenantId = tenantId,
            Name = NormalizeRequired(name, nameof(name), 120),
            Slug = NormalizeRequired(slug, nameof(slug), 160).ToLowerInvariant(),
            Description = NormalizeOptional(description, 1000),
            Type = type,
            Visibility = visibility,
            Status = SocialGroupStatus.Active,
            MemberCount = 1,
            PendingMemberCount = 0
        };
    }

    public void UpdateDetails(string name, string slug, string? description, SocialGroupType type, SocialGroupVisibility visibility)
    {
        Name = NormalizeRequired(name, nameof(name), 120);
        Slug = NormalizeRequired(slug, nameof(slug), 160).ToLowerInvariant();
        Description = NormalizeOptional(description, 1000);
        Type = type;
        Visibility = visibility;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Activate()
    {
        Status = SocialGroupStatus.Active;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Archive()
    {
        Status = SocialGroupStatus.Archived;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Suspend()
    {
        Status = SocialGroupStatus.Suspended;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void RecordMembershipActivated()
    {
        MemberCount++;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void RecordMembershipRequested()
    {
        PendingMemberCount++;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void RecordMembershipApproved()
    {
        if (PendingMemberCount > 0)
        {
            PendingMemberCount--;
        }

        MemberCount++;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void RecordMembershipRejected()
    {
        if (PendingMemberCount > 0)
        {
            PendingMemberCount--;
        }

        UpdatedAt = SystemClock.UtcNow;
    }

    public void RecordMembershipRemoved(SocialGroupMembershipStatus previousStatus)
    {
        if (previousStatus == SocialGroupMembershipStatus.Active && MemberCount > 0)
        {
            MemberCount--;
        }

        if (previousStatus == SocialGroupMembershipStatus.Pending && PendingMemberCount > 0)
        {
            PendingMemberCount--;
        }

        UpdatedAt = SystemClock.UtcNow;
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

public class SocialGroupMember : EntityBase
{
    public Guid GroupId { get; private set; }
    public Guid UserId { get; private set; }
    public SocialGroupMemberRole Role { get; private set; }
    public SocialGroupMembershipStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? JoinedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? RemovedAt { get; private set; }

    private SocialGroupMember()
    {
    }

    public static SocialGroupMember CreateOwner(Guid groupId, Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = SocialGroupMemberRole.Owner,
            Status = SocialGroupMembershipStatus.Active,
            RequestedAt = SystemClock.UtcNow,
            JoinedAt = SystemClock.UtcNow
        };

    public static SocialGroupMember Request(Guid groupId, Guid userId, SocialGroupMemberRole requestedRole, bool approveImmediately)
        => new()
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = requestedRole == SocialGroupMemberRole.Owner ? SocialGroupMemberRole.Member : requestedRole,
            Status = approveImmediately ? SocialGroupMembershipStatus.Active : SocialGroupMembershipStatus.Pending,
            RequestedAt = SystemClock.UtcNow,
            JoinedAt = approveImmediately ? SystemClock.UtcNow : null
        };

    public void RequestAgain(SocialGroupMemberRole requestedRole, bool approveImmediately)
    {
        Role = requestedRole == SocialGroupMemberRole.Owner ? SocialGroupMemberRole.Member : requestedRole;
        Status = approveImmediately ? SocialGroupMembershipStatus.Active : SocialGroupMembershipStatus.Pending;
        RequestedAt = SystemClock.UtcNow;
        JoinedAt = approveImmediately ? SystemClock.UtcNow : null;
        ApprovedByUserId = null;
        RemovedAt = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Approve(Guid approvedByUserId)
    {
        Status = SocialGroupMembershipStatus.Active;
        ApprovedByUserId = approvedByUserId;
        JoinedAt ??= SystemClock.UtcNow;
        RemovedAt = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Reject()
    {
        Status = SocialGroupMembershipStatus.Rejected;
        RemovedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void ChangeRole(SocialGroupMemberRole role)
    {
        if (role == SocialGroupMemberRole.Owner)
        {
            throw new ArgumentException("Use ownership transfer flow for owner role.", nameof(role));
        }

        Role = role;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Remove()
    {
        Status = SocialGroupMembershipStatus.Removed;
        RemovedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }
}

public enum SocialGroupType
{
    StudyGroup,
    ProjectTeam,
    InterestCommunity,
    CourseCohort,
    Institution,
    GameJamTeam
}

public enum SocialGroupVisibility
{
    Public,
    Private,
    InviteOnly
}

public enum SocialGroupStatus
{
    Active,
    Archived,
    Suspended
}

public enum SocialGroupMemberRole
{
    Owner,
    Admin,
    Moderator,
    Member
}

public enum SocialGroupMembershipStatus
{
    Pending,
    Active,
    Rejected,
    Removed
}
