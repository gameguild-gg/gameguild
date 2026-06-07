using FluentAssertions;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit;

public sealed class RetiredUserProfilesModuleTests
{
    [Fact]
    public void UserProfilesMicroserviceScope_IsConsolidatedIntoIdentityUsersModule()
    {
        typeof(UserProfile).Namespace.Should().Be("GameGuild.Identity.Users");
    }
}
