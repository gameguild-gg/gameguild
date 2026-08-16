using FluentAssertions;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.API.UnitTests.Teams;

public sealed class TeamPermissionTests
{
    [Theory]
    [InlineData("team:read", "read", "Read Teams")]
    [InlineData("team:write", "write", "Write Teams")]
    [InlineData("team:admin", "admin", "Administer Teams")]
    public void BuiltInPermissions_ExposeExpectedContract(
        string key,
        string action,
        string description)
    {
        var permission = key switch
        {
            TeamPermission.Keys.Read => TeamPermission.Read,
            TeamPermission.Keys.Write => TeamPermission.Write,
            _ => TeamPermission.Admin
        };

        permission.Key.Should().Be(key);
        permission.Resource.Should().Be("team");
        permission.Action.Should().Be(action);
        permission.Scope.Should().BeNull();
        permission.Description.Should().Be(description);
    }
}
