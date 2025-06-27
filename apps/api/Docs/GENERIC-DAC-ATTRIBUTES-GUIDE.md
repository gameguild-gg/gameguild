# Generic DAC Attributes Usage Guide

## Overview

The DAC (Discretionary Access Control) system now supports **generic attributes** that eliminate the need to create resource-specific permission attributes. You can use the generic `RequireResourcePermissionAttribute<TPermission, TResource>` or the backward-compatible `RequireResourcePermissionAttribute<TResource>`.

## Available Generic Attributes

### 1. Tenant-Level Permission Attribute
```csharp
[RequireTenantPermission(PermissionType.Create)]
```
**Usage:** Operations that affect the entire tenant (user management, tenant settings)

### 2. Content-Type Permission Attribute
```csharp
[RequireContentTypePermission<Product>(PermissionType.Read)]
[RequireContentTypePermission<Comment>(PermissionType.Review)]
```
**Usage:** Operations on entity collections or types (list all products, create new comment)

### 3. Resource-Level Permission Attributes

#### Option A: Backward-Compatible Single Parameter (Recommended)
```csharp
[RequireResourcePermission<Product>(PermissionType.Update)]
[RequireResourcePermission<Comment>(PermissionType.Delete)]
[RequireResourcePermission<Product>(PermissionType.Update, "productId")] // Custom parameter name
```

#### Option B: Explicit Two-Parameter (Type-Safe)
```csharp
[RequireResourcePermission<ProductPermission, Product>(PermissionType.Update)]
[RequireResourcePermission<CommentPermission, Comment>(PermissionType.Delete)]
```

## Usage Examples

### Product Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    // Content-type level operations
    [HttpGet]
    [RequireContentTypePermission<Product>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts() { ... }

    [HttpPost]
    [RequireContentTypePermission<Product>(PermissionType.Create)]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductDto dto) { ... }

    // Resource-level operations (backward-compatible)
    [HttpGet("{id}")]
    [RequireResourcePermission<Product>(PermissionType.Read)]
    public async Task<ActionResult<Product>> GetProduct(Guid id) { ... }

    [HttpPut("{id}")]
    [RequireResourcePermission<Product>(PermissionType.Update)]
    public async Task<ActionResult<Product>> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto) { ... }

    [HttpDelete("{id}")]
    [RequireResourcePermission<Product>(PermissionType.Delete)]
    public async Task<ActionResult> DeleteProduct(Guid id) { ... }

    // Custom parameter name example
    [HttpPut("bundle/{bundleId}/products/{productId}")]
    [RequireResourcePermission<Product>(PermissionType.Update, "productId")]
    public async Task<ActionResult> UpdateProductInBundle(Guid bundleId, Guid productId, [FromBody] UpdateProductDto dto) { ... }
}
```

### Comment Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class CommentController : ControllerBase
{
    // Content-type level operations
    [HttpGet]
    [RequireContentTypePermission<Comment>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments() { ... }

    [HttpPost]
    [RequireContentTypePermission<Comment>(PermissionType.Create)]
    public async Task<ActionResult<Comment>> CreateComment([FromBody] CreateCommentDto dto) { ... }

    [HttpGet("pending-review")]
    [RequireContentTypePermission<Comment>(PermissionType.Review)]
    public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsForReview() { ... }

    // Resource-level operations
    [HttpGet("{id}")]
    [RequireResourcePermission<Comment>(PermissionType.Read)]
    public async Task<ActionResult<Comment>> GetComment(Guid id) { ... }

    [HttpPut("{id}")]
    [RequireResourcePermission<Comment>(PermissionType.Update)]
    public async Task<ActionResult<Comment>> UpdateComment(Guid id, [FromBody] UpdateCommentDto dto) { ... }

    [HttpDelete("{id}")]
    [RequireResourcePermission<Comment>(PermissionType.Delete)]
    public async Task<ActionResult> DeleteComment(Guid id) { ... }

    [HttpPost("{id}/approve")]
    [RequireResourcePermission<Comment>(PermissionType.Approve)]
    public async Task<ActionResult<Comment>> ApproveComment(Guid id) { ... }
}
```

