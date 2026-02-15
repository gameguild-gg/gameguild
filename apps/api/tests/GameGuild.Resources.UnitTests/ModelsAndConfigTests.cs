using FluentAssertions;
using GameGuild.Resources;
using Xunit;

namespace GameGuild.Resources.UnitTests;

#region ResourceLimitCheckResponse Tests

public class ResourceLimitCheckResponseTests
{
    [Fact]
    public void IsSoftLimitWarning_WhenSoftLimitSet_AndUsageAbove_ReturnsTrue()
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
    public void IsSoftLimitWarning_WhenUsageBelowSoftLimit_ReturnsFalse()
    {
        var response = new ResourceLimitCheckResponse { SoftLimit = 100, CurrentUsage = 50 };
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
    public void UsagePercentage_WhenHardLimitIsZero_ReturnsZero()
    {
        var response = new ResourceLimitCheckResponse { HardLimit = 0, CurrentUsage = 50 };
        response.UsagePercentage.Should().Be(0);
    }

    [Fact]
    public void RemainingQuota_WithHardLimit_ReturnsRemaining()
    {
        var response = new ResourceLimitCheckResponse { HardLimit = 100, CurrentUsage = 30 };
        response.RemainingQuota.Should().Be(70);
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
            CanProceed = false, Type = ResourceUsageType.Users,
            CurrentUsage = 100, HardLimit = 50
        };
        var act = () => response.ThrowIfExceeded(tenantId, 5);
        act.Should().Throw<QuotaExceededException>()
            .Which.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void DefaultMessage_IsEmpty()
    {
        new ResourceLimitCheckResponse().Message.Should().BeEmpty();
    }
}

#endregion

#region QuotaExceededException Tests

public class QuotaExceededExceptionTests
{
    [Fact]
    public void Constructor_WithDefaultMessage_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var ex = new QuotaExceededException(ResourceUsageType.Storage, 80, 100, tenantId);

        ex.ResourceType.Should().Be(ResourceUsageType.Storage);
        ex.CurrentUsage.Should().Be(80);
        ex.Limit.Should().Be(100);
        ex.TenantId.Should().Be(tenantId);
        ex.Message.Should().Contain("Storage");
    }

    [Fact]
    public void Constructor_WithCustomMessage_SetsMessage()
    {
        var ex = new QuotaExceededException("custom msg", ResourceUsageType.Users, 10, 5, Guid.NewGuid());
        ex.Message.Should().Be("custom msg");
    }

    [Fact]
    public void RemainingQuota_WhenBelowLimit_ReturnsPositive()
    {
        var ex = new QuotaExceededException(ResourceUsageType.Users, 60, 100, Guid.NewGuid());
        ex.RemainingQuota.Should().Be(40);
    }

    [Fact]
    public void RemainingQuota_WhenExceeded_ClampsToZero()
    {
        var ex = new QuotaExceededException(ResourceUsageType.Users, 150, 100, Guid.NewGuid());
        ex.RemainingQuota.Should().Be(0);
    }
}

#endregion

#region ResourceUsageItem Tests

public class ConcreteResourceUsageItem : ResourceUsageItem { }

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
            Current = 10, Limit = 50, Timestamp = now,
            Amount = 5, PeakUsage = 30
        };
        item.Amount.Should().Be(5);
        item.PeakUsage.Should().Be(30);
        item.Timestamp.Should().Be(now);
    }
}

#endregion

#region ResourceUsageTypeRegistry Tests

