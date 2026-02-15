using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Resources;
using Xunit;

namespace GameGuild.Resources.UnitTests;

#region Command Validators

public class SetResourceQuotaCommandValidatorTests
{
    private readonly SetResourceQuotaCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new SetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, 50, 100);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var cmd = new SetResourceQuotaCommand(Guid.Empty, ResourceUsageType.Users, 50, 100);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void SoftLimitExceedsHardLimit_ShouldFail()
    {
        var cmd = new SetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, 200, 100);
        _validator.TestValidate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SoftLimitZero_ShouldFail()
    {
        var cmd = new SetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, 0, 100);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.SoftLimit);
    }

    [Fact]
    public void NullLimits_ShouldPass()
    {
        var cmd = new SetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, null, null);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidResetTime_ShouldFail()
    {
        var cmd = new SetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, null, null, ResetTime: TimeSpan.FromHours(25));
        _validator.TestValidate(cmd).IsValid.Should().BeFalse();
    }
}

public class RecordResourceUsageCommandValidatorTests
{
    private readonly RecordResourceUsageCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new RecordResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.ApiCalls, 10, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var cmd = new RecordResourceUsageCommand(Guid.Empty, ResourceUsageType.ApiCalls, 10, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void ZeroCount_ShouldFail()
    {
        var cmd = new RecordResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.ApiCalls, 0, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void EndBeforeStart_ShouldFail()
    {
        var cmd = new RecordResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.ApiCalls, 10, DateTime.UtcNow, DateTime.UtcNow.AddHours(-1));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PeriodEnd);
    }

    [Fact]
    public void MetadataTooLong_ShouldFail()
    {
        var cmd = new RecordResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.ApiCalls, 10, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, Metadata: new string('X', 1001));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Metadata);
    }
}

public class SetUserResourceQuotaCommandValidatorTests
{
    private readonly SetUserResourceQuotaCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new SetUserResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Storage, 50, 100);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new SetUserResourceQuotaCommand(Guid.Empty, ResourceUsageType.Storage, null, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void HardLimitLessThanSoft_ShouldFail()
    {
        var cmd = new SetUserResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Storage, 100, 50);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.HardLimit);
    }
}

public class RecordUserResourceUsageCommandValidatorTests
{
    private readonly RecordUserResourceUsageCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new RecordUserResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.Storage, 5, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new RecordUserResourceUsageCommand(Guid.Empty, ResourceUsageType.Storage, 5, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ZeroCount_ShouldFail()
    {
        var cmd = new RecordUserResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.Storage, 0, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void StartAfterEnd_ShouldFail()
    {
        var cmd = new RecordUserResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.Storage, 5, DateTime.UtcNow, DateTime.UtcNow.AddHours(-1));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PeriodStart);
    }
}

