# User Entity Sync Strategy

**Date**: January 12, 2026  
**Status**: ✅ COMPLETED - ENTITIES MERGED

> **⚠️ OBSOLETE DOCUMENT**: This document described the former dual-entity architecture.
> As of January 12, 2026, the `AuthUser` entity has been **completely removed** and merged into the `User` entity.
> All authentication handlers now use `IUserRepository` instead of `IAuthUserRepository`.
> See [AUTHORIZATION_VALIDATION_REPORT.md](../../apps/api/AUTHORIZATION_VALIDATION_REPORT.md) for details.

---

## Historical Context (Archived)

The system previously had **two separate user entities** which have now been merged.

### AuthUser (Authentication Module)

**Location**: `GameGuild.Identity.Authentication/Entities/AuthUser.cs`

**Purpose**: Stores authentication credentials

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key (shared with User) |
| `Email` | `string` | Unique email address |
| `Username` | `string?` | Optional username |
| `PasswordHash` | `string` | BCrypt password hash |
| `CreatedAt` | `DateTime` | Creation timestamp |
| `UpdatedAt` | `DateTime` | Last update timestamp |

### User (Users Module)

**Location**: `GameGuild.Identity.Users/Entities/User.cs`

**Purpose**: Stores profile and status information

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key (shared with AuthUser) |
| `Email` | `string` | Unique email address |
| `Name` | `string` | Display name |
| `PhoneNumber` | `string?` | Optional phone |
| `IsActive` | `bool` | Account active status |
| `IsSuspended` | `bool` | Suspension status |
| `LastSeenAt` | `DateTime?` | Last activity timestamp |
| *EntityBase fields* | | Version, timestamps, soft delete |

---

## Problems with Dual Entities

| Problem | Impact | Severity |
|---------|--------|----------|
| **Data Duplication** | Email stored in both entities | Medium |
| **Sync Complexity** | Changes must update both entities | High |
| **Inconsistency Risk** | Entities can drift out of sync | High |
| **Query Complexity** | Joins required for full user data | Medium |
| **Transaction Scope** | Cross-module transactions needed | Medium |

---

## Current Sync Strategy

Until merged, the following strategy maintains consistency:

### 1. Creation Sync

When a user registers:

```csharp
// AuthService.SignUp creates AuthUser
var authUser = new AuthUser { 
    Id = Guid.NewGuid(), 
    Email = email,
    PasswordHash = hash 
};
await _authUserRepository.AddAsync(authUser);

// Event published for User creation
await _eventBus.PublishAsync(new UserRegisteredEvent(authUser.Id, email));

// UserEventHandler creates User
public async Task Handle(UserRegisteredEvent @event)
{
    var user = User.Create(@event.Email, @event.Email); // Name defaults to email
    user.Id = @event.UserId; // Use same ID as AuthUser
    await _userRepository.AddAsync(user);
}
```

### 2. Email Update Sync

Email changes must update both entities:

```csharp
// UserCommandHandler
public async Task Handle(UpdateEmailCommand cmd)
{
    // Update User
    var user = await _userRepository.GetByIdAsync(cmd.UserId);
    user.Email = cmd.NewEmail;
    
    // Publish event for AuthUser sync
    await _eventBus.PublishAsync(new UserEmailChangedEvent(cmd.UserId, cmd.NewEmail));
}

// AuthEventHandler
public async Task Handle(UserEmailChangedEvent @event)
{
    var authUser = await _authUserRepository.GetByIdAsync(@event.UserId);
    authUser.Email = @event.NewEmail;
    await _authUserRepository.UpdateAsync(authUser);
}
```

### 3. Deletion Sync

Soft-delete in User should deactivate AuthUser:

```csharp
public async Task Handle(UserDeletedEvent @event)
{
    // AuthUser doesn't have soft-delete, but we can track status
    // Option 1: Delete AuthUser (blocks login)
    // Option 2: Keep AuthUser but set flag (not currently supported)
    
    // Current approach: Delete AuthUser to prevent login
    await _authUserRepository.DeleteAsync(@event.UserId);
}
```

---

## Migration Plan: Merge to Single Entity (v2.0)

### Target Entity: UnifiedUser

```csharp
[Table("Users")]
public class UnifiedUser : EntityBase
{
    // Identity
    public required string Email { get; set; }
    public string? Username { get; set; }
    public required string Name { get; set; }
    
    // Authentication
    public string? PasswordHash { get; set; }  // Null for OAuth-only users
    
    // OAuth Identities (JSON or separate table)
    public List<OAuthIdentity>? OAuthIdentities { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsSuspended { get; set; }
    public bool IsEmailVerified { get; set; }
    
    // Profile
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    
    // Activity
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

### Migration Steps

1. **Create UnifiedUser entity** in new `GameGuild.Identity.Core` module
2. **Add migration** to copy data from AuthUsers + Users
3. **Update repositories** to use UnifiedUser
4. **Update services** to single entity operations
5. **Deprecate** AuthUser and User entities
6. **Remove** old tables after verification period

### Breaking Changes

| Component | Change Required |
|-----------|----------------|
| `IAuthUserRepository` | Merge into `IUserRepository` |
| `AuthService` | Update to use `IUserRepository` |
| `UserController` | No change (abstracts entity) |
| DB Queries | Update to single table |

---

## Interim Recommendations

Until the merge is complete:

1. **Always use same ID** for AuthUser and User
2. **Publish domain events** for cross-module sync
3. **Use transactions** when updating both entities
4. **Query by ID** (consistent), not email (may drift)
5. **Add integration tests** for sync scenarios

---

## Related Documentation

- [User Entity](../../apps/api/Source/Modules/GameGuild.Identity.Users/Entities/User.cs)
- [AuthUser Entity](../../apps/api/Source/Modules/GameGuild.Identity.Authentication/Entities/AuthUser.cs)
- [Permission Evaluation Policy](PERMISSION_EVALUATION_POLICY.md)

---

**Document Owner**: Identity Team  
**Migration Target**: v2.0  
**Review Cycle**: Per release
