using FluentAssertions;


namespace GameGuild.Tests.SharedKernel.Unit;

public class AddressTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");

        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.PostalCode.Should().Be("62701");
        address.Country.Should().Be("US");
        address.Unit.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithUnit_SetsUnit()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US", "4B");

        address.Unit.Should().Be("4B");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var address = new Address("  123 Main St  ", "  Springfield  ", "  IL  ", "  62701  ", "  US  ", "  4B  ");

        address.Street.Should().Be("123 Main St");
        address.Unit.Should().Be("4B");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyStreet_Throws(string? street)
    {
        var act = () => new Address(street!, "City", "ST", "12345", "US");

        act.Should().Throw<ArgumentException>().WithParameterName("street");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyCity_Throws(string? city)
    {
        var act = () => new Address("Street", city!, "ST", "12345", "US");

        act.Should().Throw<ArgumentException>().WithParameterName("city");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyState_Throws(string? state)
    {
        var act = () => new Address("Street", "City", state!, "12345", "US");

        act.Should().Throw<ArgumentException>().WithParameterName("state");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyPostalCode_Throws(string? postalCode)
    {
        var act = () => new Address("Street", "City", "ST", postalCode!, "US");

        act.Should().Throw<ArgumentException>().WithParameterName("postalCode");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyCountry_Throws(string? country)
    {
        var act = () => new Address("Street", "City", "ST", "12345", country!);

        act.Should().Throw<ArgumentException>().WithParameterName("country");
    }

    [Fact]
    public void GetFullAddress_WithoutUnit_ReturnsMultiLine()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");

        var full = address.GetFullAddress();

        full.Should().Contain("123 Main St");
        full.Should().Contain("Springfield, IL 62701");
        full.Should().Contain("US");
        full.Should().NotContain("Unit");
    }

    [Fact]
    public void GetFullAddress_WithUnit_IncludesUnit()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US", "4B");

        var full = address.GetFullAddress();

        full.Should().Contain("Unit 4B");
    }

    [Fact]
    public void GetOneLine_WithoutUnit_ReturnsSingleLine()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");

        var line = address.GetOneLine();

        line.Should().Be("123 Main St, Springfield, IL, 62701, US");
    }

    [Fact]
    public void GetOneLine_WithUnit_IncludesUnit()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US", "4B");

        var line = address.GetOneLine();

        line.Should().Contain("Unit 4B");
    }

    [Fact]
    public void ToString_DelegatesToGetOneLine()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");

        address.ToString().Should().Be(address.GetOneLine());
    }
}

public class BusinessRuleViolationExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var exception = new BusinessRuleViolationException("MAX_USERS", "Too many users", new { Count = 100 });

        exception.Rule.Should().Be("MAX_USERS");
        exception.Message.Should().Be("Too many users");
        exception.Context.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullContext_IsAllowed()
    {
        var exception = new BusinessRuleViolationException("RULE", "message");

        exception.Context.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new BusinessRuleViolationException("RULE", "message", inner, new { Data = "test" });

        exception.InnerException.Should().Be(inner);
        exception.Rule.Should().Be("RULE");
        exception.Context.Should().NotBeNull();
    }

    [Fact]
    public void InheritsFromDomainException()
    {
        var exception = new BusinessRuleViolationException("RULE", "message");

        exception.Should().BeAssignableTo<DomainException>();
    }
}

public class UserInfoTests
{
    [Fact]
    public void CanBeCreated_WithAllParams()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new UserInfo(id, "user@example.com", "John Doe", true, tenantId);

        user.Id.Should().Be(id);
        user.Email.Should().Be("user@example.com");
        user.Name.Should().Be("John Doe");
        user.IsActive.Should().BeTrue();
        user.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TenantId_DefaultsToNull()
    {
        var user = new UserInfo(Guid.NewGuid(), "a@b.com", "Test", true);

        user.TenantId.Should().BeNull();
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var first = new UserInfo(id, "a@b.com", "Test", true);
        var second = new UserInfo(id, "a@b.com", "Test", true);

        first.Should().Be(second);
    }
}

public class TenantInfoTests
{
    [Fact]
    public void CanBeCreated()
    {
        var id = Guid.NewGuid();
        var tenant = new TenantInfo(id, "Acme Corp", "acme-corp", true);

        tenant.Id.Should().Be(id);
        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme-corp");
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var first = new TenantInfo(id, "Acme", "acme", true);
        var second = new TenantInfo(id, "Acme", "acme", true);

        first.Should().Be(second);
    }
}

public class SystemClockAdditionalTests
{
    [Fact]
    public void SetProvider_NullProvider_ThrowsArgumentNullException()
    {
        var act = () => SystemClock.SetProvider(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetProvider_CustomProvider_ChangesUtcNow()
    {
        try
        {
            var fakeTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            var fakeProvider = new FakeTimeProvider(fakeTime);

            SystemClock.SetProvider(fakeProvider);

            SystemClock.UtcNow.Should().Be(fakeTime.UtcDateTime);
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public void Reset_RestoresSystemTime()
    {
        var fakeTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        SystemClock.SetProvider(new FakeTimeProvider(fakeTime));
        SystemClock.Reset();

        SystemClock.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    private sealed class FakeTimeProvider(DateTimeOffset time) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => time;
    }
}
