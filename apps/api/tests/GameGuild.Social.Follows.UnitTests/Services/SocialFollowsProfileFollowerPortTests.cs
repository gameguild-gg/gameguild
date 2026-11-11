using FluentAssertions;
using GameGuild.Social.Follows;
using GameGuild.Social.Follows.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Social.Follows.Tests.Services;

/// <summary>
///     Issue #1315 SP1: the Social.Profiles follower-port adapter resolves the explicit
///     follow entitlement — only User-type follows of the profile's user count, and
///     soft-deleted follow rows never grant Private-profile access.
/// </summary>
public class SocialFollowsProfileFollowerPortTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private void SetupFollowDbSet(List<Follow> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<Follow>()).Returns(mock.Object);
    }

    [Fact]
    public async Task IsFollowerAsync_WhenActiveUserFollowExists_ReturnsTrue()
    {
        var followerId = Guid.NewGuid();
        var profileUserId = Guid.NewGuid();
        SetupFollowDbSet([
            Follow.Create(followerId, profileUserId, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), profileUserId, FollowableEntityTypes.User)
        ]);
        var port = new SocialFollowsProfileFollowerPort(_contextMock.Object);

        var isFollower = await port.IsFollowerAsync(followerId, profileUserId);

        isFollower.Should().BeTrue();
    }

    [Fact]
    public async Task IsFollowerAsync_WhenNoFollowExists_ReturnsFalse()
    {
        var followerId = Guid.NewGuid();
        SetupFollowDbSet([]);
        var port = new SocialFollowsProfileFollowerPort(_contextMock.Object);

        var isFollower = await port.IsFollowerAsync(followerId, Guid.NewGuid());

        isFollower.Should().BeFalse();
    }

    [Fact]
    public async Task IsFollowerAsync_WhenFollowTargetsAnotherEntityType_ReturnsFalse()
    {
        var followerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        SetupFollowDbSet([Follow.Create(followerId, entityId, "Course")]);
        var port = new SocialFollowsProfileFollowerPort(_contextMock.Object);

        var isFollower = await port.IsFollowerAsync(followerId, entityId);

        isFollower.Should().BeFalse();
    }

    [Fact]
    public async Task IsFollowerAsync_WhenFollowRowIsSoftDeleted_ReturnsFalse()
    {
        var followerId = Guid.NewGuid();
        var profileUserId = Guid.NewGuid();
        var follow = Follow.Create(followerId, profileUserId, FollowableEntityTypes.User);
        follow.SoftDelete();
        SetupFollowDbSet([follow]);
        var port = new SocialFollowsProfileFollowerPort(_contextMock.Object);

        var isFollower = await port.IsFollowerAsync(followerId, profileUserId);

        isFollower.Should().BeFalse();
    }
}
