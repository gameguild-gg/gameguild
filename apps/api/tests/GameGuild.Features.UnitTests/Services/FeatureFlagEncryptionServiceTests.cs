using FluentAssertions;

using Xunit;

namespace GameGuild.Features.UnitTests.Services;

public class FeatureFlagEncryptionServiceTests
{
    private readonly string _validEncryptionKey;
    private readonly FeatureFlagEncryptionService _service;

    public FeatureFlagEncryptionServiceTests()
    {
        // Generate a valid 256-bit key
        _validEncryptionKey = GenerateValidKey();
        _service = new FeatureFlagEncryptionService(_validEncryptionKey);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidKey_CreatesService()
    {
        // Act
        var service = new FeatureFlagEncryptionService(_validEncryptionKey);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FeatureFlagEncryptionService(null!));
    }

    [Fact]
    public void Constructor_EmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FeatureFlagEncryptionService(string.Empty));
    }

    [Fact]
    public void Constructor_InvalidKeyLength_ThrowsArgumentException()
    {
        // Arrange - 128-bit key instead of 256-bit
        var invalidKey = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FeatureFlagEncryptionService(invalidKey));
    }

    [Fact]
    public void Constructor_InvalidBase64_ThrowsFormatException()
    {
        // Arrange
        var invalidKey = "not-valid-base64!!!";

        // Act & Assert
        Assert.Throws<FormatException>(() => new FeatureFlagEncryptionService(invalidKey));
    }

    #endregion

    #region EncryptAsync Tests

    [Fact]
    public async Task EncryptAsync_ValidPlainText_ReturnsEncryptedText()
    {
        // Arrange
        var plainText = "my-secret-value";

        // Act
        var result = await _service.EncryptAsync(plainText);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("ENC:");
        result.Should().NotBe(plainText);
    }

    [Fact]
    public async Task EncryptAsync_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = await _service.EncryptAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptAsync_NullString_ReturnsNull()
    {
        // Act
        var result = await _service.EncryptAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EncryptAsync_AlreadyEncrypted_ReturnsSameValue()
    {
        // Arrange
        var plainText = "my-secret-value";
        var encrypted = await _service.EncryptAsync(plainText);

        // Act - try to encrypt again
        var result = await _service.EncryptAsync(encrypted);

        // Assert
        result.Should().Be(encrypted);
    }

    [Fact]
    public async Task EncryptAsync_SameTextTwice_ReturnsDifferentCipherText()
    {
        // Arrange
        var plainText = "my-secret-value";

        // Act
        var encrypted1 = await _service.EncryptAsync(plainText);
        var encrypted2 = await _service.EncryptAsync(plainText);

        // Assert - Different IV should produce different cipher text
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public async Task EncryptAsync_LongText_EncryptsSuccessfully()
    {
        // Arrange
        var longText = new string('x', 10000);

        // Act
        var result = await _service.EncryptAsync(longText);

        // Assert
        result.Should().StartWith("ENC:");
    }

    [Fact]
    public async Task EncryptAsync_SpecialCharacters_EncryptsSuccessfully()
    {
        // Arrange
        var specialText = "!@#$%^&*()_+-={}[]|:;<>?,./~`";

        // Act
        var result = await _service.EncryptAsync(specialText);

        // Assert
        result.Should().StartWith("ENC:");
    }

    [Fact]
    public async Task EncryptAsync_UnicodeCharacters_EncryptsSuccessfully()
    {
        // Arrange
        var unicodeText = "Hello 世界 🌍 Привет";

        // Act
        var result = await _service.EncryptAsync(unicodeText);

        // Assert
        result.Should().StartWith("ENC:");
    }

    #endregion

    #region DecryptAsync Tests

    [Fact]
    public async Task DecryptAsync_ValidEncryptedText_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "my-secret-value";
        var encrypted = await _service.EncryptAsync(plainText);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task DecryptAsync_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = await _service.DecryptAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DecryptAsync_NullString_ReturnsNull()
    {
        // Act
        var result = await _service.DecryptAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DecryptAsync_NotEncrypted_ReturnsSameValue()
    {
        // Arrange
        var plainText = "plain-text-value";

        // Act
        var result = await _service.DecryptAsync(plainText);

        // Assert
        result.Should().Be(plainText);
    }

    [Fact]
    public async Task DecryptAsync_LongText_DecryptsSuccessfully()
    {
        // Arrange
        var longText = new string('x', 10000);
        var encrypted = await _service.EncryptAsync(longText);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(longText);
    }

    [Fact]
    public async Task DecryptAsync_SpecialCharacters_DecryptsSuccessfully()
    {
        // Arrange
        var specialText = "!@#$%^&*()_+-={}[]|:;<>?,./~`";
        var encrypted = await _service.EncryptAsync(specialText);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(specialText);
    }

    [Fact]
    public async Task DecryptAsync_UnicodeCharacters_DecryptsSuccessfully()
    {
        // Arrange
        var unicodeText = "Hello 世界 🌍 Привет";
        var encrypted = await _service.EncryptAsync(unicodeText);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(unicodeText);
    }

    #endregion

    #region IsEncrypted Tests

    [Fact]
    public void IsEncrypted_EncryptedValue_ReturnsTrue()
    {
        // Arrange
        var encryptedValue = "ENC:somebase64value";

        // Act
        var result = _service.IsEncrypted(encryptedValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_PlainValue_ReturnsFalse()
    {
        // Arrange
        var plainValue = "plain-text-value";

        // Act
        var result = _service.IsEncrypted(plainValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_EmptyString_ReturnsFalse()
    {
        // Act
        var result = _service.IsEncrypted(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_NullString_ReturnsFalse()
    {
        // Act
        var result = _service.IsEncrypted(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_PartialPrefix_ReturnsFalse()
    {
        // Arrange
        var value = "EN:notencrypted";

        // Act
        var result = _service.IsEncrypted(value);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GenerateEncryptionKey Tests

    [Fact]
    public void GenerateEncryptionKey_ReturnsValidBase64()
    {
        // Act
        var key = _service.GenerateEncryptionKey();

        // Assert
        key.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(key); // Should not throw
        bytes.Should().NotBeNull();
    }

    [Fact]
    public void GenerateEncryptionKey_Returns256BitKey()
    {
        // Act
        var key = _service.GenerateEncryptionKey();
        var bytes = Convert.FromBase64String(key);

        // Assert
        bytes.Length.Should().Be(32); // 256 bits = 32 bytes
    }

    [Fact]
    public void GenerateEncryptionKey_GeneratesUniqueKeys()
    {
        // Act
        var key1 = _service.GenerateEncryptionKey();
        var key2 = _service.GenerateEncryptionKey();

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateEncryptionKey_CanBeUsedToCreateNewService()
    {
        // Act
        var newKey = _service.GenerateEncryptionKey();
        var newService = new FeatureFlagEncryptionService(newKey);

        // Assert
        newService.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_PreservesOriginalValue()
    {
        // Arrange
        var originalValue = "test-secret-value-123";

        // Act
        var encrypted = await _service.EncryptAsync(originalValue);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(originalValue);
    }

    [Fact]
    public async Task EncryptDecrypt_MultipleRoundTrips_WorksCorrectly()
    {
        // Arrange
        var originalValue = "test-value";

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var encrypted = await _service.EncryptAsync(originalValue);
            var decrypted = await _service.DecryptAsync(encrypted);
            decrypted.Should().Be(originalValue);
        }
    }

    [Fact]
    public async Task DifferentServices_SameKey_CanDecryptEachOther()
    {
        // Arrange
        var service1 = new FeatureFlagEncryptionService(_validEncryptionKey);
        var service2 = new FeatureFlagEncryptionService(_validEncryptionKey);
        var plainText = "shared-secret";

        // Act
        var encrypted = await service1.EncryptAsync(plainText);
        var decrypted = await service2.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task DifferentServices_DifferentKeys_CannotDecrypt()
    {
        // Arrange
        const string key = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";
        const string differentKey = "ICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8=";
        const string encrypted = "ENC:QEFCQ0RFRkdISUpLTE1OTy8RlbCrl4j9VALtCqlKhXs=";
        var service2 = new FeatureFlagEncryptionService(differentKey);
        var plainText = "secret";

        // Act
        var service1 = new FeatureFlagEncryptionService(key);
        var decryptedByOwner = await service1.DecryptAsync(encrypted);
        Func<Task> act = async () => await service2.DecryptAsync(encrypted);

        // Assert
        decryptedByOwner.Should().Be(plainText);
        await act.Should().ThrowAsync<System.Security.Cryptography.CryptographicException>();
    }

    #endregion

    #region Helper Methods

    private static string GenerateValidKey()
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    #endregion
}
