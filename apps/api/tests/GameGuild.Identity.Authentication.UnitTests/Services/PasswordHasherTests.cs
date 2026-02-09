using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
/// Unit tests for PasswordHasher service
/// </summary>
public class PasswordHasherTests
{
    private readonly Mock<ILogger<PasswordHasher>> _loggerMock;
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _loggerMock = new Mock<ILogger<PasswordHasher>>();
        var configurationMock = new Mock<IConfiguration>();
        _passwordHasher = new PasswordHasher(_loggerMock.Object, configurationMock.Object);
    }

    [Fact]
    public async Task HashPassword_WithValidPassword_ShouldReturnHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash = await _passwordHasher.HashPasswordAsync(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Should().StartWith("$2"); // BCrypt hash prefix
    }

    [Fact]
    public async Task HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash1 = await _passwordHasher.HashPasswordAsync(password);
        var hash2 = await _passwordHasher.HashPasswordAsync(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HashPassword_WithInvalidPassword_ShouldThrowException(string invalidPassword)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _passwordHasher.HashPasswordAsync(invalidPassword));
    }

    [Fact]
    public async Task VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = await _passwordHasher.HashPasswordAsync(password);

        // Act
        var result = await _passwordHasher.VerifyPasswordAsync(hash, password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = await _passwordHasher.HashPasswordAsync(password);

        // Act
        var result = await _passwordHasher.VerifyPasswordAsync(hash, wrongPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task VerifyPassword_WithInvalidPassword_ShouldReturnFalse(string invalidPassword)
    {
        // Arrange
        var hash = await _passwordHasher.HashPasswordAsync("ValidPassword123!");

        // Act
        var result = await _passwordHasher.VerifyPasswordAsync(hash, invalidPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-hash")]
    public async Task VerifyPassword_WithInvalidHash_ShouldReturnFalse(string invalidHash)
    {
        // Arrange
        var password = "ValidPassword123!";

        // Act
        var result = await _passwordHasher.VerifyPasswordAsync(invalidHash, password);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HashPassword_MultipleTimes_ShouldProduceDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashes = new List<string>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            hashes.Add(await _passwordHasher.HashPasswordAsync(password));
        }

        // Assert
        hashes.Should().OnlyHaveUniqueItems();
        
        foreach (var hash in hashes)
        {
            var isValid = await _passwordHasher.VerifyPasswordAsync(hash, password);
            isValid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task VerifyPassword_WithModifiedHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = await _passwordHasher.HashPasswordAsync(password);
        var modifiedHash = hash.Substring(0, hash.Length - 1) + "X"; // Modify last character

        // Act
        var result = await _passwordHasher.VerifyPasswordAsync(modifiedHash, password);

        // Assert
        result.Should().BeFalse();
    }
}
