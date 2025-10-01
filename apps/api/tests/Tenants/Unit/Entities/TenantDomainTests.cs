using FluentAssertions;
using GameGuild.Modules.Tenants;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Entities;

/// <summary>
/// Unit tests for TenantDomain entity
/// </summary>
public class TenantDomainTests
{
    [Fact]
    public void Constructor_Should_Create_TenantDomain_With_Default_Values()
    {
        // Act
        var tenantDomain = new TenantDomain();

        // Assert
        _ = tenantDomain.TopLevelDomain.Should().Be(string.Empty);
        _ = tenantDomain.Subdomain.Should().BeNull();
        _ = tenantDomain.TenantId.Should().BeEmpty();
        _ = tenantDomain.IsMainDomain.Should().BeFalse();
        _ = tenantDomain.Id.Should().NotBeEmpty();
        _ = tenantDomain.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = tenantDomain.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TopLevelDomain_Should_Convert_To_Lowercase()
    {
        // Arrange
        var tenantDomain = new TenantDomain();

        // Act
        tenantDomain.TopLevelDomain = "EXAMPLE.COM";

        // Assert
        _ = tenantDomain.TopLevelDomain.Should().Be("example.com");
    }

    [Fact]
    public void Subdomain_Should_Convert_To_Lowercase_When_Set()
    {
        // Arrange
        var tenantDomain = new TenantDomain();

        // Act
        tenantDomain.Subdomain = "STUDENT";

        // Assert
        _ = tenantDomain.Subdomain.Should().Be("student");
    }

    [Fact]
    public void Subdomain_Should_Accept_Null_Value()
    {
        // Arrange
        var tenantDomain = new TenantDomain();

        // Act
        tenantDomain.Subdomain = null;

        // Assert
        _ = tenantDomain.Subdomain.Should().BeNull();
    }

    [Fact]
    public void FullDomainName_Should_Return_TopLevelDomain_When_No_Subdomain()
    {
        // Arrange
        var tenantDomain = new TenantDomain
        {
            TopLevelDomain = "example.com",
            Subdomain = null
        };

        // Act
        string fullDomain = tenantDomain.FullDomainName;

        // Assert
        _ = fullDomain.Should().Be("example.com");
    }

    [Fact]
    public void FullDomainName_Should_Return_Subdomain_Plus_TopLevelDomain_When_Subdomain_Present()
    {
        // Arrange
        var tenantDomain = new TenantDomain
        {
            TopLevelDomain = "example.com",
            Subdomain = "student"
        };

        // Act
        string fullDomain = tenantDomain.FullDomainName;

        // Assert
        _ = fullDomain.Should().Be("student.example.com");
    }

    [Theory]
    [InlineData("example.com", null, "example.com")]
    [InlineData("university.edu", "student", "student.university.edu")]
    [InlineData("COMPANY.NET", "API", "api.company.net")]
    [InlineData("test.org", "", "test.org")]
    public void FullDomainName_Should_Return_Correct_Format(string topLevelDomain, string? subdomain, string expected)
    {
        // Arrange
        var tenantDomain = new TenantDomain
        {
            TopLevelDomain = topLevelDomain,
            Subdomain = subdomain
        };

        // Act
        string fullDomain = tenantDomain.FullDomainName;

        // Assert
        _ = fullDomain.Should().Be(expected);
    }

    [Fact]
    public void TenantDomain_Should_Inherit_From_EntityBase()
    {
        // Arrange & Act  
        var tenantDomain = new TenantDomain();

        // Assert
        _ = tenantDomain.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void TenantDomain_Should_Allow_Setting_All_Properties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var tenantDomain = new TenantDomain
        {
            TenantId = tenantId,
            TopLevelDomain = "example.com",
            Subdomain = "api",
            IsMainDomain = true
        };

        // Assert
        _ = tenantDomain.TenantId.Should().Be(tenantId);
        _ = tenantDomain.TopLevelDomain.Should().Be("example.com");
        _ = tenantDomain.Subdomain.Should().Be("api");
        _ = tenantDomain.IsMainDomain.Should().BeTrue();
    }

    [Fact]
    public void Constructor_With_Partial_Should_Call_Base_Constructor()
    {
        // Arrange
        var partial = new { TopLevelDomain = "test.com" };

        // Act
        var tenantDomain = new TenantDomain(partial);

        // Assert
        _ = tenantDomain.Should().NotBeNull();
        _ = tenantDomain.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Subdomain_Should_Handle_Empty_String(string emptyString)
    {
        // Arrange
        var tenantDomain = new TenantDomain();

        // Act
        tenantDomain.Subdomain = emptyString;

        // Assert
        _ = tenantDomain.Subdomain.Should().Be(emptyString.ToLowerInvariant());
        // For empty string, FullDomainName should be TopLevelDomain, for space it will be " ." 
        string expectedFullDomain = string.IsNullOrEmpty(emptyString.Trim()) && emptyString == ""
            ? tenantDomain.TopLevelDomain
            : $"{emptyString.ToLowerInvariant()}.{tenantDomain.TopLevelDomain}";
        _ = tenantDomain.FullDomainName.Should().Be(expectedFullDomain);
    }

    [Fact]
    public void IsMainDomain_Should_Default_To_False()
    {
        // Act
        var tenantDomain = new TenantDomain();

        // Assert
        _ = tenantDomain.IsMainDomain.Should().BeFalse();
    }

    [Fact]
    public void IsMainDomain_Should_Accept_True_Value()
    {
        // Act
        var tenantDomain = new TenantDomain
        {
            IsMainDomain = true
        };

        // Assert
        _ = tenantDomain.IsMainDomain.Should().BeTrue();
    }
}