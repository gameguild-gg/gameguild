using FluentAssertions;
using FluentValidation.TestHelper;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests;

public class ServiceLevelIndicatorTests
{
    [Fact]
    public void CreateSuccess_ShouldSetAllProperties()
    {
        var sloId = Guid.NewGuid();

        var sli = ServiceLevelIndicator.CreateSuccess(sloId, 99.5, 42L, 200, "/api/health");

        sli.ServiceLevelObjectiveId.Should().Be(sloId);
        sli.Value.Should().Be(99.5);
        sli.IsSuccessful.Should().BeTrue();
        sli.ResponseTimeMs.Should().Be(42);
        sli.StatusCode.Should().Be(200);
        sli.Endpoint.Should().Be("/api/health");
        sli.ErrorMessage.Should().BeNull();
        sli.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateSuccess_WithMinimalParams_ShouldWork()
    {
        var sli = ServiceLevelIndicator.CreateSuccess(Guid.NewGuid(), 100.0);

        sli.IsSuccessful.Should().BeTrue();
        sli.ResponseTimeMs.Should().BeNull();
        sli.StatusCode.Should().BeNull();
        sli.Endpoint.Should().BeNull();
    }

    [Fact]
    public void CreateFailure_ShouldSetAllProperties()
    {
        var sloId = Guid.NewGuid();

        var sli = ServiceLevelIndicator.CreateFailure(sloId, 0.0, "timeout", 5000L, 504, "/api/data");

        sli.ServiceLevelObjectiveId.Should().Be(sloId);
        sli.Value.Should().Be(0.0);
        sli.IsSuccessful.Should().BeFalse();
        sli.ErrorMessage.Should().Be("timeout");
        sli.ResponseTimeMs.Should().Be(5000);
        sli.StatusCode.Should().Be(504);
        sli.Endpoint.Should().Be("/api/data");
    }

    [Fact]
    public void CreateFailure_WithMinimalParams_ShouldWork()
    {
        var sli = ServiceLevelIndicator.CreateFailure(Guid.NewGuid(), 50.0, "error");

        sli.IsSuccessful.Should().BeFalse();
        sli.ErrorMessage.Should().Be("error");
        sli.ResponseTimeMs.Should().BeNull();
    }
}

public class SloViolationExtendedTests
{
    [Fact]
    public void Acknowledge_ShouldSetAllFields()
    {
        var violation = new SloViolation { StartedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var userId = Guid.NewGuid();

        violation.Acknowledge(userId, "investigating");

        violation.IsAcknowledged.Should().BeTrue();
        violation.AcknowledgedByUserId.Should().Be(userId);
        violation.AcknowledgedAt.Should().NotBeNull();
        violation.Notes.Should().Be("investigating");
    }

    [Fact]
    public void Acknowledge_WithoutNotes_ShouldSetNullNotes()
    {
        var violation = new SloViolation { StartedAt = DateTimeOffset.UtcNow };

        violation.Acknowledge(Guid.NewGuid());

        violation.IsAcknowledged.Should().BeTrue();
        violation.Notes.Should().BeNull();
    }

    [Fact]
    public void TriggerAlert_ShouldSetFlagsAndTimestamp()
    {
        var violation = new SloViolation { StartedAt = DateTimeOffset.UtcNow };

        violation.TriggerAlert();

        violation.AlertTriggered.Should().BeTrue();
        violation.AlertSentAt.Should().NotBeNull();
        violation.AlertSentAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Resolve_AlreadyResolved_ShouldNotUpdateEndedAt()
    {
        var originalEnd = DateTimeOffset.UtcNow.AddMinutes(-30);
        var violation = new SloViolation
        {
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndedAt = originalEnd
        };

        violation.Resolve();

        violation.EndedAt.Should().Be(originalEnd);
    }

    [Fact]
    public void GetDuration_Ongoing_ShouldReturnFromStartToNow()
    {
        var violation = new SloViolation { StartedAt = DateTimeOffset.UtcNow.AddHours(-2) };

        violation.GetDuration().TotalHours.Should().BeApproximately(2.0, 0.1);
    }

    [Fact]
    public void DetermineSeverity_BoundaryValues()
    {
        SloViolation.DetermineSeverity(94.0, 99.9).Should().Be(ViolationSeverity.Critical);
        SloViolation.DetermineSeverity(97.5, 99.9).Should().Be(ViolationSeverity.High);
        SloViolation.DetermineSeverity(99.0, 99.9).Should().Be(ViolationSeverity.Medium);
        SloViolation.DetermineSeverity(99.6, 99.9).Should().Be(ViolationSeverity.Low);
    }
}

public class ServiceLevelObjectiveExtendedTests
{
    [Fact]
    public void Enable_FromDisabled_ShouldSetActive()
    {
        var slo = new ServiceLevelObjective();
        slo.Disable();

        slo.Enable();

        slo.IsEnabled.Should().BeTrue();
        slo.Status.Should().Be(SloStatus.Active);
    }

    [Fact]
    public void Enable_FromNonDisabled_ShouldKeepCurrentStatus()
    {
        var slo = new ServiceLevelObjective { Status = SloStatus.Breached, IsEnabled = false };

        slo.Enable();

        slo.IsEnabled.Should().BeTrue();
        slo.Status.Should().Be(SloStatus.Breached);
    }

    [Fact]
    public void Disable_ShouldSetDisabledStatus()
    {
        var slo = new ServiceLevelObjective();

        slo.Disable();

        slo.IsEnabled.Should().BeFalse();
        slo.Status.Should().Be(SloStatus.Disabled);
    }

    [Fact]
    public void UpdateStatus_WhenDisabled_ShouldSetDisabledStatus()
    {
        var slo = new ServiceLevelObjective { IsEnabled = false, TargetPercentage = 99.9 };

        slo.UpdateStatus(99.0);

        slo.Status.Should().Be(SloStatus.Disabled);
    }

    [Fact]
    public void UpdateStatus_AboveTarget_PlentyOfBudget_ShouldBeActive()
    {
        var slo = new ServiceLevelObjective
        {
            IsEnabled = true,
            TargetPercentage = 95.0,
            AlertThresholdPercentage = 50.0
        };

        slo.UpdateStatus(99.5);

        slo.Status.Should().Be(SloStatus.Active);
        slo.RemainingErrorBudget.Should().BeApproximately(90.0, 1.0);
    }

    [Fact]
    public void UpdateStatus_AboveTarget_LowBudget_ShouldBeAtRisk()
    {
        var slo = new ServiceLevelObjective
        {
            IsEnabled = true,
            TargetPercentage = 99.0,
            AlertThresholdPercentage = 50.0
        };

        slo.UpdateStatus(99.5);

        slo.Status.Should().Be(SloStatus.AtRisk);
    }

    [Fact]
    public void UpdateStatus_BelowTarget_ShouldBeBreached()
    {
        var slo = new ServiceLevelObjective { IsEnabled = true, TargetPercentage = 99.9 };

        slo.UpdateStatus(99.0);

        slo.Status.Should().Be(SloStatus.Breached);
    }

    [Fact]
    public void ShouldTriggerAlert_WhenBreached_ReturnsTrue()
    {
        var slo = new ServiceLevelObjective { IsEnabled = true, Status = SloStatus.Breached };

        slo.ShouldTriggerAlert().Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerAlert_WhenDisabled_ReturnsFalse()
    {
        var slo = new ServiceLevelObjective { IsEnabled = false, Status = SloStatus.Breached };

        slo.ShouldTriggerAlert().Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerAlert_AtRisk_WithLowBudget_ReturnsTrue()
    {
        var slo = new ServiceLevelObjective
        {
            IsEnabled = true,
            Status = SloStatus.AtRisk,
            AlertThresholdPercentage = 50.0,
            RemainingErrorBudget = 30.0
        };

        slo.ShouldTriggerAlert().Should().BeTrue();
    }

    [Fact]
    public void CalculateErrorBudget_ShouldSetPercentage()
    {
        var slo = new ServiceLevelObjective { TargetPercentage = 99.9 };

        slo.CalculateErrorBudget();

        slo.ErrorBudgetPercentage.Should().BeApproximately(0.1, 0.001);
    }
}

public class SlaEnumTests
{
    [Theory]
    [InlineData(SloStatus.Active, 0)]
    [InlineData(SloStatus.Breached, 1)]
    [InlineData(SloStatus.AtRisk, 2)]
    [InlineData(SloStatus.Disabled, 3)]
    [InlineData(SloStatus.Violated, 4)]
    [InlineData(SloStatus.Warning, 5)]
    [InlineData(SloStatus.Inactive, 6)]
    public void SloStatus_ShouldHaveCorrectValues(SloStatus status, int expected)
    {
        ((int) status).Should().Be(expected);
    }

    [Theory]
    [InlineData(ViolationSeverity.Low, 0)]
    [InlineData(ViolationSeverity.Medium, 1)]
    [InlineData(ViolationSeverity.High, 2)]
    [InlineData(ViolationSeverity.Critical, 3)]
    public void ViolationSeverity_ShouldHaveCorrectValues(ViolationSeverity severity, int expected)
    {
        ((int) severity).Should().Be(expected);
    }
}

public class CreateSloCommandValidatorTests
{
    private readonly CreateSloCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "API Uptime", "Desc", "api-service", 99.9, 30, 0.1, 50.0);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.Empty, "Test", null, "svc", 99.0, 30, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void EmptyName_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), string.Empty, null, "svc", 99.0, 30, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), new string('A', 201), null, "svc", 99.0, 30, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ServiceNameTooLong_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, new string('A', 201), 99.0, 30, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ServiceName);
    }

