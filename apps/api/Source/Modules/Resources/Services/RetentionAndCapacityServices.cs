using GameGuild.Database;
using GameGuild.Modules.Resources.Abstractions;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Services;

/// <summary>
/// Implementation of usage retention and lifecycle management
/// </summary>
public class UsageRetentionService : IUsageRetentionService {
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsageRetentionService> _logger;

    public UsageRetentionService(
        ApplicationDbContext context,
        ILogger<UsageRetentionService> logger) {
        _context = context;
        _logger = logger;
    }

    public async Task<UsageRetentionPolicy> UpsertPolicyAsync(
        UsageRetentionPolicy policy,
        CancellationToken cancellationToken = default) {
        var existing = await _context.Set<UsageRetentionPolicy>()
            .FirstOrDefaultAsync(p => p.Id == policy.Id, cancellationToken);

        if (existing != null) {
            existing.Name = policy.Name;
            existing.RetentionDays = policy.RetentionDays;
            existing.ArchiveAfterDays = policy.ArchiveAfterDays;
            existing.EnableCompaction = policy.EnableCompaction;
            existing.CompactionIntervalDays = policy.CompactionIntervalDays;
            existing.DownSamplingStrategy = policy.DownSamplingStrategy;
            existing.IsActive = policy.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else {
            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;
            _context.Set<UsageRetentionPolicy>().Add(policy);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? policy;
    }

    public async Task<int> ExecutePolicyAsync(Guid policyId, CancellationToken cancellationToken = default) {
        var policy = await _context.Set<UsageRetentionPolicy>()
            .FirstOrDefaultAsync(p => p.Id == policyId && p.IsActive, cancellationToken);

        if (policy == null) {
            _logger.LogWarning("Policy {PolicyId} not found or inactive", policyId);
            return 0;
        }

        _logger.LogInformation("Executing retention policy {PolicyId}: {Name}", policyId, policy.Name);

        var affected = 0;

        // Archive old records
        if (policy.ArchiveAfterDays > 0) {
            var archiveDate = DateTime.UtcNow.AddDays(-policy.ArchiveAfterDays);
            affected += await ArchiveUsageRecordsAsync(policy.TenantId ?? Guid.Empty, archiveDate, cancellationToken);
        }

        // Compact data
        if (policy.EnableCompaction) {
            affected += await CompactUsageDataAsync(
                policy.TenantId ?? Guid.Empty,
                policy.ResourceType,
                policy.DownSamplingStrategy,
                cancellationToken);
        }

        // Delete expired
        affected += await DeleteExpiredRecordsAsync(policyId, cancellationToken);

        policy.LastExecutedAt = DateTime.UtcNow;
        policy.NextExecutionAt = CalculateNextExecution(policy);
        await _context.SaveChangesAsync(cancellationToken);

        return affected;
    }

    public async Task<int> ArchiveUsageRecordsAsync(
        Guid tenantId,
        DateTime olderThan,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Archiving usage records for tenant {TenantId} older than {Date}",
            tenantId, olderThan);

        // In real implementation, move to archive table or cold storage
        var recordsToArchive = await _context.Set<ResourceUsageRecord>()
            .Where(r => r.TenantId == tenantId && r.RecordedAt < olderThan)
            .CountAsync(cancellationToken);

        _logger.LogInformation("Archived {Count} usage records", recordsToArchive);
        return recordsToArchive;
    }

    public async Task<int> CompactUsageDataAsync(
        Guid tenantId,
        ResourceUsageType? resourceType,
        string samplingStrategy,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Compacting usage data for tenant {TenantId}, strategy {Strategy}",
            tenantId, samplingStrategy);

        var query = _context.Set<ResourceUsageRecord>()
            .Where(r => r.TenantId == tenantId);

        if (resourceType.HasValue)
            query = query.Where(r => r.UsageType == resourceType.Value);

        var records = await query.ToListAsync(cancellationToken);

        // Group and downsample based on strategy
        var compacted = samplingStrategy.ToLower() switch {
            "hourly" => records.GroupBy(r => new { r.TenantId, r.UsageType, Hour = r.RecordedAt.Hour }),
            "daily" => records.GroupBy(r => new { r.TenantId, r.UsageType, Date = r.RecordedAt.Date }),
            "weekly" => records.GroupBy(r => new { r.TenantId, r.UsageType, Week = r.RecordedAt.DayOfYear / 7 }),
            _ => null
        };

        if (compacted != null) {
            _logger.LogInformation("Compacted {Original} records to {Compacted} aggregates",
                records.Count, compacted.Count());
            return records.Count - compacted.Count();
        }

        return 0;
    }

