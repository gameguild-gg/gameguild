# Resources Module - Performance Baseline Documentation

## Overview

This document establishes the performance baseline for the GameGuild Resources module, including quota management, usage tracking, and resource allocation operations. It serves as the reference point for future performance comparisons and regression detection.

**Last Updated**: 2025-01-XX
**Module Version**: 1.0.0
**Test Environment**: .NET 9.0, PostgreSQL 15+, EF Core 9.0

---

## Performance Requirements

### Service Level Objectives (SLOs)

| Operation | Target Latency (p50) | Target Latency (p99) | Max Latency | Throughput |
|-----------|---------------------|---------------------|-------------|------------|
| `TryAtomicConsumeAsync` | ≤ 10ms | ≤ 50ms | ≤ 100ms | 1,000 ops/sec |
| `CheckLimitsAsync` | ≤ 5ms | ≤ 25ms | ≤ 50ms | 5,000 ops/sec |
| `GetQuotaAsync` | ≤ 5ms | ≤ 20ms | ≤ 50ms | 10,000 ops/sec |
| `SetQuotaAsync` | ≤ 20ms | ≤ 100ms | ≤ 200ms | 500 ops/sec |
| `GetTenantQuotasAsync` | ≤ 10ms | ≤ 50ms | ≤ 100ms | 2,000 ops/sec |

### Concurrency Requirements

| Scenario | Target | Notes |
|----------|--------|-------|
| Concurrent quota consume operations | 1,000 ops | Same tenant, same resource type |
| Multi-tenant parallel operations | 100 tenants × 100 ops | Cross-tenant isolation verification |
| Quota exceeded storm | 10,000 failures/min | Graceful degradation |

---

## Baseline Measurements

### Test Configuration

```yaml
Environment:
  CPU: 8 cores
  Memory: 16GB
  Database: PostgreSQL 15 (local Docker)
  Connection Pool: Min=10, Max=100
  Caching: Memory cache enabled

Test Parameters:
  Warm-up Iterations: 100
  Measurement Iterations: 1000
  Concurrent Users: Varies by test
```

### Single Operation Latency

| Operation | p50 | p75 | p90 | p95 | p99 | Max |
|-----------|-----|-----|-----|-----|-----|-----|
| `TryAtomicConsumeAsync` | TBD | TBD | TBD | TBD | TBD | TBD |
| `CheckLimitsAsync` (cached) | TBD | TBD | TBD | TBD | TBD | TBD |
| `CheckLimitsAsync` (uncached) | TBD | TBD | TBD | TBD | TBD | TBD |
| `GetQuotaAsync` (cached) | TBD | TBD | TBD | TBD | TBD | TBD |
| `GetQuotaAsync` (uncached) | TBD | TBD | TBD | TBD | TBD | TBD |
| `SetQuotaAsync` | TBD | TBD | TBD | TBD | TBD | TBD |

### Concurrent Operation Performance

| Scenario | Throughput (ops/sec) | Error Rate | p99 Latency |
|----------|---------------------|------------|-------------|
| 100 concurrent consumers | TBD | TBD | TBD |
| 500 concurrent consumers | TBD | TBD | TBD |
| 1000 concurrent consumers | TBD | TBD | TBD |
| Multi-tenant (100×100) | TBD | TBD | TBD |

---

## Test Scenarios

### 1. Concurrent Quota Consumption (ConcurrentQuotaConsume_1000Operations)

**Purpose**: Verify hard limit enforcement under high concurrency.

**Setup**:
- Single tenant with hard limit of 1000 units
- 1000 concurrent consumption requests for 1 unit each

**Expected Behavior**:
- Exactly 1000 successful consumptions (up to hard limit)
- All operations exceeding limit return `Success = false`
- No race conditions or over-consumption

**Metrics Captured**:
- Total execution time
- Success/failure distribution
- Final usage count accuracy

### 2. Multi-Tenant Isolation (MultiTenantQuotaOperations_IsolatedCorrectly)

**Purpose**: Verify tenant isolation under concurrent load.

**Setup**:
- 10 tenants, each with hard limit of 100 units
- 100 concurrent requests per tenant (1000 total)

**Expected Behavior**:
- Each tenant's final usage = 100 (exactly at limit)
- No cross-tenant interference
- All operations complete successfully

**Metrics Captured**:
- Per-tenant usage accuracy
- Cross-tenant isolation verification
- Aggregate throughput

### 3. Quota Exceeded Storm (QuotaExceededStorm_HandledGracefully)

**Purpose**: Verify system stability under sustained quota violations.

**Setup**:
- Tenant at 100% quota utilization
- 1000 concurrent attempts to exceed quota

**Expected Behavior**:
- All operations return `Success = false`
- No exceptions thrown
- QuotaExceededEvent published for each failure
- Alert handler processes events without backpressure

**Metrics Captured**:
- Failure handling latency
- Event publishing throughput
- Memory stability (no leaks)

---

## Observability Configuration

### OpenTelemetry Tracing

The following ActivitySource is available for distributed tracing:

```csharp
ActivitySource: "GameGuild.Resources.Quota" (v1.0.0)
```

**Traced Operations**:

