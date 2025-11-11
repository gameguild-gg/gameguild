# Authentication Integration Tests - Compilation Fixes Required

## Overview
The integration tests have 220+ compilation errors due to API mismatches with the actual implementation. This document provides a comprehensive guide to fix all errors.

## Error Categories

### 1. IRoleRepository Method Signatures (60+ errors)

**Problem**: Tests call `CreateAsync()` but interface has `AddAsync()`
**Problem**: Tests call `AssignRoleToUserAsync(userId, roleId, assignedBy)` but interface expects `AssignRoleToUserAsync(UserRole userRole)`

**Fix Pattern**:
```csharp
// WRONG:
await _roleRepository.CreateAsync(role);
await _roleRepository.AssignRoleToUserAsync(userId, roleId, assignedBy);

// CORRECT:
await _roleRepository.AddAsync(role);
var userRole = new UserRole(userId, roleId, assignedBy);
await _roleRepository.AssignRoleToUserAsync(userRole);
```

**Affected Files**:
- RoleManagementIntegrationTests.cs (all tests)

### 2. Entity Property Access Issues (80+ errors)

#### 2.1 TenantId is Protected Setter
**Problem**: `TenantId` setter is `protected` in EntityBase, cannot be set directly

**Fix Pattern**:
```csharp
// WRONG:
var policy = new AbacPolicy
{
    TenantId = tenantId,  // Error: setter is inaccessible
    ...
};

// CORRECT:
var policy = new AbacPolicy(tenantId)  // Use constructor if available
{
    ...
};

// OR create derived helper class for tests:
public class TestAbacPolicy : AbacPolicy
{
    public new Guid? TenantId { get => base.TenantId; set => base.TenantId = value; }
}
```

**Affected Files**:
- AccessControlIntegrationTests.cs (all policy tests)
- RoleManagementIntegrationTests.cs (tenant-specific tests)

#### 2.2 Role.Permissions Type Mismatch
**Problem**: Tests treat `Permissions` as `List<string>` but it's actually `string` (JSON)

**Fix Pattern**:
```csharp
// WRONG:
var role = new Role
{
    Permissions = new List<string> { "read", "write" }  // Type mismatch
};

// CORRECT:
var role = new Role
{
    Permissions = System.Text.Json.JsonSerializer.Serialize(new List<string> { "read", "write" })
};

// OR using the constructor:
var role = new Role("RoleName", "Description", tenantId);
// Permissions already initialized to "[]" by constructor
```

**Affected Files**:
- RoleManagementIntegrationTests.cs (line 84 and others)

#### 2.3 UserSession Property Names
**Problem**: Tests use `DeviceId` but entity has `DeviceFingerprint`
**Problem**: Tests use `LastActivityAt` but entity has `LastUsedAt`

**Fix Pattern**:
```csharp
// WRONG:
var session = new UserSession
{
    DeviceId = "device-1",      // Property doesn't exist
    LastActivityAt = DateTime.UtcNow  // Property doesn't exist
};

// CORRECT:
var session = new UserSession
{
    DeviceFingerprint = "device-1",
    LastUsedAt = DateTime.UtcNow
};
```

**Affected Files**:
- SessionManagementIntegrationTests.cs (all tests)

#### 2.4 AbacPolicy Properties
**Problem**: Tests use `Conditions` property which doesn't exist
**Problem**: Tests use `Effect` as string but it's enum `AbacPolicyEffect`

**Fix Pattern**:
```csharp
// WRONG:
var policy = new AbacPolicy
{
    Effect = "Allow",  // Type mismatch - it's an enum
    Conditions = @"{...}"  // Property doesn't exist
};

// CORRECT:
var policy = new AbacPolicy
{
    Effect = AbacPolicyEffect.Allow,
    AttributeExpression = @"{...}",  // Use AttributeExpression instead
    ConditionExpression = "..."      // Optional additional conditions
};
```

**Affected Files**:
- AccessControlIntegrationTests.cs (all ABAC tests)

#### 2.5 ConditionalPolicy Properties
**Problem**: Tests use `Conditions` property which doesn't exist
**Problem**: Tests use `Action` as string but it's enum `PolicyAction`
**Problem**: Tests use `IsActive` which doesn't exist

**Fix Pattern**:
```csharp
// WRONG:
var policy = new ConditionalPolicy
{
    Conditions = @"{...}",  // Property doesn't exist
    Action = "Grant",       // Type mismatch
    IsActive = true         // Property doesn't exist
};

// CORRECT:
// Note: ConditionalPolicy entity needs to be checked - may need restructuring
```

**Affected Files**:
- AccessControlIntegrationTests.cs (conditional policy tests)

#### 2.6 TenantPermission & ContentTypePermission Properties
**Problem**: Tests use `Permission` property which doesn't exist
**Problem**: Tests use `ContentType` property which doesn't exist

**Affected Files**:
- AccessControlIntegrationTests.cs (permission tests)

#### 2.7 AuthenticationAttempt Properties
**Problem**: Tests use `RiskLevel` property which doesn't exist

**Affected Files**:
- SessionManagementIntegrationTests.cs

#### 2.8 AuthUser Properties
**Problem**: Tests use `IsEmailVerified` property which doesn't exist

**Affected Files**:
- SessionManagementIntegrationTests.cs (line 748)

### 3. Missing MediatR Commands/Queries (40+ errors)