    public async Task<int> DeleteExpiredRecordsAsync(Guid policyId, CancellationToken cancellationToken = default) {
        var policy = await _context.Set<UsageRetentionPolicy>()
            .FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);

        if (policy == null) return 0;

        var deleteDate = DateTime.UtcNow.AddDays(-policy.RetentionDays);

        var query = _context.Set<ResourceUsageRecord>()
            .Where(r => r.RecordedAt < deleteDate);

        if (policy.TenantId.HasValue)
            query = query.Where(r => r.TenantId == policy.TenantId.Value);

        if (policy.ResourceType.HasValue)
            query = query.Where(r => r.UsageType == policy.ResourceType.Value);

        var count = await query.CountAsync(cancellationToken);

        // Keep minimum records
        if (count <= policy.MinimumRecordsToKeep) {
            _logger.LogInformation("Skipping deletion: {Count} records <= minimum {Min}",
                count, policy.MinimumRecordsToKeep);
            return 0;
        }

        await query.ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Deleted {Count} expired usage records", count);
        return count;
    }

    public async Task<List<UsageRetentionPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default) {
        return await _context.Set<UsageRetentionPolicy>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    private DateTime CalculateNextExecution(UsageRetentionPolicy policy) {
        return policy.ExecutionFrequency.ToLower() switch {
            "daily" => DateTime.UtcNow.AddDays(1),
            "weekly" => DateTime.UtcNow.AddDays(7),
            "monthly" => DateTime.UtcNow.AddMonths(1),
            _ => DateTime.UtcNow.AddDays(1)
        };
    }
}

/// <summary>
/// Implementation of reserved capacity service
/// </summary>
public class ReservedCapacityService : IReservedCapacityService {
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReservedCapacityService> _logger;

    private readonly Dictionary<int, decimal> _discountRates = new()
    {
        { 1, 0.00m },    // No discount for 1 month
        { 12, 0.15m },   // 15% discount for 1 year
        { 36, 0.30m }    // 30% discount for 3 years
    };

    public ReservedCapacityService(
        ApplicationDbContext context,
        ILogger<ReservedCapacityService> logger) {
        _context = context;
        _logger = logger;
    }

