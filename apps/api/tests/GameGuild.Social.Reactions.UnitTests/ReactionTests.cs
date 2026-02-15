using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Reactions.UnitTests;

public class ReactionTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var reaction = Reaction.Create(userId, targetId, ReactionTargetType.Post, ReactionType.Like);

        reaction.UserId.Should().Be(userId);
        reaction.TargetId.Should().Be(targetId);
        reaction.TargetType.Should().Be(ReactionTargetType.Post);
        reaction.Type.Should().Be(ReactionType.Like);
        reaction.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ChangeType_UpdatesType()
    {
        var reaction = Reaction.Create(Guid.NewGuid(), Guid.NewGuid(),
            ReactionTargetType.BlogPost, ReactionType.Like);

        reaction.ChangeType(ReactionType.Love);
        reaction.Type.Should().Be(ReactionType.Love);

        reaction.ChangeType(ReactionType.Insightful);
        reaction.Type.Should().Be(ReactionType.Insightful);
    }
}

public class ReactionEnumTests
{
    [Fact]
    public void ReactionType_AllValues()
    {
        var values = Enum.GetValues<ReactionType>();
        values.Should().Contain(ReactionType.Like);
        values.Should().Contain(ReactionType.Love);
        values.Should().Contain(ReactionType.Insightful);
        values.Should().Contain(ReactionType.Celebrate);
        values.Should().Contain(ReactionType.Support);
        values.Should().Contain(ReactionType.Curious);
    }

    [Fact]
    public void ReactionTargetType_AllValues()
    {
        var values = Enum.GetValues<ReactionTargetType>();
        values.Should().Contain(ReactionTargetType.Post);
        values.Should().Contain(ReactionTargetType.Comment);
        values.Should().Contain(ReactionTargetType.BlogPost);
        values.Should().Contain(ReactionTargetType.CourseReview);
        values.Should().Contain(ReactionTargetType.Discussion);
        values.Should().Contain(ReactionTargetType.Reply);
    }
}
