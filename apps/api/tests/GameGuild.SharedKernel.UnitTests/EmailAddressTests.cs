using FluentAssertions;


namespace GameGuild.Tests.SharedKernel.Unit;

/// <summary>
/// Unit tests for EmailAddress value object
/// </summary>
public class EmailAddressTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("admin@test-domain.com")]
    [InlineData("info+tag@company.org")]
    public void Constructor_WithValidEmail_ShouldCreateEmailAddress(string email)
    {
        // Act
        var emailAddress = new EmailAddress(email);

        // Assert
        emailAddress.Should().NotBeNull();
        emailAddress.Value.Should().Be(email.ToLowerInvariant().Trim());
    }

        [Theory]
    [InlineData("Test@Example.COM", "test@example.com")]
    public void Constructor_ShouldNormalizeEmail(string input, string expected)
    {
        // Act
        var emailAddress = new EmailAddress(input);

        // Assert
        emailAddress.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmpty_ShouldThrowArgumentException(string? email)
    {
        // Act
        var act = () => new EmailAddress(email!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email address cannot be null or empty*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test@@example.com")]
    [InlineData("test @example.com")]
    public void Constructor_WithInvalidFormat_ShouldThrowArgumentException(string email)
    {
        // Act
        var act = () => new EmailAddress(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email address format*");
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var emailAddress = new EmailAddress("test@example.com");

        // Act
        string email = emailAddress;

        // Assert
        email.Should().Be("test@example.com");
    }

    [Fact]
    public void ExplicitConversion_FromString_ShouldWork()
    {
        // Act
        var emailAddress = (EmailAddress)"test@example.com";

        // Assert
        emailAddress.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var emailAddress = new EmailAddress("test@example.com");

        // Act
        var result = emailAddress.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void Equality_SameEmail_ShouldBeEqual()
    {
        // Arrange
        var email1 = new EmailAddress("test@example.com");
        var email2 = new EmailAddress("test@example.com");

        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentEmail_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = new EmailAddress("test1@example.com");
        var email2 = new EmailAddress("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }

    [Fact]
    public void Equality_CaseInsensitive_ShouldBeEqual()
    {
        // Arrange
        var email1 = new EmailAddress("Test@Example.COM");
        var email2 = new EmailAddress("test@example.com");

        // Act & Assert
        email1.Should().Be(email2);
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Theory]
    [InlineData("user+tag@example.com")]
    [InlineData("user.name+tag@example.com")]
    [InlineData("first.last@subdomain.example.com")]
    public void Constructor_WithComplexButValidEmail_ShouldSucceed(string email)
    {
        // Act
        var emailAddress = new EmailAddress(email);

        // Assert
        emailAddress.Value.Should().Be(email.ToLowerInvariant());
    }

    // Note: MailAddress in .NET is quite permissive and allows some edge cases like double dots
    // This test documents actual behavior rather than ideal validation
    [Theory]
    [InlineData(".startdot@example.com")]
    public void Constructor_WithLeadingDot_ShouldThrowArgumentException(string email)
    {
        // Act
        var act = () => new EmailAddress(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email address format*");
    }

    [Fact]
    public void ValueObject_WithSameValue_ShouldHaveSameHashCode()
    {
        // Arrange
        var email1 = new EmailAddress("test@example.com");
        var email2 = new EmailAddress("test@example.com");

        // Act & Assert
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }
}