    [Fact]
    public void TargetZero_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 0, 30, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TargetPercentage);
    }

    [Fact]
    public void TargetOver100_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 101, 30, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TargetPercentage);
    }

    [Fact]
    public void TimeWindowZero_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 99.0, 0, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TimeWindowDays);
    }

    [Fact]
    public void TimeWindowOver365_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 99.0, 366, 1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TimeWindowDays);
    }

    [Fact]
    public void ErrorBudgetNegative_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 99.0, 30, -1.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ErrorBudgetPercentage);
    }

    [Fact]
    public void ErrorBudgetOver100_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 101.0, 50.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ErrorBudgetPercentage);
    }

    [Fact]
    public void AlertThresholdZero_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 1.0, 0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.AlertThresholdPercentage);
    }

    [Fact]
    public void AlertThresholdOver100_ShouldFail()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 1.0, 101.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.AlertThresholdPercentage);
    }
}

public class UpdateSloCommandValidatorTests
{
    private readonly UpdateSloCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated", "Desc", "svc", 99.9, 30, 0.1, 50.0, true);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyId_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.Empty, Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 1.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.Empty, "Test", null, "svc", 99.0, 30, 1.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void EmptyServiceName_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, string.Empty, 99.0, 30, 1.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ServiceName);
    }

    [Fact]
    public void ServiceNameTooLong_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, new string('A', 201), 99.0, 30, 1.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ServiceName);
    }

    [Fact]
    public void TargetOver100_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, "svc", 101, 30, 1.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TargetPercentage);
    }

    [Fact]
    public void TimeWindowOver365_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, "svc", 99.0, 366, 1.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TimeWindowDays);
    }

    [Fact]
    public void ErrorBudgetOver100_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 101.0, 50.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ErrorBudgetPercentage);
    }

    [Fact]
    public void AlertThresholdZero_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 1.0, 0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.AlertThresholdPercentage);
    }

    [Fact]
    public void AlertThresholdOver100_ShouldFail()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", null, "svc", 99.0, 30, 1.0, 101.0, true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.AlertThresholdPercentage);
    }
}

