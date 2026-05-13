using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using GameGuild.Monitoring.SLA.UnitTests.Infrastructure;

using Xunit;

using static GameGuild.Monitoring.SLA.UnitTests.Repositories.SlaRepositoryTestData;

namespace GameGuild.Monitoring.SLA.UnitTests.Repositories;

public class ServiceLevelObjectiveRepositoryTests
{
    [Fact]
    public async Task Repository_ShouldHandleQueryAndPersistenceOperations()
    {
        await using var context = CreateContext();
        var repository = new TestServiceLevelObjectiveRepository(context);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var primary = CreateSlo(tenantId, "API", "api", true, SloStatus.Active);
        var secondary = CreateSlo(tenantId, "Billing", "billing", false, SloStatus.Disabled);
        var foreign = CreateSlo(otherTenantId, "API", "api", true, SloStatus.Breached);
        primary.Indicators.Add(new ServiceLevelIndicator { ServiceLevelObjectiveId = primary.Id, Timestamp = DateTimeOffset.UtcNow, Value = 99.9, IsSuccessful = true });
        primary.Violations.Add(new SloViolation { ServiceLevelObjectiveId = primary.Id, StartedAt = DateTimeOffset.UtcNow.AddHours(-1), ActualValue = 98, TargetValue = 99.9, Severity = ViolationSeverity.High });
        context.AddRange(primary, secondary, foreign);
        await context.SaveChangesAsync();

        (await repository.GetByIdAsync(primary.Id)).Should().NotBeNull();
        (await repository.GetByIdWithIndicatorsAsync(primary.Id))!.Indicators.Should().ContainSingle();
        (await repository.GetByIdWithViolationsAsync(primary.Id))!.Violations.Should().ContainSingle();
        (await repository.GetByTenantIdAsync(tenantId)).Should().HaveCount(2);
        (await repository.GetByServiceNameAsync("api", tenantId)).Should().ContainSingle();
        (await repository.GetEnabledSlosAsync(tenantId)).Should().ContainSingle();
        (await repository.GetByStatusAsync(SloStatus.Disabled, tenantId)).Should().ContainSingle();
        (await repository.ExistsByNameAsync("API", tenantId)).Should().BeTrue();
        (await repository.ExistsByNameAsync("Missing", tenantId)).Should().BeFalse();
        (await repository.GetAllSlosAsync()).Should().HaveCount(3);

        var added = await repository.AddAsync(CreateSlo(tenantId, "Search", "search", true, SloStatus.Active));
        added.Name = "Search 2";
        await repository.UpdateAsync(added);
        (await repository.GetByIdAsync(added.Id))!.Name.Should().Be("Search 2");

        await repository.DeleteAsync(added.Id);
        await repository.DeleteAsync(Guid.NewGuid());

        (await repository.GetByIdAsync(added.Id)).Should().BeNull();
    }
}

public class ServiceLevelIndicatorRepositoryTests
{
    [Fact]
    public async Task Repository_ShouldFilterCountAndDeleteMetrics()
    {
        await using var context = CreateContext();
        var repository = new TestServiceLevelIndicatorRepository(context);
        var sloId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddHours(-3);
        var metrics = new List<ServiceLevelIndicator>
        {
            CreateIndicator(sloId, true, baseTime.AddHours(-1), "/api", 1),
            CreateIndicator(sloId, false, baseTime.AddHours(0), "/api", 2),
            CreateIndicator(sloId, true, baseTime.AddHours(1), "/health", 3),
            CreateIndicator(Guid.NewGuid(), true, baseTime.AddHours(2), "/other", 4)
        };
        context.AddRange(metrics);
        await context.SaveChangesAsync();

        (await repository.GetByIdAsync(metrics[0].Id)).Should().NotBeNull();
        (await repository.GetBySloIdAsync(sloId)).Should().HaveCount(3);
        (await repository.GetBySloIdAndTimeRangeAsync(sloId, baseTime.AddMinutes(-30), baseTime.AddHours(1.5))).Should().HaveCount(2);
        (await repository.GetSuccessfulCountAsync(sloId, baseTime.AddHours(-2), baseTime.AddHours(2))).Should().Be(2);
        (await repository.GetTotalCountAsync(sloId, baseTime.AddHours(-2), baseTime.AddHours(2))).Should().Be(3);
        (await repository.GetByEndpointAsync(sloId, "/api", baseTime.AddHours(-2), baseTime.AddHours(2))).Should().HaveCount(2);
        (await repository.GetRecentAsync(2)).Should().HaveCount(2);
        (await repository.GetSuccessfulAsync(sloId)).Should().HaveCount(2);
        (await repository.GetFailedAsync(sloId)).Should().ContainSingle();
        (await repository.CountAsync(sloId)).Should().Be(3);
        (await repository.CountSuccessfulAsync(sloId)).Should().Be(2);
        (await repository.CountFailedAsync(sloId)).Should().Be(1);

        var added = await repository.AddAsync(CreateIndicator(sloId, true, DateTimeOffset.UtcNow, "/new", 5));
        await repository.AddRangeAsync([
            CreateIndicator(sloId, true, DateTimeOffset.UtcNow.AddMinutes(1), "/new", 6),
            CreateIndicator(sloId, false, DateTimeOffset.UtcNow.AddMinutes(2), "/new", 7)
        ]);
        added.Id.Should().NotBeEmpty();

        await repository.DeleteOlderThanAsync(baseTime.AddMinutes(30));

        (await repository.CountAsync(sloId)).Should().Be(4);
    }
}

