using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests.Models;

public class ResourceLimitCheckResponseTests
{
    [Fact]
    public void IsSoftLimitWarning_WhenSoftLimitSetAndUsageAbove_ReturnsTrue()
    {
        var response = new ResourceLimitCheckResponse { SoftLimit = 50, CurrentUsage = 60 };

        response.IsSoftLimitWarning.Should().BeTrue();
    }

    [Fact]
    public void IsSoftLimitWarning_WhenSoftLimitNull_ReturnsFalse()
    {
        var response = new ResourceLimitCheckResponse { SoftLimit = null, CurrentUsage = 60 };

        response.IsSoftLimitWarning.Should().BeFalse();
    }

    [Fact]
    public void UsagePercentage_WithHardLimit_ReturnsCorrectPercentage()
    {
        var response = new ResourceLimitCheckResponse { HardLimit = 200, CurrentUsage = 50 };

        response.UsagePercentage.Should().Be(25.0);
    }

    [Fact]
    public void UsagePercentage_WhenNoHardLimit_ReturnsZero()
    {
        var response = new ResourceLimitCheckResponse { HardLimit = null, CurrentUsage = 50 };

        response.UsagePercentage.Should().Be(0);
    }

    [Fact]
    public void RemainingQuota_WhenExceeded_ClampsToZero()
    {
        var response = new ResourceLimitCheckResponse { HardLimit = 50, CurrentUsage = 80 };

        response.RemainingQuota.Should().Be(0);
    }

    [Fact]
    public void RemainingQuota_WhenNoHardLimit_ReturnsNull()
    {
        var response = new ResourceLimitCheckResponse { HardLimit = null };

        response.RemainingQuota.Should().BeNull();
    }

    [Fact]
    public void ThrowIfExceeded_WhenCanProceed_DoesNotThrow()
    {
        var response = new ResourceLimitCheckResponse { CanProceed = true };
        var act = () => response.ThrowIfExceeded(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfExceeded_WhenCannotProceed_ThrowsQuotaExceededException()
    {
        var tenantId = Guid.NewGuid();
        var response = new ResourceLimitCheckResponse
        {
            CanProceed = false,
            Type = ResourceUsageType.Users,
            CurrentUsage = 100,
            HardLimit = 50
        };

        var act = () => response.ThrowIfExceeded(tenantId, 5);

        act.Should().Throw<QuotaExceededException>()
            .Which.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Message_DefaultsToEmpty()
    {
        new ResourceLimitCheckResponse().Message.Should().BeEmpty();
    }
}

public sealed class ConcreteResourceUsageItem : ResourceUsageItem
{
}

public class ResourceUsageItemTests
{
    [Fact]
    public void PercentageUsed_WithPositiveLimit_ReturnsPercentage()
    {
        var item = new ConcreteResourceUsageItem { Current = 25, Limit = 100 };

        item.PercentageUsed.Should().Be(25.0);
    }

    [Fact]
    public void PercentageUsed_WithZeroLimit_ReturnsZero()
    {
        var item = new ConcreteResourceUsageItem { Current = 25, Limit = 0 };

        item.PercentageUsed.Should().Be(0);
    }

    [Fact]
    public void IsLimitExceeded_WhenCurrentEqualsLimit_ReturnsTrue()
    {
        var item = new ConcreteResourceUsageItem { Current = 100, Limit = 100 };

        item.IsLimitExceeded.Should().BeTrue();
    }

    [Fact]
    public void IsLimitExceeded_WhenCurrentBelowLimit_ReturnsFalse()
    {
        var item = new ConcreteResourceUsageItem { Current = 50, Limit = 100 };

        item.IsLimitExceeded.Should().BeFalse();
    }

    [Fact]
    public void SetProperties_AllWork()
    {
        var now = DateTime.UtcNow;
        var item = new ConcreteResourceUsageItem
        {
            Current = 10,
            Limit = 50,
            Timestamp = now,
            Amount = 5,
            PeakUsage = 30
        };

        item.Amount.Should().Be(5);
        item.PeakUsage.Should().Be(30);
        item.Timestamp.Should().Be(now);
    }
}

public class ResourceUsageTypeRegistryTests
{
    [Fact]
    public void Get_BuiltInType_ReturnsInfo()
    {
        var info = ResourceUsageTypeRegistry.Get(ResourceUsageType.Users);

        info.Key.Should().Be("Users");
        info.IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    public void GetById_BuiltInType_ReturnsInfo()
    {
        var info = ResourceUsageTypeRegistry.GetById((int)ResourceUsageType.Storage);

        info.Key.Should().Be("Storage");
        info.Unit.Should().Be("bytes");
    }

    [Fact]
    public void GetById_UnknownId_ThrowsKeyNotFoundException()
    {
        var act = () => ResourceUsageTypeRegistry.GetById(99999);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GetByKey_BuiltInType_ReturnsInfo()
    {
        var info = ResourceUsageTypeRegistry.GetByKey("ApiCalls");

        info.DefaultPeriod.Should().Be(ResourceQuotaPeriod.Daily);
    }

    [Fact]
    public void GetByKey_CaseInsensitive_Works()
    {
        var info = ResourceUsageTypeRegistry.GetByKey("apicalls");

        info.Key.Should().Be("ApiCalls");
    }

    [Fact]
    public void GetByKey_Unknown_ThrowsKeyNotFoundException()
    {
        var act = () => ResourceUsageTypeRegistry.GetByKey("NonExistent");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GetByKey_NullOrWhitespace_ThrowsArgumentException()
    {
        var act = () => ResourceUsageTypeRegistry.GetByKey(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetById_ExistingId_ReturnsTrue()
    {
        ResourceUsageTypeRegistry.TryGetById((int)ResourceUsageType.Users, out var info).Should().BeTrue();
        info.Should().NotBeNull();
    }

    [Fact]
    public void TryGetById_UnknownId_ReturnsFalse()
    {
        ResourceUsageTypeRegistry.TryGetById(99999, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetByKey_ExistingKey_ReturnsTrue()
    {
        ResourceUsageTypeRegistry.TryGetByKey("Users", out var info).Should().BeTrue();
        info.Should().NotBeNull();
    }

    [Fact]
    public void TryGetByKey_NullOrWhitespace_ReturnsFalse()
    {
        ResourceUsageTypeRegistry.TryGetByKey(string.Empty, out _).Should().BeFalse();
        ResourceUsageTypeRegistry.TryGetByKey(null!, out _).Should().BeFalse();
    }

    [Fact]
    public void ToKey_BuiltInType_ReturnsKey()
    {
        ResourceUsageTypeRegistry.ToKey(ResourceUsageType.Users).Should().Be("Users");
    }

    [Fact]
    public void ToEnum_BuiltInKey_ReturnsEnum()
    {
        ResourceUsageTypeRegistry.ToEnum("Users").Should().Be(ResourceUsageType.Users);
    }
}

public class ResourceUsageTypeInfoTests
{
    [Fact]
    public void ToEnum_WhenCustomType_ThrowsInvalidOperationException()
    {
        var info = new ResourceUsageTypeInfo
        {
            Id = 1001,
            Key = "Assets",
            DisplayName = "Assets",
            IsBuiltIn = false
        };

        var act = () => info.ToEnum();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FromEnum_BuiltInType_ReturnsRegistryInfo()
    {
        var info = ResourceUsageTypeInfo.FromEnum(ResourceUsageType.Users);

        info.Key.Should().Be("Users");
        info.IsBuiltIn.Should().BeTrue();
    }
}
