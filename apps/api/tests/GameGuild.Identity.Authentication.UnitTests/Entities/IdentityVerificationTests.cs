using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class IdentityVerificationTests
{
    private static IdentityVerification CreateVerification(
        string status = "Pending",
        DateTime? expiresAt = null)
    {
        return new IdentityVerification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            VerificationType = "email",
            Status = status,
            VerifiedValue = "test@example.com",
            InitiatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    [Fact]
    public void IsValid_ApprovedAndNotExpired_ReturnsTrue()
    {
        var v = CreateVerification(status: "Approved", expiresAt: DateTime.UtcNow.AddDays(30));
        v.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ApprovedWithNullExpiry_ReturnsTrue()
    {
        var v = CreateVerification(status: "Approved", expiresAt: null);
        v.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ApprovedButExpired_ReturnsFalse()
    {
        var v = CreateVerification(status: "Approved", expiresAt: DateTime.UtcNow.AddHours(-1));
        v.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_Pending_ReturnsFalse()
    {
        var v = CreateVerification(status: "Pending");
        v.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_Rejected_ReturnsFalse()
    {
        var v = CreateVerification(status: "Rejected");
        v.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsPending_PendingStatus_ReturnsTrue()
    {
        var v = CreateVerification(status: "Pending");
        v.IsPending.Should().BeTrue();
    }

    [Fact]
    public void IsPending_ApprovedStatus_ReturnsFalse()
    {
        var v = CreateVerification(status: "Approved");
        v.IsPending.Should().BeFalse();
    }

    [Fact]
    public void Properties_SetCorrectly()
    {
        var v = CreateVerification();
        v.VerificationType.Should().Be("email");
        v.VerifiedValue.Should().Be("test@example.com");
        v.ConfidenceScore.Should().BeNull();
        v.ReviewedBy.Should().BeNull();
    }
}
