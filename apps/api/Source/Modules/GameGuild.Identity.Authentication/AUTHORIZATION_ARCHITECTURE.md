# Authorization Architecture Documentation

**Module:** GameGuild.Identity.Authentication  
**Date:** November 10, 2025  
**Version:** 1.0

---

## Table of Contents
1. [Overview](#overview)
2. [Authorization Layers](#authorization-layers)
3. [Role-Based Access Control (RBAC)](#1-role-based-access-control-rbac)
4. [Direct Permission Assignment](#2-direct-permission-assignment)
5. [Attribute-Based Access Control (ABAC)](#3-attribute-based-access-control-abac)
6. [Conditional Access Policies](#4-conditional-access-policies)
7. [Access Review & Compliance](#5-access-review--compliance)
8. [Authorization Decision Flow](#authorization-decision-flow)
9. [Real-World Examples](#real-world-examples)
10. [Design Benefits](#design-benefits)
11. [Implementation Status](#implementation-status)
12. [Best Practices](#best-practices)

---

## Overview

The GameGuild Authentication module implements a **sophisticated, multi-layered hybrid authorization system** that combines multiple access control paradigms to provide maximum flexibility, security, and compliance capabilities.

### Key Features
- **Multiple Authorization Models**: RBAC, ABAC, Permission-based, Conditional
- **Defense in Depth**: Multiple layers of security checks
- **Context-Aware**: Considers user attributes, environment, time, location, device
- **Compliance-Ready**: Built-in access review and audit capabilities
- **Performance Optimized**: Caching layer for authorization decisions
- **Flexible & Extensible**: Mix and match approaches based on requirements

### Architecture Philosophy
The system follows a **layered evaluation model** where each request passes through multiple authorization checks, from most restrictive (conditional policies) to most granular (direct permissions). This ensures both security and flexibility.

---

## Authorization Layers

The authorization system is structured in **five distinct layers**, evaluated in a specific order:

```
┌─────────────────────────────────────────────────────────────┐
│                    Authorization Request                     │
│              "Can User X do Action Y on Resource Z?"         │
└────────────────────────────┬────────────────────────────────┘
                             │
                ┌────────────┴────────────┐
                │  Layer 1: Conditional   │  ← Security & Context
                │  Access Policies        │     (MFA, Location, Time)
                └────────────┬────────────┘
                             │
                ┌────────────┴────────────┐
                │  Layer 2: ABAC          │  ← Attribute-Based
                │  Policies               │     (User/Resource Attributes)
                └────────────┬────────────┘
                             │
                ┌────────────┴────────────┐
                │  Layer 3: Direct        │  ← Fine-Grained
                │  Permissions            │     (Tenant/Resource/Type)
                └────────────┬────────────┘
                             │
                ┌────────────┴────────────┐
                │  Layer 4: Role-Based    │  ← Hierarchical
                │  Permissions (RBAC)     │     (Role → Permissions)
                └────────────┬────────────┘
                             │
                             ▼
                    ✅ Allow / ❌ Deny
```

**Evaluation Order**:
1. **Conditional Policies** - Can block immediately (security checks)
2. **ABAC Policies** - Dynamic attribute-based rules
3. **Direct Permissions** - Explicit permission grants
4. **Role Permissions** - Inherited from roles
5. **Default Deny** - If no match, deny access

---

## 1. Role-Based Access Control (RBAC)

### Status: ⚠️ **PLANNED - NOT YET IMPLEMENTED**

### Concept
Traditional hierarchical permission management where users are assigned **roles**, and roles contain sets of **permissions**.

### Architecture

```
┌──────────┐     assigned to    ┌──────────┐    contains    ┌──────────────┐
│   User   │ ─────────────────> │   Role   │ ────────────> │  Permission  │
└──────────┘                     └──────────┘                └──────────────┘
     │                                │                              │
     │                                │                              │
  User A                         "Admin"                    "users:write"
  User B                         "Editor"                   "posts:write"
  User C                         "Viewer"                   "posts:read"
```

### Benefits
- **Simple to Understand**: Easy mental model for administrators
- **Easy to Manage**: Group permissions into meaningful roles
- **Hierarchical**: Roles can inherit from other roles
- **Audit-Friendly**: Clear role assignments

### Typical Roles Structure
```
Role: "Administrator"
  Permissions:
    - users:*           (all user operations)
    - posts:*           (all post operations)
    - settings:*        (all settings operations)
    - audit:read        (view audit logs)

Role: "Editor"
  Permissions:
    - posts:read
    - posts:write
    - posts:publish
    - media:upload

Role: "Viewer"
  Permissions:
    - posts:read
    - comments:read
```

### Permission Format
```
<resource>:<action>

Examples:
  - users:read
  - users:write
  - users:delete
  - posts:*          (wildcard for all actions)
  - *:read           (read all resources)
```

### PermissionRegistry (Auto-Discovery)

The `PermissionRegistry` class provides **automatic discovery** of all permission scopes at startup. Instead of manually registering permissions, simply create a class that inherits from `Permission`:

```csharp
// 1. Define your permission class (auto-discovered at startup)
public sealed class MyFeaturePermission : Permission
{
    // Compile-time constants for use in attributes
    public static class Keys
    {
        public const string Read = "myfeature:read";
        public const string Write = "myfeature:write";
    }
    
    // Runtime permission instances with metadata
    public static readonly MyFeaturePermission Read = new(Keys.Read, "Read my feature data");
    public static readonly MyFeaturePermission Write = new(Keys.Write, "Write my feature data");
    
    private MyFeaturePermission(string key, string description) : base(key, description) { }
}

// 2. Use the registry for validation
if (!PermissionRegistry.IsValidKey(permissionKey))
    throw new ArgumentException($"Unknown permission: {permissionKey}");

// 3. Get all permissions for a resource
var userPermissions = PermissionRegistry.GetByResource("users");

// 4. Get all registered scopes (for documentation/admin UI)
foreach (var scope in PermissionRegistry.Scopes)
{
    Console.WriteLine($"Resource: {scope.Resource}");
    Console.WriteLine($"  Wildcard: {scope.Wildcard}");
    foreach (var perm in scope.Permissions)
        Console.WriteLine($"  - {perm.Key}: {perm.Description}");
}
```

**Key Benefits:**
- **OCP Compliant**: Add new permissions without modifying existing code
- **Single Source of Truth**: All permissions discovered from code
- **Compile-Time Safety**: Use `Keys` constants in attributes (e.g., `[Authorize(Policy = Policies.HasPermission, ...)]`)
- **Runtime Validation**: `IsValidKey()` validates permission strings including wildcards
- **Self-Documenting**: Each permission carries its description for admin UIs

See [PermissionRegistry.cs](../GameGuild.Identity.Authorization/PermissionRegistry.cs) for implementation.

### Use Cases
- **Common Access Patterns**: Define standard roles for common job functions
- **Department-Based Access**: Engineering, Marketing, Sales roles
- **Hierarchical Organizations**: Manager, Team Lead, Individual Contributor
- **Multi-Tenant Systems**: Tenant Admin, Tenant Member, Guest

### Planned Implementation
See [Section 2: Roles Management](./IMPLEMENTATION_STATUS.md#2-roles-management) in IMPLEMENTATION_STATUS.md for detailed requirements.

---

## 2. Direct Permission Assignment

### Status: ✅ **FULLY IMPLEMENTED**

### Concept
Fine-grained access control through **direct permission grants** at three distinct levels, independent of roles.

### 2.1 Tenant Permissions

**Scope**: Organization/tenant-wide capabilities

**Entity**: `TenantPermission`
```csharp
{
    UserId: Guid,
    TenantId: Guid,
    Permission: string,     // e.g., "billing:manage"
    GrantedAt: DateTime,
    GrantedBy: Guid,
    ExpiresAt: DateTime?    // Optional: temporary access
}
```

**Use Cases**:
- Billing administration for entire organization
- Tenant-wide settings management
- Organization analytics access
- Workspace administration

**Example**:
```csharp
// Grant user ability to manage billing for Tenant ABC
GrantTenantPermission(
    userId: "user-123",
    tenantId: "tenant-abc",
    permission: "billing:manage"
)
```

**Endpoints**:
- `POST /v1/permissions/tenant/grant`
- `POST /v1/permissions/tenant/revoke`
- `POST /v1/permissions/tenant/check`
- `POST /v1/permissions/tenant/list`
- `POST /v1/permissions/tenant/bulk-grant`
- `POST /v1/permissions/tenant/bulk-revoke`

### 2.2 Resource Permissions

**Scope**: Specific resource instances

**Entity**: `ResourcePermission`
```csharp
{
    UserId: Guid,
    ResourceId: Guid,       // Specific document, project, etc.
    ResourceType: string,   // "Document", "Project", etc.
    Permission: string,     // e.g., "edit", "share", "delete"
    GrantedAt: DateTime,
    GrantedBy: Guid
}
```

**Use Cases**:
- Document sharing (user can edit Document #123)
- Project collaboration (user can manage Project #456)
- Record-level access control
- Delegation of specific items

**Example**:
```csharp
// Allow user to edit specific document
GrantResourcePermission(
    userId: "user-123",
    resourceId: "doc-456",
    resourceType: "Document",
    permission: "edit"
)
```

**Endpoints**:
- `POST /v1/permissions/resource/grant`
- `POST /v1/permissions/resource/revoke`
- `POST /v1/permissions/resource/bulk-grant`

### 2.3 Content-Type Permissions

**Scope**: All resources of a specific type

**Entity**: `ContentTypePermission`
```csharp
{
    UserId: Guid,
    ContentType: string,    // "BlogPost", "Comment", etc.
    Permission: string,     // e.g., "create", "publish"
    TenantId: Guid?,
    GrantedAt: DateTime
}
```

**Use Cases**:
- Allow user to create all Blog Posts
- Grant moderation access to all Comments
- Enable publishing capability for all Articles
- Type-based bulk permissions

**Example**:
```csharp
// Allow user to create any blog post
GrantContentTypePermission(
    userId: "user-123",
    contentType: "BlogPost",
    permission: "create"
)
```

**Endpoints**:
- `POST /v1/permissions/content-type/grant`
- `POST /v1/permissions/content-type/revoke`

### 2.4 Permission Templates

**Purpose**: Pre-defined permission sets for common scenarios

**Features**:
- **Template Library**: Reusable permission configurations
- **Bulk Application**: Apply multiple permissions at once
- **Consistency**: Ensure standard access patterns
- **Quick Setup**: Onboard users faster

**Example Template**:
```json
{
    "name": "Content Creator Bundle",
    "description": "Standard permissions for content creators",
    "permissions": [
        { "type": "ContentType", "contentType": "BlogPost", "permission": "create" },
        { "type": "ContentType", "contentType": "BlogPost", "permission": "edit" },
        { "type": "ContentType", "contentType": "Media", "permission": "upload" },
        { "type": "Tenant", "permission": "analytics:view" }
    ]
}
```

**Endpoint**:
- `POST /v1/permissions/template/apply`

### 2.5 Permission Caching

**Purpose**: Performance optimization for permission checks

**Features**:
- **In-Memory Cache**: Fast permission lookups
- **TTL-Based**: Automatic expiration
- **Invalidation**: Clear on permission changes
- **Cache Statistics**: Monitor cache performance

**Endpoint**:
- `POST /v1/permissions/cache/clear`

---

## 3. Attribute-Based Access Control (ABAC)

### Status: ✅ **FULLY IMPLEMENTED**

### Concept
Dynamic, context-aware authorization based on **attributes** of users, resources, and environment, evaluated at runtime using **policy expressions**.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      ABAC Policy Engine                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │    User      │  │   Resource   │  │ Environment  │      │
│  │  Attributes  │  │  Attributes  │  │  Attributes  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│         │                  │                  │             │
│         └──────────────────┴──────────────────┘             │
│                          │                                  │
│                          ▼                                  │
│              ┌─────────────────────┐                        │
│              │  Policy Expression  │                        │
│              │  Evaluation Engine  │                        │
│              └─────────────────────┘                        │
│                          │                                  │
│                          ▼                                  │
│                  Allow or Deny                              │
└─────────────────────────────────────────────────────────────┘
```

### Attribute Sources

#### User Attributes
```csharp
{
    "department": "Engineering",
    "seniority": "Senior",
    "location": "US-West",
    "clearanceLevel": 3,
    "employmentType": "FullTime",
    "costCenter": "CC-1234",
    "certifications": ["AWS", "Azure"]
}
```

#### Resource Attributes
```csharp
{
    "classification": "Confidential",
    "owner": "user-456",
    "department": "Finance",
    "createdAt": "2025-01-15",
    "dataResidency": "EU",
    "tags": ["sensitive", "pii"]
}
```

#### Environmental Attributes
```csharp
{
    "currentTime": "2025-11-10T14:30:00Z",
    "dayOfWeek": "Monday",
    "ipAddress": "192.168.1.100",
    "ipLocation": "US-CA",
    "deviceType": "Desktop",
    "networkType": "Corporate"
}
```

#### Contextual Attributes
```csharp
{
    "mfaVerified": true,
    "sessionRiskScore": 0.2,
    "trustedDevice": true,
    "requestOrigin": "WebApp"
}
```

### Policy Structure

```csharp
public class AbacPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Expression using attributes
    public string Expression { get; set; }
    
    // Effect of the policy
    public PolicyEffect Effect { get; set; }  // Allow, Deny
    
    // Resources this applies to
    public string[] ResourceTypes { get; set; }
    
    // Actions covered
    public string[] Actions { get; set; }
    
    // Priority for conflict resolution
    public int Priority { get; set; }
    
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
}
```

### Expression Language

Supports complex boolean logic:

```javascript
// Example 1: Department and Seniority
user.department == "Engineering" AND user.seniority >= "Senior"

// Example 2: Time-Based Access
user.department == "Support" AND 
environment.currentTime >= "09:00" AND 
environment.currentTime <= "18:00"

// Example 3: Data Classification
resource.classification == "Public" OR 
(resource.classification == "Internal" AND user.employmentType == "FullTime")

// Example 4: Complex Conditions
(user.clearanceLevel >= resource.requiredClearance) AND
(user.location == resource.dataResidency OR user.hasOverride == true) AND
context.mfaVerified == true

// Example 5: Ownership Check
resource.owner == user.id OR 
user.department == resource.department AND user.role == "Manager"
```

### Operators Supported
- **Comparison**: `==`, `!=`, `>`, `<`, `>=`, `<=`
- **Logical**: `AND`, `OR`, `NOT`
- **Membership**: `IN`, `NOT IN`
- **Pattern**: `MATCHES`, `CONTAINS`
- **Null Check**: `IS NULL`, `IS NOT NULL`

### Policy Examples

#### Example 1: Deployment Restrictions
```json
{
    "name": "Production Deploy Restrictions",
    "expression": "user.department == 'Engineering' AND user.seniority >= 'Senior' AND resource.environment == 'production' AND context.mfaVerified == true",
    "effect": "Allow",
    "resourceTypes": ["Deployment"],
    "actions": ["deploy", "rollback"],
    "priority": 10
}
```

#### Example 2: Data Access Control
```json
{
    "name": "PII Data Access",
    "expression": "resource.containsPII == true AND (user.certifications CONTAINS 'DataPrivacy' OR user.clearanceLevel >= 4)",
    "effect": "Allow",
    "resourceTypes": ["CustomerRecord", "EmployeeRecord"],
    "actions": ["read", "export"],
    "priority": 20
}
```

#### Example 3: Geographic Restrictions
```json
{
    "name": "EU Data Residency",
    "expression": "resource.dataResidency == 'EU' AND user.location IN ['EU-West', 'EU-Central']",
    "effect": "Allow",
    "resourceTypes": ["*"],
    "actions": ["read", "write"],
    "priority": 5
}
```

### Endpoints

**Policy Management**:
- `POST /v1/abac/policies` - Create policy
- `GET /v1/abac/policies` - List policies
- `GET /v1/abac/policies/{id}` - Get policy details
- `PUT /v1/abac/policies/{id}` - Update policy
- `DELETE /v1/abac/policies/{id}` - Delete policy
- `POST /v1/abac/policies/{id}/activate` - Activate policy
- `POST /v1/abac/policies/{id}/deactivate` - Deactivate policy
- `POST /v1/abac/policies/{id}/clone` - Clone policy
- `POST /v1/abac/policies/from-template` - Create from template

**Policy Evaluation**:
- `POST /v1/abac/evaluate` - Evaluate if action is allowed
- `POST /v1/abac/bulk-evaluate` - Evaluate multiple actions
- `POST /v1/abac/test-expression` - Test expression logic
- `POST /v1/abac/validate` - Validate policy syntax

### Use Cases

1. **Department-Based Access**:
   - Finance users can access financial reports
   - HR users can view employee records

2. **Seniority-Based Operations**:
   - Only senior engineers can deploy to production
   - Managers can approve expenses over $10,000

3. **Data Classification**:
   - Confidential data requires higher clearance
   - Public data accessible to all

4. **Time-Based Access**:
   - Support team access only during business hours
   - After-hours access requires special approval

5. **Location-Based Restrictions**:
   - EU data only accessible from EU locations
   - Sensitive operations require corporate network

6. **Ownership & Delegation**:
   - Users can edit their own resources
   - Department managers can access team resources

---

## 4. Conditional Access Policies

### Status: ✅ **FULLY IMPLEMENTED**

### Concept
Real-time access control based on **runtime conditions** with the ability to **require additional verification** or **block access** based on security context.

### Differences from ABAC
| Aspect | ABAC | Conditional Policies |
|--------|------|---------------------|
| **Focus** | User/Resource attributes | Access conditions & security context |
| **Evaluation** | Allow/Deny | Allow/Deny/RequireVerification |
| **Priority** | Policy-based | Explicit priority order |
| **Use Case** | Business logic | Security enforcement |
| **Examples** | "Senior engineers only" | "Require MFA for sensitive data" |

### Policy Structure

```csharp
public class ConditionalPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Priority (lower = evaluated first)
    public int Priority { get; set; }
    
    // Conditions to evaluate
    public PolicyCondition[] Conditions { get; set; }
    
    // Action to take
    public PolicyAction Action { get; set; }  // Block, Allow, RequireMfa, RequireApproval
    
    // Effect
    public PolicyEffect Effect { get; set; }  // Allow, Deny
    
    // Resources affected
    public string[] ResourceTypes { get; set; }
    
    public bool IsActive { get; set; }
}

public class PolicyCondition
{
    public PolicyConditionType Type { get; set; }
    public string Operator { get; set; }  // Equals, NotEquals, Contains, etc.
    public string Value { get; set; }
}
```

### Condition Types

```csharp
public enum PolicyConditionType
{
    // Location-based
    IpAddress,
    IpLocation,
    Country,
    
    // Time-based
    TimeOfDay,
    DayOfWeek,
    DateRange,
    
    // Device-based
    DeviceType,
    DeviceTrusted,
    OperatingSystem,
    
    // Security-based
    MfaVerified,
    RiskScore,
    SessionAge,
    
    // Resource-based
    ResourceClassification,
    ResourceOwner,
    ResourceAge,
    
    // User-based
    UserRole,
    UserDepartment,
    AccountAge
}
```

### Policy Actions

```csharp
public enum PolicyAction
{
    Block,              // Immediately deny access
    Allow,              // Allow with no additional requirements
    RequireMfa,         // Require MFA verification
    RequireApproval,    // Require manager approval
    RequireJustification, // Require access justification
    StepUpAuth          // Require re-authentication
}
```

### Policy Examples

#### Example 1: MFA for Sensitive Data
```json
{
    "name": "Require MFA for Confidential Data",
    "priority": 1,
    "conditions": [
        {
            "type": "ResourceClassification",
            "operator": "Equals",
            "value": "Confidential"
        },
        {
            "type": "MfaVerified",
            "operator": "Equals",
            "value": "false"
        }
    ],
    "action": "RequireMfa",
    "effect": "Deny",
    "resourceTypes": ["Document", "Report", "CustomerData"]
}
```

#### Example 2: Geographic Restrictions
```json
{
    "name": "Block Access from High-Risk Countries",
    "priority": 5,
    "conditions": [
        {
            "type": "Country",
            "operator": "In",
            "value": "CN,RU,KP"
        }
    ],
    "action": "Block",
    "effect": "Deny",
    "resourceTypes": ["*"]
}
```

#### Example 3: Business Hours Only
```json
{
    "name": "Restrict After-Hours Access",
    "priority": 10,
    "conditions": [
        {
            "type": "TimeOfDay",
            "operator": "NotBetween",
            "value": "09:00-18:00"
        },
        {
            "type": "DayOfWeek",
            "operator": "In",
            "value": "Monday,Tuesday,Wednesday,Thursday,Friday"
        },
        {
            "type": "UserRole",
            "operator": "NotEquals",
            "value": "Administrator"
        }
    ],
    "action": "Block",
    "effect": "Deny"
}
```

#### Example 4: Untrusted Device Restriction
```json
{
    "name": "Untrusted Device Limitations",
    "priority": 3,
    "conditions": [
        {
            "type": "DeviceTrusted",
            "operator": "Equals",
            "value": "false"
        }
    ],
    "action": "Block",
    "effect": "Deny",
    "resourceTypes": ["PaymentInfo", "BankAccount"]
}
```

#### Example 5: High-Risk Score
```json
{
    "name": "Elevated Risk Score",
    "priority": 2,
    "conditions": [
        {
            "type": "RiskScore",
            "operator": "GreaterThan",
            "value": "0.7"
        }
    ],
    "action": "StepUpAuth",
    "effect": "Deny"
}
```

### Endpoints

**Policy Management**:
- `POST /v1/conditional/policies` - Create policy
- `GET /v1/conditional/policies` - List policies
- `GET /v1/conditional/policies/{id}` - Get policy
- `PUT /v1/conditional/policies/{id}` - Update policy
- `DELETE /v1/conditional/policies/{id}` - Delete policy
- `POST /v1/conditional/policies/{id}/activate` - Activate
- `POST /v1/conditional/policies/{id}/deactivate` - Deactivate
- `PUT /v1/conditional/policies/{id}/priority` - Update priority
- `POST /v1/conditional/policies/{id}/clone` - Clone policy
- `POST /v1/conditional/policies/from-template` - Create from template

**Policy Evaluation**:
- `POST /v1/conditional/evaluate` - Evaluate policies
- `POST /v1/conditional/bulk-evaluate` - Bulk evaluate
- `POST /v1/conditional/simulate` - Simulate policy impact
- `POST /v1/conditional/validate` - Validate policy
- `POST /v1/conditional/validate-condition` - Validate condition
- `POST /v1/conditional/test-rule` - Test policy rule

### Use Cases

1. **Zero Trust Security**:
   - Require MFA for sensitive operations
   - Verify device trust status
   - Continuous authentication

2. **Compliance Requirements**:
   - Geographic data restrictions
   - Time-based access controls
   - Audit trail requirements

3. **Risk-Based Access**:
   - Block high-risk sessions
   - Require additional verification for anomalies
   - Step-up authentication for sensitive actions

4. **Device Management**:
   - Corporate device requirements
   - OS version compliance
   - Trusted device registration

5. **Incident Response**:
   - Emergency access restrictions
   - Rapid policy deployment
   - Temporary access controls

---

## 5. Access Review & Compliance

### Status: ✅ **FULLY IMPLEMENTED**

### Concept
Periodic review and audit of access permissions to ensure compliance with security policies and regulations.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Access Review Workflow                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. Create Campaign                                          │
│     ├─ Define scope (users, resources, permissions)         │
│     ├─ Assign reviewers                                      │
│     └─ Set deadline                                          │
│                                                              │
│  2. Generate Review Items                                    │
│     ├─ Collect all permissions                               │
│     ├─ Group by user/resource/type                           │
│     └─ Create review tasks                                   │
│                                                              │
│  3. Review Process                                           │
│     ├─ Reviewers evaluate access                             │
│     ├─ Approve or revoke permissions                         │
│     ├─ Add justifications                                    │
│     └─ Escalate if needed                                    │
│                                                              │
│  4. Execute Actions                                          │
│     ├─ Revoke denied permissions                             │
│     ├─ Update audit trail                                    │
│     └─ Notify affected users                                 │
│                                                              │
│  5. Generate Reports                                         │
│     ├─ Compliance status                                     │
│     ├─ Review statistics                                     │
│     └─ Remediation actions                                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Campaign Types

#### 1. One-Time Campaign
Manual access review for specific purpose:
```json
{
    "name": "Q4 2025 Access Review",
    "startDate": "2025-11-01",
    "endDate": "2025-11-30",
    "scope": {
        "userGroups": ["Engineering", "Finance"],
        "resourceTypes": ["FinancialReport", "CustomerData"],
        "permissionTypes": ["Admin", "Write"]
    },
    "reviewers": ["manager-1", "security-lead"]
}
```

#### 2. Periodic Campaign
Recurring automated reviews:
```json
{
    "name": "Quarterly Admin Review",
    "frequency": "Quarterly",
    "autoStart": true,
    "scope": {
        "permissionTypes": ["Admin", "Owner"]
    },
    "reviewers": ["security-team"],
    "reminderSettings": {
        "enabled": true,
        "frequency": "Weekly"
    }
}
```

### Review Item Structure

```csharp
public class AccessReviewItem
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    
    // What's being reviewed
    public Guid UserId { get; set; }
    public string Permission { get; set; }
    public Guid? ResourceId { get; set; }
    public string ResourceType { get; set; }
    
    // Review details
    public ReviewStatus Status { get; set; }  // Pending, Approved, Revoked
    public Guid? ReviewerId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string Justification { get; set; }
    public string Comments { get; set; }
    
    // Context
    public DateTime PermissionGrantedAt { get; set; }
    public DateTime LastUsed { get; set; }
    public int UsageCount { get; set; }
}
```

### Compliance Features

#### Compliance Status Tracking
```csharp
public class ComplianceStatus
{
    public int TotalReviews { get; set; }
    public int CompletedReviews { get; set; }
    public int PendingReviews { get; set; }
    public int OverdueReviews { get; set; }
    
    public int PermissionsRevoked { get; set; }
    public int PermissionsApproved { get; set; }
    
    public double ComplianceScore { get; set; }  // 0-100
    public List<ComplianceFlag> Flags { get; set; }
}

public class ComplianceFlag
{
    public string Type { get; set; }      // "StalePermission", "UnusedAccess", etc.
    public string Severity { get; set; }  // "Critical", "High", "Medium", "Low"
    public string Description { get; set; }
    public Guid[] AffectedUsers { get; set; }
}
```

### Analytics & Reporting

#### Review Analytics
- Review completion rates
- Average review time
- Permissions revoked vs approved
- Most reviewed resource types
- Top reviewers by volume

#### Access Patterns
- Permission usage statistics
- Last access timestamps
- Stale permission identification
- Privilege creep detection

### Endpoints

**Campaign Management** (7 endpoints):
- `POST /v1/access-review/campaigns`
- `GET /v1/access-review/campaigns`
- `GET /v1/access-review/campaigns/{id}`
- `PUT /v1/access-review/campaigns/{id}`
- `DELETE /v1/access-review/campaigns/{id}`
- `POST /v1/access-review/campaigns/{id}/start`
- `POST /v1/access-review/campaigns/{id}/complete`

**Review Items** (4 endpoints):
- `GET /v1/access-review/campaigns/{campaignId}/items`
- `GET /v1/access-review/items/{itemId}`
- `POST /v1/access-review/items/{itemId}/review`
- `POST /v1/access-review/items/bulk-review`

**Periodic Reviews** (4 endpoints):
- `POST /v1/access-review/periodic`
- `PUT /v1/access-review/periodic/{id}`
- `DELETE /v1/access-review/periodic/{id}`
- `POST /v1/access-review/periodic/{id}/trigger`

**Revocation & Compliance** (4 endpoints):
- `POST /v1/access-review/revoke`
- `POST /v1/access-review/bulk-revoke`
- `GET /v1/access-review/compliance`
- `PUT /v1/access-review/compliance/flags`

**Reports & Analytics** (3 endpoints):
- `POST /v1/access-review/reports/generate`
- `GET /v1/access-review/analytics`
- `GET /v1/access-review/history`

**Templates & Reminders** (4 endpoints):
- `GET /v1/access-review/templates`
- `POST /v1/access-review/campaigns/from-template`
- `POST /v1/access-review/reminders/send`
- `PUT /v1/access-review/reminders/configure`

### Use Cases

1. **Regulatory Compliance**:
   - SOX: Annual access reviews for financial systems
   - HIPAA: Quarterly reviews for healthcare data access
   - GDPR: Regular reviews of personal data access

2. **Security Audits**:
   - Privilege escalation detection
   - Stale permission cleanup
   - Orphaned account identification

3. **Organizational Changes**:
   - Department transfers
   - Role changes
   - Employee departures

4. **Risk Management**:
   - High-privilege access monitoring
   - Third-party access reviews
   - Temporary access expiration

---

## Authorization Decision Flow

### Complete Evaluation Process

```
┌─────────────────────────────────────────────────────────────┐
│                 AUTHORIZATION REQUEST                        │
│         Can User X perform Action Y on Resource Z?          │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
           ┌─────────────────────────────────┐
           │  Gather Context & Attributes    │
           │  ├─ User attributes             │
           │  ├─ Resource attributes         │
           │  ├─ Environment context         │
           │  └─ Session metadata            │
           └─────────────────┬───────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────┐
│  LAYER 1: CONDITIONAL ACCESS POLICIES (Priority Order)     │
│  ├─ Check MFA requirement                                  │
│  ├─ Verify device trust                                    │
│  ├─ Validate IP/Location                                   │
│  ├─ Check time restrictions                                │
│  └─ Evaluate risk score                                    │
└────────────────────────────┬───────────────────────────────┘
                             │
                    ┌────────┴────────┐
                    │   BLOCK?        │
                    └────────┬────────┘
                             │
                 ┌───────────┴───────────┐
                 │                       │
                YES                     NO
                 │                       │
                 ▼                       ▼
           ❌ DENY                 Continue
         (Security Block)               │
                                        │
                                        ▼
┌────────────────────────────────────────────────────────────┐
│  LAYER 2: ABAC POLICIES                                    │
│  ├─ Match user attributes                                  │
│  ├─ Match resource attributes                              │
│  ├─ Evaluate policy expressions                            │
│  └─ Apply highest priority matching policy                 │
└────────────────────────────┬───────────────────────────────┘
                             │
                    ┌────────┴────────┐
                    │   EXPLICIT      │
                    │   DENY?         │
                    └────────┬────────┘
                             │
                 ┌───────────┴───────────┐
                 │                       │
                YES                     NO
                 │                       │
                 ▼                       ▼
           ❌ DENY                 Continue
         (ABAC Deny)                    │
                                        │
                                        ▼
┌────────────────────────────────────────────────────────────┐
│  LAYER 3: DIRECT PERMISSIONS                               │
│  ├─ Check Resource Permission (specific item)              │
│  ├─ Check Content-Type Permission (all of type)            │
│  └─ Check Tenant Permission (org-wide)                     │
└────────────────────────────┬───────────────────────────────┘
                             │
                    ┌────────┴────────┐
                    │   FOUND?        │
                    └────────┬────────┘
                             │
                 ┌───────────┴───────────┐
                 │                       │
                YES                     NO
                 │                       │
                 ▼                       ▼
            ✅ ALLOW               Continue
         (Direct Grant)                 │
                                        │
                                        ▼
┌────────────────────────────────────────────────────────────┐
│  LAYER 4: ROLE-BASED PERMISSIONS (⚠️ Not Implemented)     │
│  ├─ Get user's roles                                       │
│  ├─ Aggregate permissions from all roles                   │
│  └─ Check if permission exists in any role                 │
└────────────────────────────┬───────────────────────────────┘
                             │
                    ┌────────┴────────┐
                    │   FOUND?        │
                    └────────┬────────┘
                             │
                 ┌───────────┴───────────┐
                 │                       │
                YES                     NO
                 │                       │
                 ▼                       ▼
            ✅ ALLOW              ❌ DENY
         (Role Grant)        (No Permission)
                                        │
                                        ▼
                            ┌───────────────────┐
                            │   Log Decision    │
                            │   Audit Trail     │
                            │   Metrics         │
                            └───────────────────┘
```

### Decision Priority

**Explicit Deny > Explicit Allow > Default Deny**

1. **Conditional Policy Block**: Highest priority - immediate denial
2. **ABAC Explicit Deny**: Second priority - policy-based denial
3. **ABAC Explicit Allow**: Third priority - policy-based allow
4. **Direct Permission**: Fourth priority - explicit grant
5. **Role Permission**: Fifth priority - inherited grant
6. **Default Deny**: If no allow found, deny by default

### Caching Strategy

```
┌─────────────────────────────────────────────────────────────┐
│                    Authorization Cache                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Cache Key: user:{id}:resource:{id}:action:{action}         │
│  TTL: 5 minutes (configurable)                              │
│                                                              │
│  Invalidation Triggers:                                      │
│  ├─ Permission grant/revoke                                  │
│  ├─ Role assignment change                                   │
│  ├─ Policy activation/deactivation                           │
│  └─ User logout                                              │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Real-World Examples

### Example 1: Document Access

**Scenario**: User trying to edit a confidential document

```csharp
AuthorizationRequest request = new()
{
    UserId = "user-123",
    Action = "edit",
    ResourceType = "Document",
    ResourceId = "doc-456"
};

// Step 1: Conditional Policy Check
ConditionalPolicy mfaPolicy = "Require MFA for Confidential Documents";
if (document.Classification == "Confidential" && !user.MfaVerified)
{
    return new AuthorizationResult
    {
        Allowed = false,
        Reason = "MFA verification required",
        RequiredAction = "PerformMfa"
    };
}

// Step 2: ABAC Policy Check
AbacPolicy departmentPolicy = "Department Restricted Documents";
string expression = "resource.department == user.department OR user.role == 'Manager'";
if (!EvaluateExpression(expression, user, document))
{
    return AuthorizationResult.Deny("Department restriction");
}

// Step 3: Direct Permission Check
if (HasResourcePermission(userId: "user-123", resourceId: "doc-456", permission: "edit"))
{
    return AuthorizationResult.Allow("Direct permission grant");
}

// Step 4: Role Permission Check (when implemented)
if (UserHasRoleWithPermission(userId: "user-123", permission: "documents:edit"))
{
    return AuthorizationResult.Allow("Role-based permission");
}

// Step 5: Default Deny
return AuthorizationResult.Deny("No permission found");
```

### Example 2: Production Deployment

**Scenario**: Engineer attempting to deploy to production

```csharp
// Context
User user = new()
{
    Id = "eng-789",
    Department = "Engineering",
    Seniority = "Senior",
    MfaVerified = true,
    Location = "US-West"
};

Resource resource = new()
{
    Type = "Deployment",
    Environment = "Production",
    Application = "PaymentService"
};

// Layer 1: Conditional Policies
if (environment.CurrentTime.Hour < 9 || environment.CurrentTime.Hour > 17)
{
    return Deny("Production deploys only allowed during business hours (9am-5pm)");
}

if (environment.DayOfWeek == DayOfWeek.Friday)
{
    return Deny("No Friday production deploys (change freeze)");
}

// Layer 2: ABAC
string expression = @"
    user.department == 'Engineering' AND 
    user.seniority >= 'Senior' AND 
    context.mfaVerified == true AND
    (resource.application NOT IN criticalApps OR user.hasEmergencyAccess == true)
";

if (!EvaluateAbacPolicy(expression))
{
    return Deny("ABAC policy: Only senior engineers with MFA can deploy");
}

// Layer 3: Direct Permissions
if (HasContentTypePermission(userId, "Deployment", "execute"))
{
    // Log the deployment
    AuditLog.Record(new DeploymentEvent
    {
        User = user.Id,
        Environment = "Production",
        Application = resource.Application,
        Timestamp = DateTime.UtcNow
    });
    
    return Allow("Direct deployment permission");
}

// Layer 4: Roles (when implemented)
if (UserHasRole(userId, "DevOps Engineer"))
{
    return Allow("DevOps Engineer role");
}

return Deny("Insufficient permissions for production deployment");
```

### Example 3: Sensitive Data Export

**Scenario**: User trying to export customer PII data

```csharp
// Context
User user = new()
{
    Id = "analyst-456",
    Department = "Analytics",
    DataPrivacyCertified = true,
    RiskScore = 0.15
};

Resource resource = new()
{
    Type = "CustomerData",
    Classification = "PII",
    RecordCount = 50000
};

// Layer 1: Conditional - Risk Assessment
if (user.RiskScore > 0.5)
{
    return Deny("High risk score detected - access suspended");
}

if (!user.MfaVerified)
{
    return RequireMfa("MFA required for PII data export");
}

if (!session.FromTrustedDevice)
{
    return Deny("PII exports only allowed from trusted devices");
}

// Layer 2: ABAC - Data Privacy Compliance
string expression = @"
    resource.classification == 'PII' AND
    user.dataPrivacyCertified == true AND
    user.department IN ['Legal', 'Compliance', 'Analytics'] AND
    resource.recordCount <= 100000
";

if (!EvaluateAbacPolicy(expression))
{
    return Deny("Data privacy policy violation");
}

// Require justification for large exports
if (resource.RecordCount > 10000)
{
    return RequireJustification("Justification required for large data exports");
}

// Layer 3: Direct Permissions
if (HasTenantPermission(userId, "data:export-pii"))
{
    // Create access request for approval
    AccessRequest request = CreateAccessRequest(new()
    {
        UserId = user.Id,
        ResourceType = resource.Type,
        Action = "export",
        RecordCount = resource.RecordCount,
        RequiresApproval = true,
        ApproverRoles = ["Data Protection Officer"]
    });
    
    return Pending("Access request created - awaiting approval", request.Id);
}

return Deny("No PII export permission");
```

---

## Design Benefits

### 1. **Flexibility**

**Multiple Approaches**: Choose the right tool for each use case
- Simple scenarios → Roles
- Fine-grained → Direct Permissions
- Complex logic → ABAC
- Security requirements → Conditional Policies

**Gradual Adoption**: Start simple, add complexity as needed
```
Phase 1: Roles + Direct Permissions (traditional)
Phase 2: Add ABAC for complex rules
Phase 3: Layer Conditional Policies for security
Phase 4: Implement Access Reviews for compliance
```

### 2. **Security**

**Defense in Depth**: Multiple layers of protection
- Conditional policies block insecure contexts
- ABAC enforces business rules
- Direct permissions provide granular control
- Regular reviews catch privilege creep

**Context-Aware**: Decisions based on full context
- Who is making the request
- What they're trying to access
- When and where they're accessing from
- How they're accessing (device, network)
- Why (justification, approval)

**Zero Trust Principles**:
- Never trust, always verify
- Least privilege access
- Assume breach mentality
- Continuous verification

### 3. **Compliance**

**Audit Trail**: Complete visibility
- Every decision logged
- Who approved what, when
- Changes tracked over time
- Access patterns analyzed

**Regular Reviews**: Automated compliance
- Periodic access certification
- Stale permission cleanup
- Privilege escalation detection
- Compliance reporting

**Regulatory Support**:
- SOX: Financial system access controls
- HIPAA: Healthcare data protection
- GDPR: Personal data access management
- ISO 27001: Information security

### 4. **Performance**

**Caching Layer**: Fast authorization decisions
- In-memory permission cache
- TTL-based invalidation
- Distributed cache support
- Cache hit rate monitoring

**Bulk Operations**: Efficient at scale
- Bulk permission grants
- Batch policy evaluation
- Parallel processing
- Optimized database queries

**Lazy Evaluation**: Only compute what's needed
- Short-circuit on deny
- Skip unnecessary checks
- Conditional layer first
- Cache before compute

### 5. **Maintainability**

**Clear Separation**: Each layer has distinct purpose
- Roles: Common patterns
- Permissions: Exceptions & fine-tuning
- ABAC: Complex business logic
- Conditional: Security enforcement

**Template-Based**: Reusable configurations
- Permission templates
- Policy templates
- Campaign templates
- Best practices codified

**Self-Service**: Reduce admin burden
- Delegated administration
- Request/approval workflows
- Automated reviews
- Policy simulation

---

## Implementation Status

### ✅ Fully Implemented (95% Complete)

1. **Direct Permissions** ✅
   - Tenant permissions
   - Resource permissions
   - Content-type permissions
   - Permission templates
   - Caching system
   - 13 endpoints

2. **ABAC Policies** ✅
   - Policy CRUD operations
   - Expression evaluation engine
   - Policy templates
   - Bulk evaluation
   - 13 endpoints

3. **Conditional Policies** ✅
   - Policy management
   - Priority-based evaluation
   - Condition validation
   - Policy simulation
   - 16 endpoints

4. **Access Reviews** ✅
   - Campaign management
   - Review workflows
   - Compliance tracking
   - Analytics & reporting
   - 26 endpoints

### ⚠️ Planned - Not Implemented (5% Remaining)

1. **Role-Based Access Control** ❌
   - Role entity
   - Role repository
   - Role commands/handlers
   - Role queries
   - Role-user assignments
   - Role-permission mappings
   - 5 endpoints (placeholders exist)

**Missing Components**:
```
- Role.cs entity
- IRoleRepository interface
- RoleRepository implementation
- CreateRoleCommand + Handler
- UpdateRoleCommand + Handler
- DeleteRoleCommand + Handler
- AssignRoleToUserCommand + Handler
- RemoveRoleFromUserCommand + Handler
- GetRolesQuery + Handler
- GetRoleByIdQuery + Handler
- GetUserRolesQuery + Handler
- RoleDto, CreateRoleRequest, UpdateRoleRequest
```

**Estimated Effort**: 3-5 days for full RBAC implementation

### Current Capabilities

**Without RBAC**, the system still provides:
- ✅ Fine-grained direct permissions
- ✅ Complex attribute-based rules
- ✅ Security-focused conditional policies
- ✅ Compliance & access reviews

**With RBAC** (once implemented), adds:
- Simplified administration for common patterns
- Hierarchical permission management
- Traditional role-based workflows
- Role templates for new users

---

## Best Practices

### 1. **Layer Usage Guidelines**

**Use Conditional Policies for**:
- Security requirements (MFA, device trust)
- Geographic restrictions
- Time-based access
- Risk-based decisions

**Use ABAC for**:
- Complex business logic
- Dynamic attribute-based rules
- Cross-cutting concerns
- Temporary/contextual access

**Use Direct Permissions for**:
- Specific exceptions
- Fine-grained control
- Individual resource access
- Temporary grants

**Use Roles for** (when implemented):
- Common access patterns
- Job function definitions
- Department-based access
- Standard user onboarding

### 2. **Performance Optimization**

**Cache Aggressively**:
```csharp
// Cache permission checks
CacheKey = $"auth:{userId}:{resourceId}:{action}";
TTL = 5 minutes;

// Invalidate on changes
OnPermissionGrant() => ClearCache(userId);
OnPolicyUpdate() => ClearPolicyCache();
```

**Batch Operations**:
```csharp
// Instead of checking one by one
foreach (resource in resources)
{
    await CheckPermission(userId, resource.Id, action);
}

// Bulk check
var results = await BulkCheckPermissions(userId, resourceIds, action);
```

**Short-Circuit Evaluation**:
```csharp
// Stop on first deny
if (ConditionalPolicyBlocks()) return Deny;
if (AbacPolicyDenies()) return Deny;
// Continue to next layer
```

### 3. **Security Best Practices**

**Default Deny**:
```csharp
// Always end with deny
return AuthorizationResult.Deny("No permission found");
```

**Explicit Over Implicit**:
```csharp
// Good: Explicit permission
GrantPermission(userId, "documents:edit", documentId);

// Avoid: Wildcard for everything
GrantPermission(userId, "*:*");  // Too broad
```

**Least Privilege**:
```csharp
// Good: Minimal permission
GrantPermission(userId, "reports:read");

// Avoid: Excessive permission
GrantPermission(userId, "reports:*");  // Includes delete
```

**Regular Reviews**:
```csharp
// Quarterly review of admin access
CreatePeriodicReview(
    frequency: "Quarterly",
    scope: { permissionTypes: ["Admin", "Owner"] }
);
```

### 4. **Policy Design**

**Keep Policies Simple**:
```javascript
// Good: Simple, readable
user.department == "Engineering"

// Avoid: Overly complex
(user.department == "Engineering" OR user.role == "Contractor") 
AND (resource.classification != "Secret" OR user.clearance >= 5)
AND (environment.time >= "09:00" AND environment.time <= "17:00")
// Split into multiple policies
```

**Use Descriptive Names**:
```csharp
// Good
"Require MFA for Confidential Data Access"

// Avoid
"Policy_123"
```

**Document Policies**:
```csharp
Policy.Description = @"
    This policy requires MFA verification when accessing 
    confidential classified data. Applies to all users 
    except administrators with emergency access override.
    
    Compliance: SOC 2 requirement A.1.2.3
";
```

### 5. **Monitoring & Alerting**

**Track Key Metrics**:
- Authorization decision latency
- Cache hit rate
- Policy evaluation count
- Permission grant/revoke rate
- Failed authorization attempts

**Alert on Anomalies**:
- Spike in denials
- Unusual access patterns
- Policy conflicts
- Performance degradation

**Audit Everything**:
```csharp
AuditLog.Record(new AuthorizationEvent
{
    UserId = user.Id,
    Action = action,
    Resource = resource.Id,
    Decision = decision,
    Reason = reason,
    Policies = evaluatedPolicies,
    Timestamp = DateTime.UtcNow,
    IpAddress = context.IpAddress,
    UserAgent = context.UserAgent
});
```

### 6. **Testing Strategy**

**Unit Test Policies**:
```csharp
[Test]
public void SeniorEngineers_CanDeployToProduction()
{
    var user = new User { Department = "Engineering", Seniority = "Senior" };
    var resource = new Resource { Environment = "Production" };
    
    var result = policyEngine.Evaluate("Production Deploy", user, resource);
    
    Assert.That(result.Allowed, Is.True);
}
```

**Integration Test Layers**:
```csharp
[Test]
public async Task Authorization_ChecksAllLayers()
{
    // Setup conditional policy (MFA required)
    // Setup ABAC policy (department restriction)
    // Setup direct permission
    
    var result = await authService.CheckAccess(userId, resourceId, action);
    
    // Verify correct layer granted/denied
}
```

**Load Test Performance**:
```csharp
[Test]
public async Task Authorization_PerformsUnderLoad()
{
    // Simulate 1000 concurrent authorization checks
    var tasks = Enumerable.Range(0, 1000)
        .Select(_ => authService.CheckAccess(userId, resourceId, action));
    
    await Task.WhenAll(tasks);
    
    Assert.That(averageLatency, Is.LessThan(50 milliseconds));
}
```

---

## Conclusion

The GameGuild Authorization Architecture provides a **comprehensive, enterprise-grade access control system** that balances:

- **Security**: Multi-layered defense with conditional policies
- **Flexibility**: Multiple authorization models for different needs
- **Performance**: Caching and optimization for scale
- **Compliance**: Built-in review and audit capabilities
- **Usability**: Templates and self-service for ease of use

The only missing piece is **Role-Based Access Control (RBAC)**, which is planned and will complement the existing layers by providing traditional role-based workflows alongside the advanced ABAC and conditional policy systems.

**For More Information**:
- [Implementation Status](./IMPLEMENTATION_STATUS.md)
- [API Documentation](./API_DOCUMENTATION.md)
- [Security Best Practices](./SECURITY.md)
- [Compliance Guide](./COMPLIANCE.md)

---

**Last Updated**: November 10, 2025  
**Version**: 1.0  
**Status**: Complete except RBAC layer

┌─────────────────────────────────────────────────────────────┐
│                    Authorization Request                     │
│              "Can User X do Action Y on Resource Z?"         │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Step 1: Check Conditional Policies (by priority)           │
│  ├─ Location allowed?                                        │
│  ├─ Time window allowed?                                     │
│  ├─ Device trusted?                                          │
│  ├─ MFA verified? (if required)                             │
│  └─ Risk score acceptable?                                   │
└────────────────────────────┬────────────────────────────────┘
                             │ (If BLOCK → Deny immediately)
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Step 2: Check ABAC Policies                                │
│  ├─ Evaluate attribute-based rules                          │
│  ├─ Match user/resource/context attributes                  │
│  └─ Apply policy effect (Allow/Deny)                        │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Step 3: Check Direct Permissions                           │
│  ├─ Resource permission? (specific to this resource)        │
│  ├─ Content-type permission? (for this type)                │
│  └─ Tenant permission? (organization-wide)                  │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Step 4: Check Role Permissions (⚠️ Not Implemented Yet)    │
│  ├─ Get user's roles                                         │
│  ├─ Aggregate permissions from all roles                    │
│  └─ Check if action is allowed                              │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    Authorization Decision                    │
│                    ✅ Allow  or  ❌ Deny                     │
└─────────────────────────────────────────────────────────────┘