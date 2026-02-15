using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests;

/// <summary>
///     R4 supplemental tests: PasswordHasher methods and AuthenticationModule DI.
/// </summary>
public class PasswordHasherAndModuleTests
{
    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection().Build();

    // ═══════════════════════════════════════════════════════════════════
    // PasswordHasher sync methods
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void PasswordHasher_HashPassword_ReturnsHash()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = hasher.HashPassword("TestPassword123!");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = hasher.HashPassword("TestPassword123!");
        hasher.VerifyPassword(hash, "TestPassword123!").Should().BeTrue();
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = hasher.HashPassword("TestPassword123!");
        hasher.VerifyPassword(hash, "WrongPassword456!").Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_NeedsUpgrade_ReturnsBool()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = hasher.HashPassword("TestPassword123!");
        // Current hash should not need upgrade
        hasher.NeedsUpgrade(hash).Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_ValidatePasswordStrength_StrongPassword()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var result = hasher.ValidatePasswordStrength("C0mpl3xP@ssw0rd!Str0ng");
        result.Should().NotBeNull();
    }

    [Fact]
    public void PasswordHasher_ValidatePasswordStrength_WeakPassword()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var result = hasher.ValidatePasswordStrength("abc");
        result.Should().NotBeNull();
    }

    [Fact]
    public void PasswordHasher_HashPassword_NullOrEmpty_Throws()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var act = () => hasher.HashPassword(null!);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void PasswordHasher_HashPassword_EmptyString_Throws()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var act = () => hasher.HashPassword("");
        act.Should().Throw<Exception>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // PasswordHasher async methods
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PasswordHasher_HashPasswordAsync_ReturnsHash()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = await hasher.HashPasswordAsync("TestPassword123!", CancellationToken.None);
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PasswordHasher_VerifyPasswordAsync_Correct_ReturnsTrue()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = await hasher.HashPasswordAsync("TestPassword123!", CancellationToken.None);
        var result = await hasher.VerifyPasswordAsync(hash, "TestPassword123!", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PasswordHasher_VerifyPasswordAsync_Wrong_ReturnsFalse()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = await hasher.HashPasswordAsync("TestPassword123!", CancellationToken.None);
        var result = await hasher.VerifyPasswordAsync(hash, "WrongPassword!", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordHasher_NeedsRehashAsync_ReturnsBool()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var hash = await hasher.HashPasswordAsync("TestPassword123!", CancellationToken.None);
        var result = await hasher.NeedsRehashAsync(hash, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordHasher_ValidatePasswordStrengthAsync_ReturnsResult()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        var result = await hasher.ValidatePasswordStrengthAsync("StR0ng!P@ss123", CancellationToken.None);
        result.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // PasswordHasher NeedsUpgrade with old format
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void PasswordHasher_NeedsUpgrade_InvalidFormat_ReturnsTrue()
    {
        var hasher = new PasswordHasher(
            Mock.Of<ILogger<PasswordHasher>>(),
            EmptyConfig());
        // An invalid/old format hash should need upgrade
        hasher.NeedsUpgrade("not-a-valid-hash-format").Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // AuthenticationModule DI
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AuthenticationModule_AddAuthenticationModule_RegistersServices()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestKeyThatIsAtLeast32BytesLongForHmacSha256!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        services.AddAuthenticationModule(config);
        services.Count.Should().BeGreaterThan(0);
    }
}