[Collection("ResourceUsageTypeRegistry")]
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
        var act = () => ResourceUsageTypeRegistry.GetByKey("");
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
        ResourceUsageTypeRegistry.TryGetByKey("", out _).Should().BeFalse();
        ResourceUsageTypeRegistry.TryGetByKey(null!, out _).Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsBuiltInTypes()
    {
        ResourceUsageTypeRegistry.GetAll().Should().NotBeEmpty();
    }

    [Fact]
    public void GetBuiltIn_ReturnsOnlyBuiltIn()
    {
        ResourceUsageTypeRegistry.GetBuiltIn().Should().OnlyContain(t => t.IsBuiltIn);
    }

    [Fact]
    public void GetCustom_WhenNoCustom_ReturnsEmpty()
    {
        ResourceUsageTypeRegistry.GetCustom().Should().BeEmpty();
    }

    [Fact]
    public void IsRegistered_ById_ReturnsTrueForKnownType()
    {
        ResourceUsageTypeRegistry.IsRegistered((int)ResourceUsageType.Users).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_ById_ReturnsFalseForUnknown()
    {
        ResourceUsageTypeRegistry.IsRegistered(99999).Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_ByKey_ReturnsTrueForKnownType()
    {
        ResourceUsageTypeRegistry.IsRegistered("Users").Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_ByKey_ReturnsFalseForUnknown()
    {
        ResourceUsageTypeRegistry.IsRegistered("NonExistent").Should().BeFalse();
    }

    [Fact]
    public void ToKey_ReturnsCorrectKey()
    {
        ResourceUsageTypeRegistry.ToKey(ResourceUsageType.Projects).Should().Be("Projects");
    }

    [Fact]
    public void ToEnum_ReturnsCorrectEnum()
    {
        ResourceUsageTypeRegistry.ToEnum("Projects").Should().Be(ResourceUsageType.Projects);
    }

    [Fact]
    public void CustomTypeIdStart_Is1000()
    {
        ResourceUsageTypeRegistry.CustomTypeIdStart.Should().Be(1000);
    }
}

#endregion

#region ResourceUsageTypeInfo Tests

public class ResourceUsageTypeInfoTests
{
    [Fact]
    public void ToEnum_WhenBuiltIn_ReturnsEnum()
    {
        var info = ResourceUsageTypeRegistry.Get(ResourceUsageType.Users);
        info.ToEnum().Should().Be(ResourceUsageType.Users);
    }

    [Fact]
    public void ToEnum_WhenNotBuiltIn_Throws()
    {
        var info = new ResourceUsageTypeInfo
        {
            Id = 2000, Key = "Custom", DisplayName = "Custom Type", IsBuiltIn = false
        };
        var act = () => info.ToEnum();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FromEnum_ReturnsRegistryResult()
    {
        var info = ResourceUsageTypeInfo.FromEnum(ResourceUsageType.Storage);
        info.Key.Should().Be("Storage");
    }

    [Fact]
    public void Default_Unit_IsCount()
    {
        var info = new ResourceUsageTypeInfo { Id = 3000, Key = "Test", DisplayName = "Test" };
        info.Unit.Should().Be("count");
    }

    [Fact]
    public void Default_SupportsSoftLimit_IsTrue()
    {
        var info = new ResourceUsageTypeInfo { Id = 3001, Key = "Test2", DisplayName = "Test2" };
        info.SupportsSoftLimit.Should().BeTrue();
    }
}

#endregion

#region ResourcesOptions Tests

public class ResourcesOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var opts = new ResourcesOptions();
        opts.MaxFileSize.Should().Be(10 * 1024 * 1024);
        opts.BasePath.Should().Be("uploads");
        opts.EnableContentScanning.Should().BeTrue();
        opts.DefaultCostPerUnit.Should().Be(0.01m);
        opts.CostPerUnit.Should().ContainKey("Users");
        opts.AllowedFileExtensions.Should().Contain(".jpg");
    }

    [Fact]
    public void Validate_ValidDefaults_DoesNotThrow()
    {
        var opts = new ResourcesOptions();
        var act = () => opts.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroMaxFileSize_Throws()
    {
        var opts = new ResourcesOptions { MaxFileSize = 0 };
        var act = () => opts.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxFileSize*");
    }

    [Fact]
    public void Validate_EmptyBasePath_Throws()
    {
        var opts = new ResourcesOptions { BasePath = "" };
        var act = () => opts.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*BasePath*");
    }

    [Fact]
    public void Validate_EmptyExtensions_Throws()
    {
        var opts = new ResourcesOptions { AllowedFileExtensions = [] };
        var act = () => opts.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*extension*");
    }

    [Fact]
    public void Validate_EmptyCostPerUnit_Throws()
    {
        var opts = new ResourcesOptions { CostPerUnit = new() };
        var act = () => opts.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*CostPerUnit*");
    }

    [Fact]
    public void Validate_NegativeDefaultCost_Throws()
    {
        var opts = new ResourcesOptions { DefaultCostPerUnit = -1 };
        var act = () => opts.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*DefaultCostPerUnit*");
    }
}

#endregion

#region SlaEscalationResult Tests

public class SlaEscalationResultTests
{
    [Fact]
    public void Success_SetsExpectedProperties()
    {
        var users = new List<Guid> { Guid.NewGuid() };
        var result = SlaEscalationResult.Success("INC-123", users);

        result.WasEscalated.Should().BeTrue();
        result.IncidentId.Should().Be("INC-123");
        result.NotificationSent.Should().BeTrue();
        result.NotifiedUserIds.Should().HaveCount(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Success_NullUsers_DefaultsToEmptyList()
    {
        var result = SlaEscalationResult.Success("INC-456");
        result.NotifiedUserIds.Should().BeEmpty();
    }

    [Fact]
    public void NotRequired_SetsExpectedProperties()
    {
        var result = SlaEscalationResult.NotRequired();
        result.WasEscalated.Should().BeFalse();
        result.NotificationSent.Should().BeFalse();
        result.IncidentId.Should().BeNull();
    }

    [Fact]
    public void Failed_SetsErrorMessage()
    {
        var result = SlaEscalationResult.Failed("something went wrong");
        result.WasEscalated.Should().BeFalse();
        result.NotificationSent.Should().BeFalse();
        result.ErrorMessage.Should().Be("something went wrong");
    }
}

#endregion

#region SlaEscalationConfig Tests

public class SlaEscalationConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new SlaEscalationConfig();
        config.AutoEscalationEnabled.Should().BeTrue();
        config.MinimumEscalationSeverity.Should().Be(SlaViolationSeverity.High);
        config.AutoCreateIncidents.Should().BeTrue();
        config.NotificationCooldownMinutes.Should().Be(15);
        config.EscalationEmails.Should().BeEmpty();
        config.EscalationUserIds.Should().BeEmpty();
    }
}

#endregion

#region RequiresQuotaAttribute Tests

public class RequiresQuotaAttributeTests
{
    [Fact]
    public void Constructor_SetsRequiredProperties()
    {
        var attr = new RequiresQuotaAttribute(ResourceUsageType.Storage, 5);
        attr.ResourceType.Should().Be(ResourceUsageType.Storage);
        attr.Amount.Should().Be(5);
    }

    [Fact]
    public void DefaultAmount_IsOne()
    {
        var attr = new RequiresQuotaAttribute(ResourceUsageType.Users);
        attr.Amount.Should().Be(1);
    }

    [Fact]
    public void DefaultRecordUsage_IsTrue()
    {
        var attr = new RequiresQuotaAttribute(ResourceUsageType.Users);
        attr.RecordUsage.Should().BeTrue();
    }

    [Fact]
    public void Source_CanBeSet()
    {
        var attr = new RequiresQuotaAttribute(ResourceUsageType.Users) { Source = "test" };
        attr.Source.Should().Be("test");
    }
}

#endregion

#region Event Tests

public class QuotaChangedEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;
        var evt = new QuotaChangedEvent(
            tenantId, ResourceUsageType.Users, QuotaChangeType.Created,
            null, 5, 10, 100, "test", actorId, ts);

        evt.TenantId.Should().Be(tenantId);
        evt.ResourceType.Should().Be(ResourceUsageType.Users);
        evt.ChangeType.Should().Be(QuotaChangeType.Created);
        evt.PreviousUsage.Should().BeNull();
        evt.CurrentUsage.Should().Be(5);
        evt.SoftLimit.Should().Be(10);
        evt.HardLimit.Should().Be(100);
        evt.Source.Should().Be("test");
        evt.ActorId.Should().Be(actorId);
        evt.EventId.Should().NotBeEmpty();
        evt.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(QuotaChangeType.Created)]
    [InlineData(QuotaChangeType.UsageIncremented)]
    [InlineData(QuotaChangeType.UsageDecremented)]
    [InlineData(QuotaChangeType.LimitsUpdated)]
    [InlineData(QuotaChangeType.Reset)]
    [InlineData(QuotaChangeType.Deleted)]
    public void QuotaChangeType_AllValuesValid(QuotaChangeType type)
    {
        Enum.IsDefined(type).Should().BeTrue();
    }
}

