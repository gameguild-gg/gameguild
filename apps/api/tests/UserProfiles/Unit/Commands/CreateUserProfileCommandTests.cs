using FluentAssertions;
using GameGuild.Modules.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Commands;

/// <summary>
/// Unit tests for CreateUserProfileCommand
/// </summary>
public class CreateUserProfileCommandTests
{
    [Fact]
    public void Command_Should_Have_Required_Properties()
    {
        // Arrange
        var displayName = "Test User";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var command = new CreateUserProfileCommand
        {
            DisplayName = displayName,
            UserId = userId,
            TenantId = tenantId
        };

        // Assert
        command.DisplayName.Should().Be(displayName);
        command.UserId.Should().Be(userId);
        command.TenantId.Should().Be(tenantId);
    }
}
