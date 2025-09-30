using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.ValueObjects;

/// <summary>
/// Unit tests for EmailAddress value object
/// </summary>
public class EmailAddressTests
{
    [Fact]
    public void Constructor_Should_Create_Valid_EmailAddress()
    {
        // Arrange
        const string email = "test@example.com";

        // Act
        EmailAddress emailAddress = new(email);

        // Assert
        _ = emailAddress.Value.Should().Be(email);
    }

    [Fact]
    public void Constructor_Should_Normalize_Email_To_Lowercase()
    {
        // Arrange
        const string email = "TEST@EXAMPLE.COM";

        // Act
        EmailAddress emailAddress = new(email);

        // Assert
        _ = emailAddress.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Constructor_Should_Trim_Whitespace()
    {
        // Arrange
        const string email = "  test@example.com  ";

        // Act
        EmailAddress emailAddress = new(email);

        // Assert
        _ = emailAddress.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_Email_Is_Null_Or_Empty(string? email)
    {
        // Act & Assert
        Action act = () => new EmailAddress(email!);
        _ = act.Should().Throw<ArgumentException>()
            .WithParameterName("email")
            .WithMessage("*cannot be null or empty*");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    [InlineData("user name@example.com")]
    [InlineData("user@example")]
    public void Constructor_Should_Throw_When_Email_Format_Is_Invalid(string email)
    {
        // Act & Assert
        Action act = () => new EmailAddress(email);
        _ = act.Should().Throw<ArgumentException>()
            .WithParameterName("email")
            .WithMessage("*Invalid email address format*");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+label@example.com")]
    [InlineData("user123@example123.com")]
    [InlineData("a@b.co")]
    public void Constructor_Should_Accept_Valid_Email_Formats(string email)
    {
        // Act & Assert
        Action act = () => new EmailAddress(email);
        _ = act.Should().NotThrow();
    }

    [Fact]
    public void Implicit_Conversion_To_String_Should_Return_Value()
    {
        // Arrange
        EmailAddress emailAddress = new("test@example.com");

        // Act
        string emailString = emailAddress;

        // Assert
        _ = emailString.Should().Be("test@example.com");
    }

    [Fact]
    public void Implicit_Conversion_From_String_Should_Create_EmailAddress()
    {
        // Arrange & Act
        EmailAddress emailAddress = "test@example.com";

        // Assert
        _ = emailAddress.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void ToString_Should_Return_Email_Value()
    {
        // Arrange
        EmailAddress emailAddress = new("test@example.com");

        // Act
        string result = emailAddress.ToString();

        // Assert
        _ = result.Should().Be("test@example.com");
    }

    [Fact]
    public void Equality_Should_Work_Correctly_For_Records()
    {
        // Arrange
        EmailAddress email1 = new("test@example.com");
        EmailAddress email2 = new("test@example.com");
        EmailAddress email3 = new("different@example.com");

        // Act & Assert
        _ = email1.Should().Be(email2);
        _ = email1.Should().NotBe(email3);
        _ = (email1 == email2).Should().BeTrue();
        _ = (email1 == email3).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Be_Same_For_Equal_EmailAddresses()
    {
        // Arrange
        EmailAddress email1 = new("test@example.com");
        EmailAddress email2 = new("test@example.com");

        // Act & Assert
        _ = email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void With_Expression_Should_Create_New_Instance()
    {
        // Arrange
        EmailAddress originalEmail = new("test@example.com");

        // Act
        EmailAddress newEmail = originalEmail with { Value = "new@example.com" };

        // Assert
        _ = newEmail.Value.Should().Be("new@example.com");
        _ = originalEmail.Value.Should().Be("test@example.com");
        _ = newEmail.Should().NotBe(originalEmail);
    }
}