### GraphQL Implementation Example

The same DAC attributes work seamlessly with **HotChocolate GraphQL** using custom extension methods:

```csharp
[ExtendObjectType<Query>]
public class ProgramContentQueries
{
    // Content-type level operations
    [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
    public async Task<IEnumerable<ProgramContent>> GetProgramContents(
        [Service] IProgramContentService contentService)
    {
        return await contentService.GetAllContentAsync();
    }

    // Resource-level operations (with permission inheritance)
    [RequireResourcePermission<Program>(PermissionType.Read, "programId")]
    public async Task<ProgramContent?> GetProgramContentById(
        Guid id,
        [Service] IProgramContentService programContentService)
    {
        return await programContentService.GetContentByIdAsync(id);
    }
}

[ExtendObjectType<Mutation>]
public class ProgramContentMutations
{
    // Resource-level operations (checking parent Program permissions)
    [RequireResourcePermission<Program>(PermissionType.Create, "programId")]
    public async Task<ProgramContent> CreateContentAsync(
        [Service] IProgramContentService contentService,
        Guid programId,
        string title,
        ProgramContentTypeEnum type,
        string body,
        string description)
    {
        return await contentService.CreateContentAsync(programId, title, type, body, description);
    }

    [RequireResourcePermission<Program>(PermissionType.Update, "programId")]
    public async Task<ProgramContent> UpdateContentAsync(
        [Service] IProgramContentService contentService,
        Guid id,
        string? title = null,
        string? body = null,
        string? description = null)
    {
        return await contentService.UpdateContentAsync(id, title, body, description);
    }
}
```

#### GraphQL DAC Extension Methods

The GraphQL implementation uses these extension methods for field-level authorization:

```csharp
// Tenant-level permission
.RequireTenantPermission(PermissionType.ReadContent)

// Content-type permission  
.RequireContentTypePermission<ProgramContent>(PermissionType.CreateContent)

// Resource-level permission with parameter mapping
.RequireResourcePermission<Program>(PermissionType.UpdateProgram, "programId")
```

#### Permission Inheritance in GraphQL

Notice how **ProgramContent** operations check **Program permissions** instead of ProgramContent permissions:

```csharp
// ProgramContent inherits permissions from parent Program
[RequireResourcePermission<Program>(PermissionType.Read, "programId")]  // ← Checks Program permissions
public async Task<ProgramContent?> GetProgramContentById(Guid id) { ... }
```

This demonstrates the **permission inheritance pattern** where child entities derive authorization from their parent resources.

## How It Works

### Hierarchical Permission Checking

The generic attributes implement a **3-level hierarchical fallback** system:

1. **Resource-Level Check**: Check if user has specific permission for the individual resource
2. **Content-Type-Level Check**: If resource-level fails, check if user has permission for the content type
3. **Tenant-Level Check**: If content-type-level fails, check if user has tenant-wide permission

### Automatic Type Resolution

The **backward-compatible single parameter version** (`RequireResourcePermission<TResource>`) automatically resolves the permission entity type based on naming convention:

- `Product` → `ProductPermission`
- `Comment` → `CommentPermission`
- `User` → `UserPermission` (if it exists)

If you need explicit control or the naming convention doesn't work, use the **two-parameter version**.

## Migration from Specific Attributes

### Before (Resource-Specific Attributes - No Longer Needed)
```csharp
// These approaches required creating separate attribute classes for each resource type
[HttpGet("{id}")]
public async Task<ActionResult<Product>> GetProduct(Guid id) { ... } // Needed ProductPermissionAttribute

[HttpGet("{id}")]  
public async Task<ActionResult<Comment>> GetComment(Guid id) { ... } // Needed CommentPermissionAttribute
```

### After (Generic Attributes - Current Approach)
```csharp
// Single generic implementation works for all resource types
[RequireResourcePermission<Product>(PermissionType.Update)]
[RequireResourcePermission<Comment>(PermissionType.Delete)]
```

