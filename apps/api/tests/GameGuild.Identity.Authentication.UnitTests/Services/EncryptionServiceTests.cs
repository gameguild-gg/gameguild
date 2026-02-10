using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Configuration;
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
        var configurationMock = new Mock<IConfiguration>();
        // Return null so fallback key is used in tests
        configurationMock.Setup(c => c["Encryption:Key"]).Returns((string?)null);
        _service = new EncryptionService(_loggerMock.Object, configurationMock.Object);
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

    // --- Synchronous Encrypt/Decrypt ---

    [Fact]
    public void Encrypt_ShouldReturnNonEmptyBase64()
    {
        var result = _service.Encrypt("Hello World");

        result.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(result);
        act.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_WithNullOrEmpty_ShouldThrow()
    {
        FluentActions.Invoking(() => _service.Encrypt(null!)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _service.Encrypt("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_ShouldRoundTrip()
    {
        var original = "round-trip test data!";
        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Decrypt_WithNullOrEmpty_ShouldThrow()
    {
        FluentActions.Invoking(() => _service.Decrypt(null!)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _service.Decrypt("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_WithInvalidData_ShouldThrow()
    {
        var act = () => _service.Decrypt(Convert.ToBase64String(new byte[10]));
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Encrypt_SameInput_ProducesDifferentCiphertext()
    {
        var a = _service.Encrypt("same");
        var b = _service.Encrypt("same");
        a.Should().NotBe(b);
    }

    // --- GenerateSecureRandomString ---

    [Fact]
    public void GenerateSecureRandomString_ShouldReturnCorrectLength()
    {
        _service.GenerateSecureRandomString(20).Should().HaveLength(20);
    }

    [Fact]
    public void GenerateSecureRandomString_ShouldContainOnlyAlphanumeric()
    {
        _service.GenerateSecureRandomString(100).Should().MatchRegex("^[A-Za-z0-9]+$");
    }

    [Fact]
    public void GenerateSecureRandomString_WithZeroOrNegative_ShouldThrow()
    {
        FluentActions.Invoking(() => _service.GenerateSecureRandomString(0))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _service.GenerateSecureRandomString(-5))
            .Should().Throw<ArgumentException>();
    }

    // --- GenerateSecureToken (sync) ---

    [Fact]
    public void GenerateSecureToken_ShouldReturnNonEmpty()
    {
        _service.GenerateSecureToken().Should().NotBeNullOrEmpty();
    }

    // --- GenerateSecureTokenAsync / ValidateSecureTokenAsync ---

    [Fact]
    public async Task GenerateSecureTokenAsync_ShouldReturnNonEmptyToken()
    {
        var token = await _service.GenerateSecureTokenAsync(32);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSecureTokenAsync_WithZeroOrNegative_ShouldThrow()
    {
        await FluentActions.Invoking(() => _service.GenerateSecureTokenAsync(0))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => _service.GenerateSecureTokenAsync(-1))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateSecureTokenAsync_TokensShouldBeDifferent()
    {
        var t1 = await _service.GenerateSecureTokenAsync(32);
        var t2 = await _service.GenerateSecureTokenAsync(32);
        t1.Should().NotBe(t2);
    }

    [Fact]
    public async Task ValidateSecureTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        var token = await _service.GenerateSecureTokenAsync(32);
        (await _service.ValidateSecureTokenAsync(token)).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSecureTokenAsync_WithNullOrEmpty_ShouldReturnFalse()
    {
        (await _service.ValidateSecureTokenAsync(null!)).Should().BeFalse();
        (await _service.ValidateSecureTokenAsync("")).Should().BeFalse();
        (await _service.ValidateSecureTokenAsync("   ")).Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSecureTokenAsync_WithShortToken_ShouldReturnFalse()
    {
        var shortToken = Convert.ToBase64String(new byte[8])
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        (await _service.ValidateSecureTokenAsync(shortToken)).Should().BeFalse();
    }
}
