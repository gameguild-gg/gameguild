# Coverage Threshold Implementation Report

## Summary

This report documents the implementation of the **Dual Coverage Methodology** across all test projects in the GameGuild API solution. The methodology ensures consistent code coverage tracking and enforcement across all modules.

## Methodology Overview

### Two-Step Workflow

1. **Step 1 - Collect Coverage**: Run tests excluding threshold tests to generate Cobertura XML report
   ```bash
   dotnet test --collect:"XPlat Code Coverage" --filter "Category!=CoverageThreshold"
   ```

2. **Step 2 - Validate Coverage**: Run threshold tests to validate coverage meets requirements
   ```bash
   dotnet test --filter "Category=CoverageThreshold"
   ```

### Features Per Module

Each `CoverageThresholdTests.cs` file includes:
- **Module Line Coverage Test**: Validates minimum line coverage threshold
- **Module Branch Coverage Test**: Validates minimum branch coverage threshold  
- **Critical Classes Test**: Validates 100% coverage on designated critical classes
- **Detailed Report Generation**: Outputs coverage metrics for debugging

### Threshold Configuration

| Test Type | Line Threshold | Branch Threshold |
|-----------|---------------|------------------|
| Unit Tests | 20.0% | 15.0% |
| Integration Tests | 15.0% | 10.0% |

*Note: Subscriptions module has higher thresholds (30% line, 40% branch) with critical classes at 100%*

---

## Implementation Status

### Unit Test Projects (18 modules) ✅

| # | Module | Namespace | File Created |
|---|--------|-----------|--------------|
| 1 | API | `GameGuild.API` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 2 | Assets | `GameGuild.Assets` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 3 | Audit | `GameGuild.Compliance.Audit` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 4 | Commerce.Billing | `GameGuild.Commerce.Billing` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 5 | Commerce.Orders | `GameGuild.Commerce.Orders` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 6 | Commerce.Payments | `GameGuild.Commerce.Payments` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 7 | Commerce.Products | `GameGuild.Commerce.Products` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 8 | Commerce.Subscriptions | `GameGuild.Commerce.Subscriptions` | ✅ **EXISTING** (100% critical class coverage) |
| 9 | Contents | `GameGuild.Contents` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 10 | Features | `GameGuild.Features` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 11 | Identity.Authentication | `GameGuild.Identity.Authentication` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 12 | Identity.Authorization | `GameGuild.Identity.Authorization` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 13 | Identity.Tenants | `GameGuild.Identity.Tenants` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 14 | Identity.Users | `GameGuild.Identity.Users` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 15 | Localization | `GameGuild.Localization` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 16 | Permissions | `GameGuild.Permissions` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 17 | Projects | `GameGuild.Projects` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 18 | Resources | `GameGuild.Resources` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 19 | SharedKernel | `GameGuild.SharedKernel` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 20 | UserProfiles | `GameGuild.UserProfiles` | ✅ `Coverage/CoverageThresholdTests.cs` |

### Integration Test Projects (17 modules) ✅

| # | Module | Namespace | File Created |
|---|--------|-----------|--------------|
| 1 | API | `GameGuild.API` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 2 | Assets | `GameGuild.Assets` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 3 | Commerce.Billing | `GameGuild.Commerce.Billing` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 4 | Commerce.Orders | `GameGuild.Commerce.Orders` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 5 | Commerce.Payments | `GameGuild.Commerce.Payments` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 6 | Commerce.Products | `GameGuild.Commerce.Products` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 7 | Commerce.Subscriptions | `GameGuild.Commerce.Subscriptions` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 8 | Contents | `GameGuild.Contents` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 9 | Identity.Authentication | `GameGuild.Identity.Authentication` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 10 | Identity.Authorization | `GameGuild.Identity.Authorization` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 11 | Identity.Tenants | `GameGuild.Identity.Tenants` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 12 | Identity.Users | `GameGuild.Identity.Users` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 13 | Permissions | `GameGuild.Permissions` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 14 | Projects | `GameGuild.Projects` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 15 | Resources | `GameGuild.Resources` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 16 | SharedKernel | `GameGuild.SharedKernel` | ✅ `Coverage/CoverageThresholdTests.cs` |
| 17 | UserProfiles | `GameGuild.UserProfiles` | ✅ `Coverage/CoverageThresholdTests.cs` |

---

## Total Files Created

| Category | Count |
|----------|-------|
| Unit Test Coverage Files | **19** |
| Integration Test Coverage Files | **17** |
| **Total** | **36** |

---

## Usage Instructions

### Running All Coverage Tests

```bash
# Navigate to API directory
cd apps/api

# Step 1: Generate coverage report (excludes threshold tests)
dotnet test --collect:"XPlat Code Coverage" --filter "Category!=CoverageThreshold"

# Step 2: Validate coverage thresholds
dotnet test --filter "Category=CoverageThreshold"
```

### Running Coverage for Specific Module

```bash
# Example: Run Subscriptions unit tests with coverage
dotnet test GameGuild.Commerce.Subscriptions.UnitTests --collect:"XPlat Code Coverage" --filter "Category!=CoverageThreshold"

# Validate thresholds
dotnet test GameGuild.Commerce.Subscriptions.UnitTests --filter "Category=CoverageThreshold"
```

### Viewing Detailed Reports

Run the `GenerateDetailedCoverageReport` test to output coverage details:

```bash
dotnet test --filter "FullyQualifiedName~GenerateDetailedCoverageReport" -v n
```

---

## Configuring Critical Classes

To enforce 100% coverage on critical domain entities, edit the `CriticalClassThresholds` dictionary in each module's `CoverageThresholdTests.cs`:

```csharp
private static readonly Dictionary<string, double> CriticalClassThresholds = new()
{
    { "Subscription", 100.0 },
    { "SubscriptionPlan", 100.0 },
    { "BillingCalculator", 100.0 },
    // Add more critical classes
};
```

---

## Best Practices

1. **Run coverage after changes**: Always regenerate coverage report after code changes
2. **Increase thresholds over time**: Gradually increase module thresholds as coverage improves
3. **Mark critical classes**: Add business-critical entities to `CriticalClassThresholds`
4. **Exclude untestable code**: Use `[ExcludeFromCodeCoverage]` for generated code or framework-specific code
5. **CI Integration**: Add both steps to CI pipeline to enforce coverage gates

---

## Report Generated

**Date**: 2025-01-10  
**Total Modules Covered**: 36 (19 Unit + 17 Integration)  
**Methodology**: Dual Coverage Workflow with Cobertura XML Parsing