## Best Practices

### 1. Choose the Right Permission Level

- **Tenant-level**: User management, tenant settings, global configurations
- **Content-type-level**: List entities, create new entities, bulk operations
- **Resource-level**: View/edit/delete specific entities, resource-specific operations

### 2. Use Specific Permission Types

```csharp
// Good - Specific permission for the operation
[RequireContentTypePermission<Comment>(PermissionType.Review)]
public async Task<ActionResult> GetCommentsForReview() { }

// Avoid - Overly broad permission
[RequireContentTypePermission<Comment>(PermissionType.All)]
public async Task<ActionResult> GetCommentsForReview() { }
```

### 3. Consistent Parameter Naming

Use consistent route parameter names for resource IDs:
```csharp
// Preferred - uses default "id" parameter
[HttpGet("{id}")]
[RequireResourcePermission<Product>(PermissionType.Read)]
public async Task<ActionResult<Product>> GetProduct(Guid id) { ... }

// When needed - specify custom parameter name
[HttpGet("custom/{productId}")]
[RequireResourcePermission<Product>(PermissionType.Read, "productId")]
public async Task<ActionResult<Product>> GetProductById(Guid productId) { ... }
```

## Adding New Resource Types

To add DAC support for a new resource type:

1. **Create the permission entity:**
   ```csharp
   public class MyResourcePermission : ResourcePermission<MyResource>
   {
       // Add computed properties for resource-specific permissions
       public bool CanDoSomething => HasPermission(PermissionType.SomePermission) && IsValid;
   }
   ```

2. **Register in ApplicationDbContext:**
   ```csharp
   public DbSet<MyResourcePermission> MyResourcePermissions { get; set; }
   ```

3. **Add to the generic attribute switch statement** (for backward compatibility):
   ```csharp
   // In RequireResourcePermissionAttribute<TResource>.OnAuthorizationAsync()
   case "MyResource":
       var hasMyResourcePermission = await permissionService.HasResourcePermissionAsync<MyResourcePermission, MyResource>(
           userId, tenantId, resourceId, _requiredPermission);
       if (hasMyResourcePermission)
       {
           return; // Permission granted at resource level
       }
       break;
   ```

4. **Create database migration:**
   ```bash
   dotnet ef migrations add AddMyResourcePermission
   ```

5. **Use in controllers and GraphQL resolvers:**
   ```csharp
   // MVC Controller
   [RequireResourcePermission<MyResource>(PermissionType.Read)]
   public async Task<ActionResult<MyResource>> GetMyResource(Guid id) { ... }
   
   // GraphQL Query
   [RequireResourcePermission<MyResource>(PermissionType.Read)]
   public async Task<MyResource?> GetMyResourceById(Guid id, [Service] IMyResourceService service) { ... }
   ```

## Backwards Compatibility

- The system now uses only generic attributes - no resource-specific attributes are needed
- All functionality is provided by the three core generic attributes:
  - `RequireTenantPermissionAttribute` 
  - `RequireContentTypePermissionAttribute<TResource>`
  - `RequireResourcePermissionAttribute<TResource>` or `RequireResourcePermissionAttribute<TPermission, TResource>`
- Clean, maintainable architecture with no deprecated code

## Performance Considerations

- **Resource-level** checks are the most expensive (database queries for specific permissions)
- **Content-type-level** checks are moderately expensive (user permissions for entity types)
- **Tenant-level** checks are the least expensive (user's tenant-wide permissions)
- The hierarchical fallback ensures optimal performance by checking most specific first

## Conclusion

The generic DAC attributes provide:
- ✅ **Scalability**: No need to create attributes for each resource type
- ✅ **Type Safety**: Generic parameters ensure compile-time type checking
- ✅ **Flexibility**: Both explicit and inferred permission type resolution
- ✅ **Backward Compatibility**: Existing code continues to work
- ✅ **Performance**: Hierarchical permission checking with optimal fallback
- ✅ **Maintainability**: Single implementation for all resource types