public class RecordSliMetricCommandValidatorTests
{
    private readonly RecordSliMetricCommandValidator _validator = new();

    [Fact]
    public void ValidSuccessCommand_ShouldPass()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, 99.5, 42);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var command = new RecordSliMetricCommand(Guid.Empty, Guid.NewGuid(), true, 99.5, 42);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void FailureWithoutErrorMessage_ShouldFail()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), false, 0.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ErrorMessage);
    }

    [Fact]
    public void FailureWithErrorMessage_ShouldPass()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), false, 0.0, ErrorMessage: "timeout");

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeValue_ShouldFail()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, -1.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void ValueOver100_ShouldFail()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, 101.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void NegativeResponseTime_ShouldFail()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, 99.0, ResponseTimeMs: -1);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ResponseTimeMs!.Value);
    }

    [Fact]
    public void EmptySloId_ShouldFail()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.Empty, true, 99.0);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ServiceLevelObjectiveId);
    }
}

public class ResolveSloViolationCommandValidatorTests
{
    private readonly ResolveSloViolationCommandValidator _validator = new();

    private sealed record ConcreteResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null)
        : ResolveSloViolationCommand(ViolationId, TenantId, ResolutionNotes);

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new ConcreteResolveSloViolationCommand(Guid.NewGuid(), Guid.NewGuid(), "Fixed");

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyViolationId_ShouldFail()
    {
        var command = new ConcreteResolveSloViolationCommand(Guid.Empty, Guid.NewGuid());

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ViolationId);
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var command = new ConcreteResolveSloViolationCommand(Guid.NewGuid(), Guid.Empty);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void NotesTooLong_ShouldFail()
    {
        var command = new ConcreteResolveSloViolationCommand(Guid.NewGuid(), Guid.NewGuid(), new string('X', 2001));

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ResolutionNotes);
    }
}

