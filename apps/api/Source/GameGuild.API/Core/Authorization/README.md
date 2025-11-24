# Permission-Based Authorization System

## Overview

The GameGuild API implements a comprehensive permission-based authorization system using ASP.NET Core's filter pipeline. This system allows fine-grained control over API endpoints by requiring specific permissions.

## Architecture

The system consists of three main components:

### 1. RequiresPermissionAttribute

A marker attribute that specifies which permissions are required to access a controller or action.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute, IFilterMetadata
{
    public string Name { get; }
    public RequiresPermissionAttribute(string name) => Name = name;
}
```

### 2. PermissionAuthorizationFilter

An `IAsyncAuthorizationFilter` that:

- Validates user authentication
- Reads `RequiresPermissionAttribute` from controllers/actions
- Checks permissions via `IPermissionsContext`
- Returns `401 Unauthorized` if not authenticated
- Returns `403 Forbidden` if missing required permissions
- Allows system admins to bypass permission checks

### 3. IPermissionsContext

A service that provides permission checking for the current user/tenant context:

- `HasTenantPermissionAsync(permission)` - Check tenant-level permissions
- `HasResourcePermissionAsync(resourceType, resourceId, permission)` - Check resource-level permissions
- `IsSystemAdmin` - Check if user is a system administrator
- `GetEffectivePermissionsAsync()` - Get all user permissions

## Usage

### For MVC Controllers

Apply `[RequiresPermission]` attribute to controllers or actions:

```csharp
using GameGuild.API.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

// Require permission for entire controller
[RequiresPermission("Admin.Dashboard")]
public class AdminController : Controller
{
    // Inherits "Admin.Dashboard" requirement from controller
    public IActionResult Index() => View();

    // Requires both "Admin.Dashboard" (from controller) AND "Users.Export"
    [RequiresPermission("Users.Export")]
    public IActionResult ExportUsers() => File(Array.Empty<byte>(), "text/csv");
}

// No controller-level permission
public class UsersController : Controller
{
    // Only requires "Users.Read"
    [RequiresPermission("Users.Read")]
    public IActionResult List() => View();

    // Requires both "Users.Read" AND "Users.Write"
    [RequiresPermission("Users.Read")]
    [RequiresPermission("Users.Write")]
    public IActionResult Create() => View();
}
```

### For Minimal APIs

Use extension methods to add permission requirements:

```csharp
using GameGuild.API.Extensions;

var app = WebApplication.CreateBuilder(args).Build();

// Single permission
app.MapGet("/roles", GetRoles)
    .RequirePermission("Roles.Read")
    .WithName("GetRoles");

// Multiple permissions (user must have ALL)
app.MapPost("/roles", CreateRole)
    .RequirePermissions("Roles.Read", "Roles.Write")
    .WithName("CreateRole");

// Group with common permission
var rolesGroup = app.MapGroup("/roles")
    .RequirePermission("Roles.Read");

rolesGroup.MapGet("/", GetAllRoles);
rolesGroup.MapGet("/{id}", GetRoleById);
```

## Permission Naming Convention

Follow these conventions for permission names:

- **Format**: `{Resource}.{Action}` or `{Module}.{Resource}.{Action}`
- **Examples**:
    - `Users.Read` - Read user data
    - `Users.Write` - Create/update users
    - `Users.Delete` - Delete users
    - `Admin.Dashboard` - Access admin dashboard
    - `Billing.Invoices.Read` - Read invoices in billing module
    - `Content.Posts.Publish` - Publish posts

## Authorization Flow

1. **Request arrives** at the API
2. **Authentication middleware** validates the JWT token
3. **PermissionAuthorizationFilter executes** (globally applied)
4. **Filter checks**:
    - Is user authenticated? → If no, return `401 Unauthorized`
    - Are permissions required? → If no, allow access
    - Is user system admin? → If yes, allow access (bypass checks)
    - Does user have all required permissions? → If no, return `403 Forbidden`
5. **Request continues** to the controller/action

## Bypass Rules

System administrators automatically bypass all permission checks:

- `IPermissionsContext.IsSystemAdmin == true`
- Useful for emergency access and administrative operations
- Still requires authentication

## Integration with Existing Permission System

The filter integrates with GameGuild's three-layer permission model:

### Layer 1: Tenant-Wide Permissions

```csharp
// Check via IPermissionsContext
await _permissions.HasTenantPermissionAsync("Users.Read");
```

### Layer 2: Content-Type Permissions

Handled at the application layer, not by the authorization filter.

### Layer 3: Resource-Specific Permissions

```csharp
// Check via IPermissionsContext
await _permissions.HasResourcePermissionAsync("Post", postId, "Edit");
```

## Configuration

The authorization filter is automatically registered in the DI container:

```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<PermissionAuthorizationFilter>();

// In DependencyInjection.cs - Applied globally to all controllers
services.AddControllers(options => 
{
    options.Filters.Add<PermissionAuthorizationFilter>();
});
```

## Error Responses

### 401 Unauthorized

Returned when the user is not authenticated:

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### 403 Forbidden

Returned when the user lacks required permissions:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have permission to access this resource."
}
```

## Testing

### Unit Testing Controllers with Permission Requirements

```csharp
[Fact]
public async Task Action_WithoutPermission_Returns403()
{
    // Arrange
    var mockPermissions = new Mock<IPermissionsContext>();
    mockPermissions.Setup(p => p.IsAuthenticated).Returns(true);
    mockPermissions.Setup(p => p.IsSystemAdmin).Returns(false);
    mockPermissions.Setup(p => p.HasTenantPermissionAsync("Users.Read", null))
        .ReturnsAsync(false);

    var filter = new PermissionAuthorizationFilter(
        mockPermissions.Object,
        Mock.Of<ILogger<PermissionAuthorizationFilter>>());

    var context = CreateAuthorizationContext();

    // Act
    await filter.OnAuthorizationAsync(context);

    // Assert
    Assert.IsType<ForbidResult>(context.Result);
}
```

## Best Practices

1. **Principle of Least Privilege**: Only grant the minimum permissions needed
2. **Granular Permissions**: Use specific permission names (e.g., `Users.Read` instead of just `Users`)
3. **Consistent Naming**: Follow the `{Resource}.{Action}` convention
4. **Document Permissions**: Keep a centralized list of all permissions in your system
5. **Test Authorization**: Write tests for both authorized and unauthorized access
6. **Audit Permissions**: Log permission checks for security auditing

## Migration from Existing Code

### Before (Basic Authorization)

```csharp
[Authorize] // Only checks authentication
public class UsersController : Controller
{
    public IActionResult List() => View();
}
```

### After (Permission-Based)

```csharp
[RequiresPermission("Users.Read")] // Checks specific permission
public class UsersController : Controller
{
    public IActionResult List() => View();
}
```

## Related Documentation

- [DAC Strategy](../../../docs/architecture/DAC-STRATEGY.md) - Overall permission architecture
- [Permissions Module](../Modules/GameGuild.Permissions/) - Permission domain logic
- [Authentication Module](../Modules/GameGuild.Authentication/) - User authentication

## Future Enhancements

- **Policy-Based Authorization**: Migrate to ASP.NET Core policy system for more flexibility
- **Caching**: Cache permission results to reduce database queries
- **Permission Templates**: Define reusable permission sets (e.g., "Editor" role)
- **Dynamic Permissions**: Load permissions from database at runtime
- **Resource-Level Filters**: Add attribute for resource-specific permission checks
