# ProgramContent Permission Inheritance Implementation

## Overview

This document describes the implementation of proper permission inheritance for ProgramContent entities in the GameGuild API. The key architectural change is that **ProgramContent permissions now inherit from their parent Program entity** rather than having separate ProgramContentPermission entries.

## Architecture Changes

### Before (Incorrect Architecture)
- ProgramContent had its own separate `ProgramContentPermission` entities
- Each content item could have individual permission entries
- Led to complex permission management and potential inconsistencies

### After (Correct Architecture) 
- ProgramContent permissions inherit from parent `Program` entity
- Uses existing `ProgramPermission` for all Program-related operations
- Consistent with domain modeling - content belongs to a program

## Permission Hierarchy

The 3-layer DAC (Discretionary Access Control) system now works as follows for ProgramContent:

### Layer 1: Tenant Level
- Global permissions across the entire tenant

### Layer 2: Content-Type Level  
- Permissions on `ProgramContent` entity type (for some operations)

### Layer 3: Resource Level
- Permissions on the **parent Program** entity (most operations)

## GraphQL Implementation Changes

### Queries
All ProgramContent queries now use Program permissions:

```csharp
// Individual content access - requires Program permission
[RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(PermissionType.Read, "programId")]
public async Task<ProgramContentEntity?> GetProgramContentById(Guid id, [Service] IProgramContentService service)

// Program-specific content collections - requires Program permission  
[RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(PermissionType.Read, "programId")]
public async Task<IEnumerable<ProgramContentEntity>> GetProgramContents(Guid programId, [Service] IProgramContentService service)
```

### Mutations
All ProgramContent mutations now use Program permissions:

```csharp
// Create content - requires Program Create permission
[RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(PermissionType.Create, "programId")]
public async Task<ProgramContent> CreateContentAsync(...)

// Update/Delete content - requires Program Edit/Delete permission
[RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(PermissionType.Edit, "programId")]
public async Task<ProgramContent> UpdateContentAsync(...)
```

## Parameter Resolution

### Challenge
Some operations (like `GetProgramContentById`, `UpdateContentAsync`) only receive a `contentId` but need to check permissions on the parent `programId`.

### Solution
The DAC attribute system needs to resolve the `programId` from the content's `ProgramId` property. This is noted in the implementation comments:

```csharp
/// Note: The programId will be resolved from the content's ProgramId property
```

## Implementation Status

### ✅ Completed
- [x] Updated GraphQL queries to use `ProgramPermission` instead of `ProgramContentPermission`
- [x] Updated GraphQL mutations to use Program-based permissions
- [x] Updated permission hierarchy documentation
- [x] Build verification - no compilation errors

### ⚠️ Pending Technical Issues

1. **GraphQL Authorization Integration**: The current DAC attributes are designed for MVC controllers (`IAsyncAuthorizationFilter`) and may not work directly with HotChocolate GraphQL. This needs investigation and possibly custom GraphQL authorization handlers.

2. **Parameter Resolution**: For operations that only have `contentId`, the system needs to resolve the parent `programId` for permission checking. This may require:
   - Custom attribute logic to fetch the content and extract `ProgramId`
   - Service-level permission checks
   - GraphQL middleware for permission validation

3. **Database Cleanup**: The `ProgramContentPermission` model and related database entries should be removed as they are no longer needed.

## Future Improvements

1. **GraphQL-Specific Authorization**: Implement HotChocolate-compatible authorization attributes
2. **Service-Level Permissions**: Add permission checks directly in service methods as fallback
3. **Database Migration**: Remove `ProgramContentPermission` table and references
4. **Integration Testing**: Verify permission inheritance works correctly in practice

## Benefits

- **Simplified Permission Model**: No separate content permissions to manage
- **Consistent Domain Logic**: Content permissions follow Program ownership
- **Reduced Complexity**: Fewer permission entities and relationships
- **Better Security**: Centralized control through Program permissions

## Notes

This implementation establishes the correct permission architecture even if the technical integration with GraphQL authorization needs further work. The business logic and permission hierarchy are now properly aligned with the domain model.
