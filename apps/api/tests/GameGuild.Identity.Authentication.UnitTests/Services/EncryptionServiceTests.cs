using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class EncryptionServiceTests
{
    private readonly Mock<ILogger<EncryptionService>> _loggerMock;
    private readonly EncryptionService _service;

    public EncryptionServiceTests()
    {
        _loggerMock = new Mock<ILogger<EncryptionService>>();
        _service = new EncryptionService(_loggerMock.Object);
    }

    [Fact]
    public async Task EncryptAsync_EncryptsPlaintext()
    {
        // Arrange
        var plaintext = "sensitive data";

        // Act
        var encrypted = await _service.EncryptAsync(plaintext);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plaintext);
    }

    [Fact]
    public async Task EncryptAsync_ThrowsException_WhenPlaintextIsEmpty()
    {
        // Arrange
        var plaintext = "";

        // Act
        var act = async () => await _service.EncryptAsync(plaintext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EncryptAsync_ProducesDifferentCiphertext_ForSamePlaintext()
    {
        // Arrange
        var plaintext = "same data";

        // Act
        var encrypted1 = await _service.EncryptAsync(plaintext);
        var encrypted2 = await _service.EncryptAsync(plaintext);

        // Assert - Different because of random nonce
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public async Task DecryptAsync_DecryptsEncryptedData()
    {
        // Arrange
        var plaintext = "secret message";
        var encrypted = await _service.EncryptAsync(plaintext);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public async Task DecryptAsync_ThrowsException_WhenCiphertextIsInvalid()
    {
        // Arrange
        var invalidCiphertext = "invalid-base64";

        // Act
        var act = async () => await _service.DecryptAsync(invalidCiphertext);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DecryptAsync_ThrowsException_WhenCiphertextIsEmpty()
    {
        // Arrange
        var ciphertext = "";

        // Act
        var act = async () => await _service.DecryptAsync(ciphertext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_PreservesData()
    {
        // Arrange
        var originalData = "Test data with special chars: @#$%^&*()";

        // Act
        var encrypted = await _service.EncryptAsync(originalData);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(originalData);
    }

    [Fact]
    public async Task EncryptAsync_HandlesLongText()
    {
        // Arrange
        var longText = new string('a', 10000);

        // Act
        var encrypted = await _service.EncryptAsync(longText);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(longText);
    }

    [Fact]
    public async Task EncryptAsync_HandlesUnicodeCharacters()
    {
        // Arrange
        var unicode = "Hello 世界 🌍";

        // Act
        var encrypted = await _service.EncryptAsync(unicode);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(unicode);
    }

    [Fact]
    public async Task EncryptAsync_ProducesBase64Output()
    {
        // Arrange
        var plaintext = "test";

        // Act
        var encrypted = await _service.EncryptAsync(plaintext);

        // Assert - Should be valid Base64
        var act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }
}
