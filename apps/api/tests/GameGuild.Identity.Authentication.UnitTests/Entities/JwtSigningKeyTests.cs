using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class JwtSigningKeyTests
{
    [Fact]
    public void CreateNew_ReturnsValidKey()
    {
        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow, TimeSpan.FromDays(90));

        key.Should().NotBeNull();
        key.KeyVersion.Should().Be(1);
        key.IsActive.Should().BeFalse();
        key.KeyId.Should().StartWith("key-");
        key.KeyMaterial.Should().NotBeNullOrEmpty();
        key.Algorithm.Should().Be("HS256");
    }

    [Fact]
    public void CreateNew_SetsValidFromAndExpiry()
    {
        var validFrom = DateTime.UtcNow;
        var key = JwtSigningKey.CreateNew(1, validFrom, TimeSpan.FromDays(90));

        key.ValidFrom.Should().Be(validFrom);
        key.ExpiresAt.Should().Be(validFrom.Add(TimeSpan.FromDays(90)));
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow, TimeSpan.FromDays(90));
        key.Activate();
        key.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Rotate_DeactivatesAndSetsReason()
    {
        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow, TimeSpan.FromDays(90));
        key.Activate();
        key.Rotate("scheduled rotation");

        key.IsActive.Should().BeFalse();
        key.RotatedAt.Should().NotBeNull();
        key.RotationReason.Should().Be("scheduled rotation");
    }

    [Fact]
    public void IsValidForValidation_CurrentTimeInRange_ReturnsTrue()
    {
        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow.AddDays(-1), TimeSpan.FromDays(90));
        key.IsValidForValidation(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsValidForValidation_BeforeValidFrom_ReturnsFalse()
    {
        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow.AddDays(1), TimeSpan.FromDays(90));
        key.IsValidForValidation(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsValidForValidation_AfterExpiry_ReturnsFalse()
    {
        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow.AddDays(-100), TimeSpan.FromDays(30));
        key.IsValidForValidation(DateTime.UtcNow).Should().BeFalse();
    }
}
