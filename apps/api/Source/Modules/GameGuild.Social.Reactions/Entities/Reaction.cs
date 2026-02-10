
namespace GameGuild.Social.Reactions;

/// <summary>
/// Represents a reaction to content (like, love, etc.)
/// </summary>
public class Reaction : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid TargetId { get; private set; }
    public ReactionTargetType TargetType { get; private set; }
    public ReactionType Type { get; private set; }

    private Reaction() { } // EF Core

    public static Reaction Create(Guid userId, Guid targetId, ReactionTargetType targetType, ReactionType type)
    {
        return new Reaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetId = targetId,
            TargetType = targetType,
            Type = type
        };
    }

    public void ChangeType(ReactionType newType)
    {
        Type = newType;
        UpdatedAt = SystemClock.UtcNow;
    }
}

public enum ReactionType
{
    Like,
    Love,
    Insightful,
    Celebrate,
    Support,
    Curious
}

public enum ReactionTargetType
{
    Post,
    Comment,
    BlogPost,
    CourseReview,
    Discussion,
    Reply
}