public class GetErrorBudgetQueryValidatorTests
{
    private readonly GetErrorBudgetQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        var query = new GetErrorBudgetQuery(Guid.NewGuid(), Guid.NewGuid());

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptySloId_ShouldFail()
    {
        var query = new GetErrorBudgetQuery(Guid.Empty, Guid.NewGuid());

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.SloId);
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var query = new GetErrorBudgetQuery(Guid.NewGuid(), Guid.Empty);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class GetSloByIdQueryValidatorTests
{
    private readonly GetSloByIdQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetSloByIdQuery(Guid.NewGuid(), Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyId_ShouldFail()
    {
        _validator.TestValidate(new GetSloByIdQuery(Guid.Empty, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetSloByIdQuery(Guid.NewGuid(), Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class GetSlosQueryValidatorTests
{
    private readonly GetSlosQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetSlosQuery(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetSlosQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void NegativeSkip_ShouldFail()
    {
        _validator.TestValidate(new GetSlosQuery(Guid.NewGuid(), Skip: -1))
            .ShouldHaveValidationErrorFor(x => x.Skip);
    }

    [Fact]
    public void TakeZero_ShouldFail()
    {
        _validator.TestValidate(new GetSlosQuery(Guid.NewGuid(), Take: 0))
            .ShouldHaveValidationErrorFor(x => x.Take);
    }

    [Fact]
    public void TakeOver1000_ShouldFail()
    {
        _validator.TestValidate(new GetSlosQuery(Guid.NewGuid(), Take: 1001))
            .ShouldHaveValidationErrorFor(x => x.Take);
    }

    [Fact]
    public void ServiceNameTooLong_ShouldFail()
    {
        _validator.TestValidate(new GetSlosQuery(Guid.NewGuid(), ServiceName: new string('A', 101)))
            .ShouldHaveValidationErrorFor(x => x.ServiceName);
    }
}

public class GetSloComplianceQueryValidatorTests
{
    private readonly GetSloComplianceQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptySloId_ShouldFail()
    {
        _validator.TestValidate(new GetSloComplianceQuery(Guid.Empty, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.SloId);
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        _validator.TestValidate(new GetSloComplianceQuery(Guid.NewGuid(), Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void StartDateInFuture_ShouldFail()
    {
        _validator.TestValidate(new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1)))
            .ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void EndDateInFuture_ShouldFail()
    {
        _validator.TestValidate(new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void DateRangeOver365Days_ShouldFail()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-400);
        var end = DateTimeOffset.UtcNow.AddDays(-1);

        _validator.TestValidate(new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), start, end))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndBeforeStart_ShouldFail()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-5);
        var end = DateTimeOffset.UtcNow.AddDays(-10);

        _validator.TestValidate(new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), start, end))
            .IsValid.Should().BeFalse();
    }
}

public class GetSloViolationsQueryValidatorTests
{
    private readonly GetSloViolationsQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        _validator.TestValidate(new GetSloViolationsQuery(SloId: Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeSkip_ShouldFail()
    {
        _validator.TestValidate(new GetSloViolationsQuery(Skip: -1))
            .ShouldHaveValidationErrorFor(x => x.Skip);
    }

    [Fact]
    public void TakeZero_ShouldFail()
    {
        _validator.TestValidate(new GetSloViolationsQuery(Take: 0))
            .ShouldHaveValidationErrorFor(x => x.Take);
    }

    [Fact]
    public void TakeOver1000_ShouldFail()
    {
        _validator.TestValidate(new GetSloViolationsQuery(Take: 1001))
            .ShouldHaveValidationErrorFor(x => x.Take);
    }

    [Fact]
    public void StartDateInFuture_ShouldFail()
    {
        _validator.TestValidate(new GetSloViolationsQuery(StartDate: DateTimeOffset.UtcNow.AddDays(1)))
            .ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void EndDateInFuture_ShouldFail()
    {
        _validator.TestValidate(new GetSloViolationsQuery(EndDate: DateTimeOffset.UtcNow.AddDays(1)))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void EndBeforeStart_ShouldFail()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-5);
        var end = DateTimeOffset.UtcNow.AddDays(-10);

        _validator.TestValidate(new GetSloViolationsQuery(StartDate: start, EndDate: end))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void DateRangeOver365Days_ShouldFail()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-400);
        var end = DateTimeOffset.UtcNow.AddDays(-1);

        _validator.TestValidate(new GetSloViolationsQuery(StartDate: start, EndDate: end))
            .IsValid.Should().BeFalse();
    }
}

public class SlaCommandRecordTests
{
    [Fact]
    public void CreateSloCommand_ShouldStoreAllProperties()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "SLO", "desc", "svc", 99.9, 30, 0.1, 50.0);

        command.Name.Should().Be("SLO");
        command.Description.Should().Be("desc");
        command.ServiceName.Should().Be("svc");
        command.TargetPercentage.Should().Be(99.9);
        command.TimeWindowDays.Should().Be(30);
    }

    [Fact]
    public void UpdateSloCommand_ShouldStoreAllProperties()
    {
        var command = new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated", null, "svc", 99.0, 7, 1.0, 60.0, false);

        command.Name.Should().Be("Updated");
        command.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void DeleteSloCommand_ShouldStore()
    {
        var command = new DeleteSloCommand(Guid.NewGuid(), Guid.NewGuid());

        command.Id.Should().NotBeEmpty();
        command.TenantId.Should().NotBeEmpty();
    }

    [Fact]
    public void RecordSliMetricCommand_ShouldStoreDefaults()
    {
        var command = new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, 99.5);

        command.ResponseTimeMs.Should().BeNull();
        command.StatusCode.Should().BeNull();
        command.Endpoint.Should().BeNull();
        command.Metadata.Should().BeNull();
        command.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void GetSlosQuery_DefaultValues()
    {
        var query = new GetSlosQuery(Guid.NewGuid());

        query.ServiceName.Should().BeNull();
        query.IsEnabled.Should().BeNull();
        query.Skip.Should().Be(0);
        query.Take.Should().Be(50);
    }

    [Fact]
    public void GetSloViolationsQuery_DefaultValues()
    {
        var query = new GetSloViolationsQuery();

        query.SloId.Should().BeNull();
        query.TenantId.Should().BeNull();
        query.OnlyUnresolved.Should().BeFalse();
        query.Skip.Should().Be(0);
        query.Take.Should().Be(50);
    }
}

public class SlaDtoTests
{
    [Fact]
    public void SloDto_ShouldStoreAllFields()
    {
        var dto = new SloDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Uptime",
            ServiceName = "api",
            TargetPercentage = 99.9,
            TimeWindowDays = 30,
            IsEnabled = true,
            Status = SloStatus.Active
        };

        dto.Name.Should().Be("Uptime");
        dto.Status.Should().Be(SloStatus.Active);
    }

    [Fact]
    public void ErrorBudgetDto_ShouldStoreAllFields()
    {
        var dto = new ErrorBudgetDto
        {
            TotalRequests = 10000,
            SuccessfulRequests = 9990,
            FailedRequests = 10,
            AllowedFailures = 100,
            RemainingBudget = 90,
            BurnRate = 0.1,
            IsHealthy = true
        };

        dto.TotalRequests.Should().Be(10000);
        dto.RemainingBudget.Should().Be(90);
    }

    [Fact]
    public void SloViolationDto_IsOngoing_WhenNoEndDate_ShouldBeTrue()
    {
        var dto = new SloViolationDto { EndedAt = null };

        dto.IsOngoing.Should().BeTrue();
    }

    [Fact]
    public void SloViolationDto_IsOngoing_WhenEndDate_ShouldBeFalse()
    {
        var dto = new SloViolationDto { EndedAt = DateTimeOffset.UtcNow };

        dto.IsOngoing.Should().BeFalse();
    }

    [Fact]
    public void SloComplianceDto_ShouldSetDefaults()
    {
        var dto = new SloComplianceDto();

        dto.Name.Should().BeEmpty();
        dto.ServiceName.Should().BeEmpty();
    }

    [Fact]
    public void SloComplianceReportDto_ShouldStoreAllFields()
    {
        var report = new SloComplianceReportDto
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalSlos = 5,
            CompliantSlos = 4,
            ViolatedSlos = 1,
            OverallCompliancePercentage = 80.0,
            SloSummaries = new List<SloComplianceSummaryDto>
            {
                new() { SloName = "uptime", IsCompliant = true }
            }
        };

        report.TotalSlos.Should().Be(5);
        report.SloSummaries.Should().HaveCount(1);
    }

    [Fact]
    public void SloComplianceSummaryDto_ShouldSetDefaults()
    {
        var summary = new SloComplianceSummaryDto();

        summary.SloName.Should().BeEmpty();
        summary.ServiceName.Should().BeEmpty();
        summary.Status.Should().BeEmpty();
    }

    [Fact]
    public void SliMetricDto_ShouldStoreAllFields()
    {
        var dto = new SliMetricDto
        {
            ServiceLevelObjectiveId = Guid.NewGuid(),
            Value = 99.5,
            IsSuccessful = true,
            ResponseTimeMs = 42,
            StatusCode = 200,
            Endpoint = "/api/health",
            Metadata = "{}",
            ErrorMessage = null,
            Timestamp = DateTimeOffset.UtcNow
        };

        dto.Value.Should().Be(99.5);
        dto.ResponseTimeMs.Should().Be(42);
    }
}