**Problem**: Tests reference MediatR commands/queries that don't exist:
- `EvaluateAbacPoliciesCommand`
- `EvaluateConditionalPoliciesCommand`
- `BulkEvaluateAbacPoliciesCommand`
- `HasTenantPermissionQuery`
- `RevokeTenantPermissionCommand`
- `ClearPermissionCacheCommand`
- `BulkRevokeTenantPermissionsCommand`

**Problem**: Tests reference `_mediator` field which doesn't exist in test class

**Solution Options**:
1. Remove MediatR usage and test services directly
2. Create these command/query handlers if they should exist
3. Use HTTP client to test via API endpoints

**Recommended Fix**: Remove `_mediator` references and test services directly

**Affected Files**:
- AccessControlIntegrationTests.cs (all policy evaluation tests)

### 4. Missing Service Methods (20+ errors)

#### 4.1 ISessionManagementService
**Problem**: Tests call methods that don't exist:
- `TerminateAllSessionsAsync`
- `GetSessionSecurityAnalysisAsync`
- `GetActivityTimelineAsync`

**Affected Files**:
- SessionManagementIntegrationTests.cs

#### 4.2 IAuthenticationAnomalyDetectionService
**Problem**: Tests call methods that don't exist:
- `AnalyzeLoginAttemptAsync`
- `ShouldThrottleAsync`
- `LogSuspiciousActivityAsync`

**Problem**: Tests use types that don't exist:
- `AuthenticationAttemptContext`
- `LocationInfo`
- `RiskLevel` enum

**Affected Files**:
- SessionManagementIntegrationTests.cs (all anomaly detection tests)

#### 4.3 IAuthService
**Problem**: Tests call methods that don't exist:
- `PolymorphicSignInAsync`

**Affected Files**:
- AuthenticationFlowsE2ETests.cs

### 5. Missing Request/Response Types (15+ errors)

**Problem**: Tests use types that don't exist:
- `GenerateWeb3ChallengeRequest`
- `VerifyWeb3SignatureRequest`
- `PolymorphicSignInRequest`
- `Web3ChallengeRequest` (namespace mismatch)

**Affected Files**:
- AuthenticationFlowsE2ETests.cs

### 6. Type Conversion Errors (5+ errors)

**Problem**: Various type mismatches
- Web3ChallengeRequest namespace confusion (DTOs vs Models.Requests)

**Affected Files**:
- AuthenticationFlowsE2ETests.cs

## Recommended Approach

### Phase 1: Fix Entity & Property Issues (High Priority)
1. Fix all `TenantId` setter access issues
2. Fix `Role.Permissions` type handling  
3. Fix `UserSession` property names (DeviceId→DeviceFingerprint, LastActivityAt→LastUsedAt)
4. Fix `AbacPolicy` property usage (Conditions→AttributeExpression, Effect enum)
5. Fix missing properties on various entities

### Phase 2: Fix Repository Method Calls
1. Replace all `CreateAsync` → `AddAsync`
2. Fix all `AssignRoleToUserAsync` calls to use `UserRole` object

### Phase 3: Remove Invalid Service/MediatR Usage
1. Remove all `_mediator` references
2. Either implement missing service methods or remove those test cases
3. Document which features are not yet implemented

### Phase 4: Fix Missing Types
1. Create missing request/response DTOs or adjust tests
2. Fix namespace issues with existing types

## Files Requiring Updates

1. **RoleManagementIntegrationTests.cs** (~150 fixes needed)
   - All CreateAsync → AddAsync
   - All AssignRoleToUserAsync signatures
   - All TenantId assignments
   - Role.Permissions type handling

2. **AccessControlIntegrationTests.cs** (~100 fixes needed)
   - All TenantId assignments
   - All MediatR command removals
   - AbacPolicy property fixes
   - ConditionalPolicy property fixes
   - Permission entity property fixes

3. **SessionManagementIntegrationTests.cs** (~80 fixes needed)
   - All DeviceId → DeviceFingerprint
   - All LastActivityAt → LastUsedAt
   - Remove anomaly detection service calls or implement them
   - Fix RiskLevel references

4. **AuthenticationFlowsE2ETests.cs** (~10 fixes needed)
   - Fix Web3 request types
   - Fix PolymorphicSignIn references
   - Remove unused variables warnings

## Next Steps

1. Decide whether to:
   - Implement missing features (recommended for production code)
   - Remove test cases for unimplemented features (temporary workaround)
   - Mock missing dependencies (test-only solution)

2. Update IMPLEMENTATION_STATUS.md to reflect actual implementation state

3. Consider creating test helper classes to simplify entity creation with protected properties

## Example Helper Class Pattern

```csharp
// TestHelpers.cs
public static class TestEntityFactory
{
    public static AbacPolicy CreateAbacPolicy(
        string name,
        Guid? tenantId,
        string resourceType,
        AbacPolicyEffect effect,
        string attributeExpression)
    {
        // Use reflection or test-specific derived classes
        // to set protected properties
        var policy = new AbacPolicy
        {
            Name = name,
            ResourceType = resourceType,
            Effect = effect,
            AttributeExpression = attributeExpression
        };
        
        // Set TenantId via reflection if needed
        typeof(EntityBase<Guid>)
            .GetProperty("TenantId")!
            .SetValue(policy, tenantId);
            
        return policy;
    }
}
```