public class QuotaExceededEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;
        var evt = new QuotaExceededEvent(tenantId, ResourceUsageType.Storage, 150, 10, 100, "api", null, ts);

        evt.TenantId.Should().Be(tenantId);
        evt.CurrentUsage.Should().Be(150);
        evt.RequestedAmount.Should().Be(10);
        evt.HardLimit.Should().Be(100);
        evt.Source.Should().Be("api");
        evt.ActorId.Should().BeNull();
        evt.EventId.Should().NotBeEmpty();
        evt.Version.Should().Be(1);
    }
}

#endregion

#region DTO Tests

public class ResourceQuotaResponseTests
{
    [Fact]
    public void AllProperties_CanBeSetAndRead()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dto = new ResourceQuotaResponse
        {
            Id = id, TenantId = tenantId,
            Type = ResourceUsageType.Users, Limit = 100,
            CurrentUsage = 50, RemainingQuota = 50,
            UsagePercentage = 50m, SoftLimitPercentage = 80m,
            IsActive = true, Period = ResourceQuotaPeriod.Monthly,
            LastResetDate = DateTime.UtcNow.AddDays(-1),
            NextResetDate = DateTime.UtcNow.AddDays(29),
            Description = "desc", IsSoftLimitExceeded = false,
            IsHardLimitExceeded = false, ShouldReset = false,
            SoftLimit = 80, HardLimit = 100
        };

        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.UsagePercentage.Should().Be(50m);
        dto.SoftLimit.Should().Be(80);
        dto.HardLimit.Should().Be(100);
    }
}

public class ResourceUsageHistoryResponseTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var r = new ResourceUsageHistoryResponse();
        r.Period.Should().BeEmpty();
        r.Usage.Should().BeEmpty();
    }
}

public class ResourceUsageResponseTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var r = new ResourceUsageResponse();
        r.Usage.Should().BeEmpty();
        r.History.Should().BeEmpty();
    }
}

#endregion
