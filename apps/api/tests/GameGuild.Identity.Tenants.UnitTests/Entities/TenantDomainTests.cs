using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

/// <summary>
/// Unit tests for TenantDomain entity
/// </summary>
public class TenantDomainTests
{
    [Fact]
    public void TenantDomain_Should_Be_Created_With_Valid_Properties()
    {
        // Arrange & Act
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "example.com",
            Subdomain = "test"
        };

        // Assert
        domain.Should().NotBeNull();
        domain.TopLevelDomain.Should().Be("example.com");
        domain.Subdomain.Should().Be("test");
    }

    [Fact]
    public void TenantDomain_Partial_Constructor_Should_Map_Properties()
    {
        var domain = new TenantDomain(new { TopLevelDomain = "example.com", Subdomain = "team" });

        domain.TopLevelDomain.Should().Be("example.com");
        domain.Subdomain.Should().Be("team");
    }

    [Fact]
    public void TenantDomain_Should_Support_SetAsMainDomain()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "example.com"
        };

        // Act
        domain.SetAsMainDomain();

        // Assert
        domain.IsMainDomain.Should().BeTrue();
    }

    [Fact]
    public void TenantDomain_Should_Normalize_Domain_To_Lowercase()
    {
        // Arrange
        var domain = new TenantDomain
        {
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "Example.COM",
            Subdomain = "Admin"
        };

        // Assert
        domain.TopLevelDomain.Should().Be("example.com");
        domain.Subdomain.Should().Be("admin");
        domain.FullDomain.Should().Be("admin.example.com");
    }

    [Fact]
    public void FullDomain_Should_Return_TopLevel_When_No_Subdomain()
    {
        // Arrange
        var domain = new TenantDomain
        {
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "example.com",
            Subdomain = null
        };

        // Assert
        domain.FullDomain.Should().Be("example.com");
    }

    [Fact]
    public void MatchesEmail_Should_Return_True_For_Matching_Domain()
    {
        // Arrange
        var domain = new TenantDomain
        {
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "example.com",
            Subdomain = "team"
        };

        // Act
        var result = domain.MatchesEmail("user@team.example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("user@other.com")]
    public void MatchesEmail_Should_Return_False_For_Invalid_Or_NonMatching_Email(string email)
    {
        // Arrange
        var domain = new TenantDomain
        {
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "example.com",
            Subdomain = "team"
        };

        // Act
        var result = domain.MatchesEmail(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetAsSecondaryDomain_Should_Set_Flags()
    {
        // Arrange
        var domain = new TenantDomain
        {
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "example.com"
        };

        // Act
        domain.SetAsSecondaryDomain();

        // Assert
        domain.IsMainDomain.Should().BeFalse();
        domain.IsSecondaryDomain.Should().BeTrue();
    }
}