public class SloViolationRepositoryTests
{
    [Fact]
    public async Task Repository_ShouldFilterAndPersistViolations()
    {
        await using var context = CreateContext();
        var repository = new TestSloViolationRepository(context);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var sloOneId = Guid.NewGuid();
        var sloTwoId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddHours(-4);
        var ongoing = CreateViolation(tenantId, sloOneId, baseTime.AddHours(3), null, ViolationSeverity.High, acknowledged: false, alertTriggered: true);
        var resolved = CreateViolation(tenantId, sloOneId, baseTime.AddHours(2), baseTime.AddHours(2.5), ViolationSeverity.Low, acknowledged: true, alertTriggered: false);
        var otherTenant = CreateViolation(otherTenantId, sloTwoId, baseTime.AddHours(1), null, ViolationSeverity.Critical, acknowledged: false, alertTriggered: true);
        context.AddRange(ongoing, resolved, otherTenant);
        await context.SaveChangesAsync();

        (await repository.GetByIdAsync(ongoing.Id)).Should().NotBeNull();
        (await repository.GetBySloIdAsync(sloOneId)).Should().HaveCount(2);
        (await repository.GetBySloIdAndTimeRangeAsync(sloOneId, baseTime.AddHours(1.5), baseTime.AddHours(3.5))).Should().HaveCount(2);
        (await repository.GetByTenantIdAsync(tenantId)).Should().HaveCount(2);
        (await repository.GetOngoingViolationsAsync(sloOneId)).Should().ContainSingle();
        (await repository.GetAllOngoingViolationsAsync()).Should().HaveCount(2);
        (await repository.GetAllOngoingViolationsAsync(tenantId)).Should().ContainSingle();
        (await repository.GetBySeverityAsync(ViolationSeverity.High, tenantId)).Should().ContainSingle();
        (await repository.GetUnacknowledgedAsync(tenantId)).Should().ContainSingle();
        (await repository.GetWithAlertsAsync(tenantId)).Should().ContainSingle();
        (await repository.CountViolationsAsync(sloOneId, baseTime, DateTimeOffset.UtcNow)).Should().Be(2);

        var added = await repository.AddAsync(CreateViolation(tenantId, sloTwoId, DateTimeOffset.UtcNow, null, ViolationSeverity.Medium, acknowledged: false, alertTriggered: false));
        added.Notes = "updated";
        await repository.UpdateAsync(added);

        (await repository.GetByIdAsync(added.Id))!.Notes.Should().Be("updated");
    }
}

internal sealed class TestServiceLevelObjectiveRepository(DbContext context) : ServiceLevelObjectiveRepository(context);

internal sealed class TestServiceLevelIndicatorRepository(DbContext context) : ServiceLevelIndicatorRepository(context);

internal sealed class TestSloViolationRepository(DbContext context) : SloViolationRepository(context);

file static class SlaRepositoryTestData
{
    public static SlaMonitoringTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SlaMonitoringTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SlaMonitoringTestDbContext(options);
    }

    public static ServiceLevelObjective CreateSlo(Guid tenantId, string name, string serviceName, bool isEnabled, SloStatus status)
    {
        var slo = new ServiceLevelObjective
        {
            Id = Guid.NewGuid(),
            Name = name,
            ServiceName = serviceName,
            TargetPercentage = 99.9,
            TimeWindowDays = 30,
            ErrorBudgetPercentage = 0.1,
            AlertThresholdPercentage = 50,
            IsEnabled = isEnabled,
            Status = status
        };
        slo.SetTenantId(tenantId);

        return slo;
    }

    public static ServiceLevelIndicator CreateIndicator(Guid sloId, bool success, DateTimeOffset timestamp, string endpoint, int ordinal)
    {
        return new ServiceLevelIndicator
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = sloId,
            Timestamp = timestamp,
            Value = success ? 99.0 + ordinal : 0,
            IsSuccessful = success,
            Endpoint = endpoint,
            ErrorMessage = success ? null : "failed"
        };
    }

    public static SloViolation CreateViolation(Guid tenantId, Guid sloId, DateTimeOffset startedAt, DateTimeOffset? endedAt, ViolationSeverity severity, bool acknowledged, bool alertTriggered)
    {
        var violation = new SloViolation
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = sloId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = severity,
            IsAcknowledged = acknowledged,
            AlertTriggered = alertTriggered,
            Description = "violation"
        };
        violation.SetTenantId(tenantId);

        return violation;
    }
}
