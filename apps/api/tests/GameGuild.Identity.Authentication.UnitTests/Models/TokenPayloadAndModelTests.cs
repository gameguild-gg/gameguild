using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Models;

public class TokenPayloadTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var payload = new TokenPayload();

        payload.TokenType.Should().BeEmpty();
        payload.TenantId.Should().BeNull();
        payload.Email.Should().BeNull();
        payload.Roles.Should().BeNull();
        payload.Issuer.Should().BeNull();
        payload.Audience.Should().BeNull();
        payload.Claims.Should().BeNull();
    }

    [Fact]
    public void IsValid_WhenNotExpired_ShouldReturnTrue()
    {
        var payload = new TokenPayload
        {
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        payload.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var payload = new TokenPayload
        {
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        payload.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SecondsUntilExpiration_WhenNotExpired_ShouldReturnPositive()
    {
        var payload = new TokenPayload
        {
            ExpiresAt = DateTime.UtcNow.AddSeconds(60)
        };

        payload.SecondsUntilExpiration.Should().BeGreaterThan(0);
        payload.SecondsUntilExpiration.Should().BeLessOrEqualTo(60);
    }

    [Fact]
    public void SecondsUntilExpiration_WhenExpired_ShouldReturnZero()
    {
        var payload = new TokenPayload
        {
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        payload.SecondsUntilExpiration.Should().Be(0);
    }

    [Fact]
    public void TokenPayload_ShouldStoreAllProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var payload = new TokenPayload
        {
            UserId = userId,
            TokenType = "Access",
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TenantId = tenantId,
            Email = "test@example.com",
            Roles = new[] { "admin", "user" },
            Issuer = "game-guild",
            Audience = "game-guild-api",
            Claims = new Dictionary<string, object> { { "sub", "user-1" } }
        };

        payload.UserId.Should().Be(userId);
        payload.TokenType.Should().Be("Access");
        payload.TenantId.Should().Be(tenantId);
        payload.Email.Should().Be("test@example.com");
        payload.Roles.Should().HaveCount(2);
        payload.Issuer.Should().Be("game-guild");
        payload.Audience.Should().Be("game-guild-api");
        payload.Claims.Should().ContainKey("sub");
    }
}

public class Web3ChallengeTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var challenge = new Web3Challenge();

        challenge.Message.Should().BeEmpty();
        challenge.WalletAddress.Should().BeEmpty();
        challenge.Nonce.Should().BeEmpty();
        challenge.TenantId.Should().BeNull();
    }

    [Fact]
    public void IsValid_WhenNotExpired_ShouldReturnTrue()
    {
        var challenge = new Web3Challenge
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        challenge.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var challenge = new Web3Challenge
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };

        challenge.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SecondsUntilExpiration_WhenNotExpired_ShouldReturnPositive()
    {
        var challenge = new Web3Challenge
        {
            ExpiresAt = DateTime.UtcNow.AddSeconds(120)
        };

        challenge.SecondsUntilExpiration.Should().BeGreaterThan(0);
        challenge.SecondsUntilExpiration.Should().BeLessOrEqualTo(120);
    }

    [Fact]
    public void SecondsUntilExpiration_WhenExpired_ShouldReturnZero()
    {
        var challenge = new Web3Challenge
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };

        challenge.SecondsUntilExpiration.Should().Be(0);
    }

    [Fact]
    public void Web3Challenge_ShouldStoreAllProperties()
    {
        var tenantId = Guid.NewGuid();

        var challenge = new Web3Challenge
        {
            Message = "Sign this message to authenticate",
            WalletAddress = "0x1234567890abcdef",
            Nonce = "abc123",
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            TenantId = tenantId
        };

        challenge.Message.Should().Contain("Sign");
        challenge.WalletAddress.Should().Be("0x1234567890abcdef");
        challenge.Nonce.Should().Be("abc123");
        challenge.TenantId.Should().Be(tenantId);
    }
}

