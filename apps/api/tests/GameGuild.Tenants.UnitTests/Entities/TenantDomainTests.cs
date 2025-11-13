using FluentAssertions;
using GameGuild.Tenants.Entities;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Entities;

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
}
