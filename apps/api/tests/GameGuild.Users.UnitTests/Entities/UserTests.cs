using FluentAssertions;
using GameGuild.Users.Entities;
using Xunit;

namespace GameGuild.Users.UnitTests.Entities;

/// <summary>
/// Unit tests for User entity
/// </summary>
public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var name = "Test User";
        var phoneNumber = "+1234567890";

        // Act
        var user = User.Create(email, name, phoneNumber);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.PhoneNumber.Should().Be(phoneNumber);
        user.IsActive.Should().BeTrue();
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithoutPhoneNumber_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var user = User.Create(email, name);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.PhoneNumber.Should().BeNull();
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidEmail_ShouldThrowException(string invalidEmail)
    {
        // Arrange
        var name = "Test User";

        // Act & Assert
        var action = () => User.Create(invalidEmail, name);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidName_ShouldThrowException(string invalidName)
    {
        // Arrange
        var email = "test@example.com";

        // Act & Assert
        var action = () => User.Create(email, invalidName);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldActivateUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        user.Deactivate();
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Fact]
    public void UpdateInfo_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        var newName = "Updated User";
        var newPhoneNumber = "+9876543210";
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.UpdateInfo(newName, newPhoneNumber);

        // Assert
        user.Name.Should().Be(newName);
        user.PhoneNumber.Should().Be(newPhoneNumber);
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateInfo_WithInvalidName_ShouldThrowException(string invalidName)
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");

        // Act & Assert
        var action = () => user.UpdateInfo(invalidName);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        var newName = "Updated Name";
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.UpdateName(newName);

        // Assert
        user.Name.Should().Be(newName);
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateName_WithInvalidName_ShouldThrowException(string invalidName)
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");

        // Act & Assert
        var action = () => user.UpdateName(invalidName);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdatePhoneNumber_WithValidNumber_ShouldUpdatePhoneNumber()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        var newPhoneNumber = "+9876543210";
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.UpdatePhoneNumber(newPhoneNumber);

        // Assert
        user.PhoneNumber.Should().Be(newPhoneNumber);
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Fact]
    public void UpdatePhoneNumber_WithNull_ShouldClearPhoneNumber()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "+1234567890");
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.UpdatePhoneNumber(null);

        // Assert
        user.PhoneNumber.Should().BeNull();
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Fact]
    public void RecordActivity_ShouldUpdateLastSeenAt()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        var previousUpdateTime = user.UpdatedAt;
        var previousLastSeen = user.LastSeenAt;

        // Act
        Thread.Sleep(10); // Ensure timestamps change
        user.RecordActivity();

        // Assert
        user.LastSeenAt.Should().NotBeNull();
        user.LastSeenAt.Should().BeAfter(previousLastSeen ?? DateTime.MinValue);
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Fact]
    public void Touch_ShouldUpdateTimestamp()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        var previousUpdateTime = user.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure UpdatedAt changes
        user.Touch();

        // Assert
        user.UpdatedAt.Should().BeAfter(previousUpdateTime);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");

        // Act
        user.SoftDelete();

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        user.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Restore_WhenDeleted_ShouldRestoreUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User");
        user.SoftDelete();

        // Act
        user.Restore();

        // Assert
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void IsNew_ForNewUser_ShouldReturnTrue()
    {
        // Arrange & Act
        var user = User.Create("test@example.com", "Test User");

        // Assert
        user.IsNew.Should().BeTrue();
        user.Version.Should().Be(0);
    }
}