public class LocationInfoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var info = new LocationInfo();

        info.IpAddress.Should().BeEmpty();
        info.Country.Should().BeNull();
        info.CountryCode.Should().BeNull();
        info.Region.Should().BeNull();
        info.City.Should().BeNull();
        info.PostalCode.Should().BeNull();
        info.Latitude.Should().BeNull();
        info.Longitude.Should().BeNull();
        info.Timezone.Should().BeNull();
        info.Isp.Should().BeNull();
        info.Organization.Should().BeNull();
        info.IsProxy.Should().BeNull();
        info.IsHosting.Should().BeNull();
    }

    [Fact]
    public void DisplayLocation_WhenCityAndCountrySet_ShouldReturnFormatted()
    {
        var info = new LocationInfo
        {
            City = "New York",
            Country = "United States"
        };

        info.DisplayLocation.Should().Be("New York, United States");
    }

    [Fact]
    public void DisplayLocation_WhenOnlyCountrySet_ShouldReturnCountry()
    {
        var info = new LocationInfo
        {
            Country = "United States"
        };

        info.DisplayLocation.Should().Be("United States");
    }

    [Fact]
    public void DisplayLocation_WhenNothingSet_ShouldReturnUnknown()
    {
        var info = new LocationInfo();

        info.DisplayLocation.Should().Be("Unknown Location");
    }

    [Fact]
    public void DisplayLocation_WhenCitySetButNoCountry_ShouldReturnUnknown()
    {
        var info = new LocationInfo { City = "Paris" };

        info.DisplayLocation.Should().Be("Unknown Location");
    }

    [Fact]
    public void LocationInfo_ShouldStoreAllProperties()
    {
        var info = new LocationInfo
        {
            IpAddress = "192.168.1.1",
            Country = "Brazil",
            CountryCode = "BR",
            Region = "São Paulo",
            City = "São Paulo",
            PostalCode = "01000-000",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Timezone = "America/Sao_Paulo",
            Isp = "Vivo",
            Organization = "Telefonica",
            IsProxy = false,
            IsHosting = false
        };

        info.IpAddress.Should().Be("192.168.1.1");
        info.CountryCode.Should().Be("BR");
        info.Latitude.Should().Be(-23.5505);
        info.Longitude.Should().Be(-46.6333);
        info.IsProxy.Should().BeFalse();
        info.IsHosting.Should().BeFalse();
    }
}

public class BlockchainCertificateAnchorTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var anchor = new BlockchainCertificateAnchor();

        anchor.CertificateType.Should().BeEmpty();
        anchor.CertificateHash.Should().BeEmpty();
        anchor.CertificateData.Should().BeEmpty();
        anchor.TransactionHash.Should().BeEmpty();
        anchor.BlockchainNetwork.Should().BeEmpty();
        anchor.BlockNumber.Should().BeNull();
        anchor.IsRevoked.Should().BeFalse();
        anchor.RevokedAt.Should().BeNull();
        anchor.RevocationReason.Should().BeNull();
        anchor.RevocationTransactionHash.Should().BeNull();
        anchor.ExpiresAt.Should().BeNull();
        anchor.Metadata.Should().BeNull();
    }

    [Fact]
    public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = false,
            ExpiresAt = null
        };

        anchor.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenNotRevokedAndFutureExpiry_ShouldReturnTrue()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        anchor.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        anchor.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        anchor.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenRevokedAndExpired_ShouldReturnFalse()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        anchor.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BlockchainCertificateAnchor_ShouldStoreAllProperties()
    {
        var userId = Guid.NewGuid();
        var anchor = new BlockchainCertificateAnchor
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CertificateType = "EmailVerified",
            CertificateHash = "abc123hash",
            CertificateData = "{\"email\": \"test@example.com\"}",
            TransactionHash = "0xdeadbeef",
            BlockchainNetwork = "Polygon",
            BlockNumber = 12345678,
            AnchoredAt = DateTime.UtcNow,
            Metadata = "{\"version\": 1}"
        };

        anchor.UserId.Should().Be(userId);
        anchor.CertificateType.Should().Be("EmailVerified");
        anchor.BlockchainNetwork.Should().Be("Polygon");
        anchor.BlockNumber.Should().Be(12345678);
    }
}
