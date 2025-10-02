using FluentAssertions;
using GameGuild.Modules.Authentication.Services;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the EncryptionOptions configuration
/// Tests the properties and validation behavior of encryption configuration
/// </summary>
public class EncryptionOptionsTests
{
    [Fact]
    public void EncryptionOptions_Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new EncryptionOptions();

        // Assert
        options.EncryptionKey.Should().BeEmpty();
        options.Salt.Should().Be("GameGuild_Salt_2024");
        options.KeyDerivationIterations.Should().Be(10000);
        options.BcryptWorkFactor.Should().Be(12);
    }

    [Fact]
    public void EncryptionOptions_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var options = new EncryptionOptions
        {
            EncryptionKey = "this-is-a-32-character-secret-key-minimum",
            Salt = "CustomSalt",
            KeyDerivationIterations = 50000,
            BcryptWorkFactor = 14
        };

        // Assert
        options.EncryptionKey.Should().Be("this-is-a-32-character-secret-key-minimum");
        options.Salt.Should().Be("CustomSalt");
        options.KeyDerivationIterations.Should().Be(50000);
        options.BcryptWorkFactor.Should().Be(14);
    }

    [Fact]
    public void EncryptionOptions_Validate_Should_Throw_When_EncryptionKey_Is_Empty()
    {
        // Arrange
        var options = new EncryptionOptions { EncryptionKey = string.Empty };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Encryption key is required");
    }

    [Fact]
    public void EncryptionOptions_Validate_Should_Throw_When_EncryptionKey_Is_Too_Short()
    {
        // Arrange
        var options = new EncryptionOptions { EncryptionKey = "short-key" };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Encryption key must be at least 32 characters long");
    }

    [Fact]
    public void EncryptionOptions_Validate_Should_Throw_When_KeyDerivationIterations_Is_Too_Low()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            EncryptionKey = "this-is-a-32-character-secret-key-minimum",
            KeyDerivationIterations = 500
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Key derivation iterations must be at least 1000");
    }

    [Fact]
    public void EncryptionOptions_Validate_Should_Throw_When_BcryptWorkFactor_Is_Below_Range()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            EncryptionKey = "this-is-a-32-character-secret-key-minimum",
            BcryptWorkFactor = 3
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("BCrypt work factor must be between 4 and 31");
    }

    [Fact]
    public void EncryptionOptions_Validate_Should_Throw_When_BcryptWorkFactor_Is_Above_Range()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            EncryptionKey = "this-is-a-32-character-secret-key-minimum",
            BcryptWorkFactor = 32
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("BCrypt work factor must be between 4 and 31");
    }

    [Fact]
    public void EncryptionOptions_Validate_Should_Pass_With_Valid_Configuration()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            EncryptionKey = "this-is-a-32-character-secret-key-minimum",
            KeyDerivationIterations = 10000,
            BcryptWorkFactor = 12
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
