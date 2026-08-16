
namespace GameGuild.Learning.Assessments;

/// <summary>
/// Represents a named collection of course groups used for group assessments.
/// </summary>
public class CourseGroupSet : EntityBase
{
    public Guid CourseId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private CourseGroupSet() { } // EF Core

    public static CourseGroupSet Create(Guid courseId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        return new CourseGroupSet
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Name = name.Trim()
        };
    }
}

/// <summary>
/// Represents a single group within a course group set, with a member capacity.
/// </summary>
public class CourseGroup : EntityBase
{
    public Guid GroupSetId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }

    private CourseGroup() { } // EF Core

    public static CourseGroup Create(Guid groupSetId, string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (capacity < 2)
        {
            throw new ArgumentException("Capacity must be at least two.", nameof(capacity));
        }

        return new CourseGroup
        {
            Id = Guid.NewGuid(),
            GroupSetId = groupSetId,
            Name = name.Trim(),
            Capacity = capacity
        };
    }
}

/// <summary>
/// Represents a student's membership in a course group.
/// </summary>
public class CourseGroupMember : EntityBase
{
    public Guid GroupId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private CourseGroupMember() { } // EF Core

    public static CourseGroupMember Create(Guid groupId, Guid userId)
    {
        return new CourseGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            JoinedAt = SystemClock.UtcNow
        };
    }
}
