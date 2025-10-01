using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit;

/// <summary>
/// Unit tests for the User entity
/// </summary>
public class UserTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.IsActive.Should().BeTrue();
        user.Username.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.EmailAddress.Should().BeNull();
        user.GivenName.Should().BeNull();
        user.FamilyName.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.Credentials.Should().BeEmpty();
        user.IsNew.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_With_Partial_Should_Initialize_Correctly()
    {
        // Arrange
        var partial = new { Username = "testuser", GivenName = "John" };

        // Act
        var user = new User(partial);

        // Assert
        user.Username.Should().Be("testuser");
        user.GivenName.Should().Be("John");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Email_Property_Should_Map_To_EmailAddress_Value()
    {
        // Arrange
        var user = new User();
        const string email = "test@example.com";

        // Act
        user.Email = email;

        // Assert
        user.Email.Should().Be(email);
        user.EmailAddress.Should().NotBeNull();
        user.EmailAddress!.Value.Should().Be(email);
    }

    [Fact]
    public void Email_Property_Should_Handle_Null_Value()
    {
        // Arrange
        var user = new User();

        // Act
        user.Email = null!;

        // Assert
        user.Email.Should().BeEmpty();
        user.EmailAddress.Should().BeNull();
    }

    [Fact]
    public void Email_Property_Should_Handle_Empty_String()
    {
        // Arrange
        var user = new User();

        // Act
        user.Email = string.Empty;

        // Assert
        user.Email.Should().BeEmpty();
        user.EmailAddress.Should().BeNull();
    }

    [Fact]
    public void Activate_Should_Set_IsActive_To_True_And_Update_Timestamp()
    {
        // Arrange
        var user = new User { IsActive = false };
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_To_False_And_Update_Timestamp()
    {
        // Arrange
        var user = new User { IsActive = true };
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_Should_Update_Names_And_PhoneNumber()
    {
        // Arrange
        var user = new User();
        const string givenName = "John";
        const string familyName = "Doe";
        const string phoneNumber = "+1234567890";
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        user.Update(givenName, familyName, phoneNumber);

        // Assert
        user.GivenName.Should().Be(givenName);
        user.FamilyName.Should().Be(familyName);
        user.PhoneNumber.Should().NotBeNull();
        user.PhoneNumber!.Value.Should().Be(phoneNumber);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_Should_Handle_Null_PhoneNumber()
    {
        // Arrange
        var user = new User();
        const string givenName = "John";
        const string familyName = "Doe";

        // Act
        user.Update(givenName, familyName, null);

        // Assert
        user.GivenName.Should().Be(givenName);
        user.FamilyName.Should().Be(familyName);
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Update_Should_Handle_Empty_PhoneNumber()
    {
        // Arrange
        var user = new User();
        const string givenName = "John";
        const string familyName = "Doe";

        // Act
        user.Update(givenName, familyName, string.Empty);

        // Assert
        user.GivenName.Should().Be(givenName);
        user.FamilyName.Should().Be(familyName);
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void RecordActivity_Should_Update_UpdatedAt_Timestamp()
    {
        // Arrange
        var user = new User();
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        user.RecordActivity();

        // Assert
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Create_Should_Create_User_With_Valid_Parameters()
    {
        // Arrange
        const string email = "test@example.com";
        const string givenName = "John";
        const string familyName = "Doe";
        const string username = "johndoe";
        const string phoneNumber = "+1234567890";

        // Act
        var user = User.Create(email, givenName, familyName, username, phoneNumber);

        // Assert
        user.Email.Should().Be(email);
        user.GivenName.Should().Be(givenName);
        user.FamilyName.Should().Be(familyName);
        user.Username.Should().Be(username);
        user.PhoneNumber.Should().NotBeNull();
        user.PhoneNumber!.Value.Should().Be(phoneNumber);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Create_User_Without_PhoneNumber()
    {
        // Arrange
        const string email = "test@example.com";
        const string givenName = "John";
        const string familyName = "Doe";
        const string username = "johndoe";

        // Act
        var user = User.Create(email, givenName, familyName, username);

        // Assert
        user.Email.Should().Be(email);
        user.GivenName.Should().Be(givenName);
        user.FamilyName.Should().Be(familyName);
        user.Username.Should().Be(username);
        user.PhoneNumber.Should().BeNull();
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_Should_Throw_When_Email_Is_Invalid(string? email)
    {
        // Arrange
        const string givenName = "John";
        const string familyName = "Doe";
        const string username = "johndoe";

        // Act & Assert
        var action = () => User.Create(email!, givenName, familyName, username);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_Should_Throw_When_Username_Is_Invalid(string? username)
    {
        // Arrange
        const string email = "test@example.com";
        const string givenName = "John";
        const string familyName = "Doe";

        // Act & Assert
        var action = () => User.Create(email, givenName, familyName, username!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateNames_Should_Update_Names_And_Timestamp()
    {
        // Arrange
        var user = new User();
        const string givenName = "Jane";
        const string familyName = "Smith";
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        user.UpdateNames(givenName, familyName);

        // Assert
        user.GivenName.Should().Be(givenName);
        user.FamilyName.Should().Be(familyName);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateNames_Should_Handle_Null_Names()
    {
        // Arrange
        var user = new User { GivenName = "John", FamilyName = "Doe" };

        // Act
        user.UpdateNames(null, null);

        // Assert
        user.GivenName.Should().BeNull();
        user.FamilyName.Should().BeNull();
    }

    [Fact]
    public void UpdatePhoneNumber_Should_Update_PhoneNumber_And_Timestamp()
    {
        // Arrange
        var user = new User();
        const string phoneNumber = "+1234567890";
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        user.UpdatePhoneNumber(phoneNumber);

        // Assert
        user.PhoneNumber.Should().NotBeNull();
        user.PhoneNumber!.Value.Should().Be(phoneNumber);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdatePhoneNumber_Should_Handle_Null_PhoneNumber()
    {
        // Arrange
        var user = new User();
        user.UpdatePhoneNumber("+1234567890"); // Set initial value

        // Act
        user.UpdatePhoneNumber(null);

        // Assert
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void UpdatePhoneNumber_Should_Handle_Empty_PhoneNumber()
    {
        // Arrange
        var user = new User();
        user.UpdatePhoneNumber("+1234567890"); // Set initial value

        // Act
        user.UpdatePhoneNumber(string.Empty);

        // Assert
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void UpdatePhoneNumber_Should_Handle_Whitespace_PhoneNumber()
    {
        // Arrange
        var user = new User();
        user.UpdatePhoneNumber("+1234567890"); // Set initial value

        // Act
        user.UpdatePhoneNumber("   ");

        // Assert
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void User_Should_Inherit_From_EntityBase()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void User_Should_Implement_IUser_Interface()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Should().BeAssignableTo<IUser>();
    }

    [Fact]
    public void User_Should_Have_Credentials_Collection_Initialized()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Credentials.Should().NotBeNull();
        user.Credentials.Should().BeEmpty();
    }
}