public class DeleteResourceQuotaCommandValidatorTests
{
    private readonly DeleteResourceQuotaCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new DeleteResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var cmd = new DeleteResourceQuotaCommand(Guid.Empty, ResourceUsageType.Users);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class ResetResourceQuotaCommandValidatorTests
{
    private readonly ResetResourceQuotaCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        _validator.TestValidate(new ResetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.ApiCalls))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new ResetResourceQuotaCommand(Guid.Empty, ResourceUsageType.ApiCalls))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class ResetResourceUsageCommandValidatorTests
{
    private readonly ResetResourceUsageCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_WithoutType_ShouldPass()
    {
        _validator.TestValidate(new ResetResourceUsageCommand(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new ResetResourceUsageCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class ToggleResourceQuotaCommandValidatorTests
{
    private readonly ToggleResourceQuotaCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        _validator.TestValidate(new ToggleResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, true))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new ToggleResourceQuotaCommand(Guid.Empty, ResourceUsageType.Users, true))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class ArchiveResourceUsageRecordsCommandValidatorTests
{
    private readonly ArchiveResourceUsageRecordsCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        _validator.TestValidate(new ArchiveResourceUsageRecordsCommand(DateTime.UtcNow.AddDays(-30)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FutureDate_ShouldFail()
    {
        _validator.TestValidate(new ArchiveResourceUsageRecordsCommand(DateTime.UtcNow.AddDays(1)))
            .ShouldHaveValidationErrorFor(x => x.OlderThan);
    }
}

#endregion

#region Query Validators

public class GetResourceQuotaQueryValidatorTests
{
    private readonly GetResourceQuotaQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetResourceQuotaQuery(Guid.NewGuid(), ResourceUsageType.Users))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetResourceQuotaQuery(Guid.Empty, ResourceUsageType.Users))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class CheckResourceQuotaQueryValidatorTests
{
    private readonly CheckResourceQuotaQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new CheckResourceQuotaQuery(Guid.NewGuid(), ResourceUsageType.Users, 1))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroAmount_ShouldFail()
    {
        _validator.TestValidate(new CheckResourceQuotaQuery(Guid.NewGuid(), ResourceUsageType.Users, 0))
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new CheckResourceQuotaQuery(Guid.Empty, ResourceUsageType.Users, 1))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class GetResourceUsageByTypeQueryValidatorTests
{
    private readonly GetResourceUsageByTypeQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetResourceUsageByTypeQuery(ResourceUsageType.ApiCalls, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddHours(-1)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndBeforeStart_ShouldFail()
    {
        _validator.TestValidate(new GetResourceUsageByTypeQuery(ResourceUsageType.ApiCalls, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-7)))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
    }
}

public class GetResourceUsageRecordsQueryValidatorTests
{
    private readonly GetResourceUsageRecordsQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetResourceUsageRecordsQuery(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetResourceUsageRecordsQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void FutureStartDate_ShouldFail()
    {
        _validator.TestValidate(new GetResourceUsageRecordsQuery(Guid.NewGuid(), StartDate: DateTime.UtcNow.AddDays(1)))
            .ShouldHaveValidationErrorFor(x => x.StartDate);
    }
}

public class CheckResourceUsageLimitsQueryValidatorTests
{
    private readonly CheckResourceUsageLimitsQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new CheckResourceUsageLimitsQuery(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new CheckResourceUsageLimitsQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class GetTenantResourceQuotasQueryValidatorTests
{
    private readonly GetTenantResourceQuotasQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetTenantResourceQuotasQuery(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetTenantResourceQuotasQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class GetCurrentResourceUsageSummaryQueryValidatorTests
{
    private readonly GetCurrentResourceUsageSummaryQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetCurrentResourceUsageSummaryQuery(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetCurrentResourceUsageSummaryQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

#endregion

#region Command/Query Record Tests

public class ResourceCommandRecordTests
{
    [Fact]
    public void SetResourceQuotaCommand_DefaultValues()
    {
        var cmd = new SetResourceQuotaCommand(Guid.NewGuid(), ResourceUsageType.Users, null, null);
        cmd.Period.Should().Be(ResourceQuotaPeriod.Monthly);
        cmd.IsActive.Should().BeTrue();
        cmd.ResetTime.Should().BeNull();
    }

    [Fact]
    public void RecordResourceUsageCommand_DefaultValues()
    {
        var cmd = new RecordResourceUsageCommand(Guid.NewGuid(), ResourceUsageType.ApiCalls, 10, DateTime.UtcNow, DateTime.UtcNow);
        cmd.Metadata.Should().BeNull();
        cmd.Source.Should().BeNull();
        cmd.SkipQuotaIncrement.Should().BeFalse();
    }

    [Fact]
    public void CheckResourceQuotaQuery_DefaultAmount()
    {
        var query = new CheckResourceQuotaQuery(Guid.NewGuid(), ResourceUsageType.Users);
        query.Amount.Should().Be(1);
    }

    [Fact]
    public void GetResourceUsageRecordsQuery_DefaultPagination()
    {
        var query = new GetResourceUsageRecordsQuery(Guid.NewGuid());
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(50);
    }

    [Fact]
    public void ResetResourceUsageCommand_DefaultType()
    {
        var cmd = new ResetResourceUsageCommand(Guid.NewGuid());
        cmd.ResourceUsageType.Should().BeNull();
    }

    [Fact]
    public void ArchiveResourceUsageRecordsCommand_ShouldStore()
    {
        var date = DateTime.UtcNow.AddDays(-30);
        var cmd = new ArchiveResourceUsageRecordsCommand(date);
        cmd.OlderThan.Should().Be(date);
    }
}

#endregion