| Operation Name | Description |
|---------------|-------------|
| `quota.set` | SetQuotaAsync - create/update quota |
| `quota.get` | GetQuotaAsync - retrieve quota |
| `quota.delete` | DeleteQuotaAsync - remove quota |
| `quota.check_limits` | CheckLimitsAsync - advisory limit check |
| `quota.consume` | TryConsumeResourceAsync - consume with enforcement |
| `quota.atomic_consume` | TryAtomicConsumeAsync - core atomic operation |
| `quota.decrement` | DecrementUsageAsync - release resources |
| `quota.reset_expired` | ResetExpiredQuotasAsync - periodic reset |
| `quota.recalculate` | RecalculateUsageAsync - usage reconciliation |

**Standard Tags**:

| Tag | Description |
|-----|-------------|
| `tenant.id` | Tenant GUID |
| `resource.type` | ResourceUsageType enum value |
| `quota.current_usage` | Current usage count |
| `quota.hard_limit` | Hard limit value or "unlimited" |
| `quota.soft_limit` | Soft limit value or "unlimited" |
| `quota.requested_amount` | Amount requested for consume |
| `quota.success` | Whether operation succeeded |
| `quota.result` | Result type: consumed, exceeded, unlimited |

### Alerts ActivitySource

```csharp
ActivitySource: "GameGuild.Resources.Alerts" (v1.0.0)
```

**Traced Operations**:

| Operation Name | Description |
|---------------|-------------|
| `quota.alert.exceeded` | Alert processing for quota exceeded events |

**Alert Tags**:

| Tag | Description |
|-----|-------------|
| `alert.severity` | warning or error |
| `alert.violation_count` | Violations in current window |
| `alert.is_repeated` | Whether this is a repeated violation |

### Structured Logging

Alerts are logged with structured format for log aggregation:

```
QUOTA_EXCEEDED: Tenant {TenantId} exceeded {ResourceType} quota...
QUOTA_EXCEEDED_REPEATED: Tenant {TenantId} has exceeded {ResourceType} quota {ViolationCount} times...
```

**Recommended Alert Rules**:

| Metric/Log Pattern | Threshold | Action |
|-------------------|-----------|--------|
| `QUOTA_EXCEEDED` count | > 100/min per tenant | Warning notification |
| `QUOTA_EXCEEDED_REPEATED` | Any occurrence | PagerDuty alert |
| `quota.atomic_consume` p99 | > 100ms | Performance degradation alert |
| `quota.result=exceeded` rate | > 10% of requests | Capacity planning alert |

---

## Running Performance Tests

### Prerequisites

1. Docker running (for PostgreSQL)
2. .NET 9 SDK installed
3. NBomber and BenchmarkDotNet packages

### Quick Test Run

```bash
cd apps/api/Tests/GameGuild.Resources.PerformanceTests
dotnet test --filter "FullyQualifiedName~QuotaConcurrencyLoadTests"
```

### Full Benchmark Suite

```bash
cd apps/api/Tests/GameGuild.Resources.PerformanceTests
dotnet run -c Release -- --job short
```

### Continuous Integration

Performance tests are marked with `[Trait("Category", "Performance")]` and excluded from standard CI runs. Include in nightly builds:

```bash
dotnet test --filter "Category=Performance"
```

---

## Regression Detection

### Baseline Comparison

After running tests, compare results against this baseline:

1. **Latency Regression**: If p99 increases > 20%, investigate
2. **Throughput Regression**: If ops/sec decreases > 10%, investigate
3. **Error Rate**: If error rate > 0.1% for non-exceeded operations, investigate

### Common Causes of Regression

| Symptom | Possible Cause | Remediation |
|---------|---------------|-------------|
| Increased p99 latency | Connection pool exhaustion | Increase pool size |
| Decreased throughput | Database lock contention | Review index usage |
| Memory growth | Event handler backpressure | Add async queue |
| Inconsistent results | Cache invalidation issues | Review cache TTL |

---

## Capacity Planning

### Resource Scaling Guidelines

| Tenants | Quota Operations/sec | Recommended DB Connections | Memory Cache Size |
|---------|---------------------|---------------------------|-------------------|
| 100 | 1,000 | 20 | 256MB |
| 1,000 | 10,000 | 50 | 1GB |
| 10,000 | 100,000 | 100 | 4GB |

### Database Optimization

Ensure these indexes exist for optimal performance:

```sql
-- Primary lookup index
CREATE INDEX IX_ResourceQuotas_TenantId_Type ON ResourceQuotas (TenantId, Type) WHERE IsDeleted = false;

-- Usage history queries
CREATE INDEX IX_UsageRecords_TenantId_Type_PeriodStart ON UsageRecords (TenantId, Type, PeriodStart DESC);

-- Expired quota reset job
CREATE INDEX IX_ResourceQuotas_Period_LastReset ON ResourceQuotas (Period, LastReset) WHERE IsActive = true;
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-XX | Initial baseline documentation |

---

## Appendix: Test Code Location

- Load Tests: `apps/api/Tests/GameGuild.Resources.PerformanceTests/QuotaConcurrencyLoadTests.cs`
- Integration Tests: `apps/api/Tests/GameGuild.Resources.IntegrationTests/`
- Unit Tests: `apps/api/Tests/GameGuild.Resources.UnitTests/`