    public async Task<ReservedCapacity> CreateReservationAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        long quantity,
        int commitmentMonths,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Creating reservation for tenant {TenantId}: {Quantity} {ResourceType} for {Months} months",
            tenantId, quantity, resourceType, commitmentMonths);

        var standardPrice = await GetStandardPriceAsync(resourceType);
        var discountRate = _discountRates.GetValueOrDefault(commitmentMonths, 0.10m);
        var discountedPrice = standardPrice * (1 - discountRate);

        var reservation = new ReservedCapacity {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceType = resourceType,
            ReservedQuantity = quantity,
            CommitmentTermMonths = commitmentMonths,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(commitmentMonths),
            StandardPricePerUnit = standardPrice,
            DiscountedPricePerUnit = discountedPrice,
            DiscountPercentage = discountRate * 100,
            TotalCommitmentAmount = discountedPrice * quantity,
            ConsumedAmount = 0,
            ConsumedUnits = 0,
            AutoRenew = false,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<ReservedCapacity>().Add(reservation);
        await _context.SaveChangesAsync(cancellationToken);

        return reservation;
    }

    public async Task<bool> ConsumeReservedUnitsAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        long units,
        CancellationToken cancellationToken = default) {
        var reservation = await _context.Set<ReservedCapacity>()
            .Where(r => r.TenantId == tenantId &&
                        r.ResourceType == resourceType &&
                        r.Status == "Active" &&
                        r.EndDate > DateTime.UtcNow &&
                        r.ConsumedUnits + units <= r.ReservedQuantity)
            .OrderBy(r => r.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (reservation == null)
            return false;

        reservation.ConsumedUnits += units;
        reservation.ConsumedAmount = reservation.ConsumedUnits * reservation.DiscountedPricePerUnit;
        reservation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Consumed {Units} reserved units for tenant {TenantId}",
            units, tenantId);

        return true;
    }

    public async Task<List<ReservedCapacity>> GetActiveReservationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) {
        return await _context.Set<ReservedCapacity>()
            .Where(r => r.TenantId == tenantId &&
                        r.Status == "Active" &&
                        r.EndDate > DateTime.UtcNow)
            .OrderBy(r => r.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> CalculateDiscountedPriceAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        long units,
        CancellationToken cancellationToken = default) {
        var reservation = await _context.Set<ReservedCapacity>()
            .Where(r => r.TenantId == tenantId &&
                        r.ResourceType == resourceType &&
                        r.Status == "Active" &&
                        r.EndDate > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

        if (reservation != null && reservation.ConsumedUnits + units <= reservation.ReservedQuantity) {
            return units * reservation.DiscountedPricePerUnit;
        }

        var standardPrice = await GetStandardPriceAsync(resourceType);
        return units * standardPrice;
    }

    public async Task<ReservedCapacity> RenewReservationAsync(
        Guid reservationId,
        int newCommitmentMonths,
        CancellationToken cancellationToken = default) {
        var existing = await _context.Set<ReservedCapacity>()
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (existing == null)
            throw new InvalidOperationException("Reservation not found");

        return await CreateReservationAsync(
            existing.TenantId!.Value,
            existing.ResourceType,
            existing.ReservedQuantity,
            newCommitmentMonths,
            cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetUtilizationReportAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default) {
        var reservation = await _context.Set<ReservedCapacity>()
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (reservation == null)
            return new Dictionary<string, object>();

        var utilizationPercent = reservation.ReservedQuantity > 0
            ? (reservation.ConsumedUnits / (double)reservation.ReservedQuantity) * 100
            : 0;

        return new Dictionary<string, object> {
            ["ReservationId"] = reservation.Id,
            ["TenantId"] = reservation.TenantId,
            ["ResourceType"] = reservation.ResourceType.ToString(),
            ["ReservedQuantity"] = reservation.ReservedQuantity,
            ["ConsumedUnits"] = reservation.ConsumedUnits,
            ["RemainingUnits"] = reservation.ReservedQuantity - reservation.ConsumedUnits,
            ["UtilizationPercent"] = utilizationPercent,
            ["TotalCommitmentAmount"] = reservation.TotalCommitmentAmount,
            ["ConsumedAmount"] = reservation.ConsumedAmount,
            ["DiscountPercentage"] = reservation.DiscountPercentage,
            ["DaysRemaining"] = (reservation.EndDate - DateTime.UtcNow).Days
        };
    }

    private Task<decimal> GetStandardPriceAsync(ResourceUsageType resourceType) {
        // In real implementation, would query pricing table
        return Task.FromResult(resourceType switch {
            ResourceUsageType.Compute => 0.10m,
            ResourceUsageType.Storage => 0.05m,
            ResourceUsageType.Bandwidth => 0.02m,
            ResourceUsageType.ApiCalls => 0.001m,
            ResourceUsageType.Database => 0.15m,
            ResourceUsageType.Users => 5.00m,
            _ => 0.01m
        });
    }
}
