# User and Tenant Context Middleware Implementation

## Overview

The User and Tenant Context system provides a clean way to access user authentication information and tenant-specific data throughout your application. The system is built with:

- **IUserContext**: Interface for accessing current user information
- **ITenantContext**: Interface for accessing current tenant information  
- **ContextMiddleware**: Middleware that sets up context for each request
- **Dependency Injection**: Automatic registration of context services

## Implementation Details

### 1. Context Interfaces

#### IUserContext
```csharp
public interface IUserContext
{
    Guid? UserId { get; }           // Current user ID
    string? Email { get; }          // User email address
    string? Name { get; }           // User display name
    IDictionary<string, object> Claims { get; } // All user claims
    bool IsAuthenticated { get; }   // Authentication status
    bool IsInRole(string role);     // Role checking
    IEnumerable<string> Roles { get; } // User roles
}
```

#### ITenantContext
```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }         // Current tenant ID
    string? TenantName { get; }     // Tenant name
    IDictionary<string, object> Settings { get; } // Tenant settings
    bool IsActive { get; }          // Tenant status
    string? SubscriptionPlan { get; } // Tenant subscription plan
}
```

### 2. Middleware Pipeline Integration

The context middleware is automatically registered in the application pipeline:

```csharp
// In ConfigurePipeline method (WebApplicationBuilderExtensions.cs)
app.UseAuthentication();
app.UseContextMiddleware();  // <- Added here
app.UseAuthorization();
```

### 3. Service Registration

Context services are automatically registered in dependency injection:

```csharp
// In AddApplication method (DependencyInjection.cs)
services.AddContextServices();  // <- Added here
```

The `AddContextServices()` extension method registers:
- `HttpContextAccessor`
- `IUserContext` implementation
- `ITenantContext` implementation

## Usage Examples

### In Controllers

```csharp
[ApiController]
public class MyController : ControllerBase
{
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;

    public MyController(IUserContext userContext, ITenantContext tenantContext)
    {
        _userContext = userContext;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public IActionResult GetData()
    {
        // Check authentication
        if (!_userContext.IsAuthenticated)
            return Unauthorized();

        // Check roles
        if (!_userContext.IsInRole("Admin"))
            return Forbid();

        // Access user information
        var userId = _userContext.UserId;
        var userEmail = _userContext.Email;

        // Access tenant information
        var tenantId = _tenantContext.TenantId;
        var tenantName = _tenantContext.TenantName;

        return Ok(new { userId, userEmail, tenantId, tenantName });
    }
}
```

### In MediatR Handlers

```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;

    public CreateProductHandler(
        ApplicationDbContext context,
        IUserContext userContext,
        ITenantContext tenantContext)
    {
        _context = context;
        _userContext = userContext;
        _tenantContext = tenantContext;
    }

    public async Task<ProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Validate user authentication
        if (!_userContext.IsAuthenticated)
            return ProductResult.Failure("User must be authenticated");

        // Check user permissions
        if (!_userContext.IsInRole("ContentCreator"))
            return ProductResult.Failure("Insufficient permissions");

        // Create product with user and tenant context
        var product = new Product
        {
            Name = request.Name,
            CreatedBy = _userContext.UserId.Value,
            TenantId = _tenantContext.TenantId,
            // ... other properties
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return ProductResult.Success(product);
    }
}
```

### In GraphQL Resolvers

```csharp
public class ProductQueries
{
    public async Task<IEnumerable<Product>> GetUserProductsAsync(
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        [Service] ITenantContext tenantContext)
    {
        // Context is automatically injected
        var query = new GetUserProductsQuery
        {
            UserId = userContext.UserId ?? Guid.Empty,
            TenantId = tenantContext.TenantId
        };

        return await mediator.Send(query);
    }
}
```

## Context Data Sources

### User Context Data Sources
- **Claims**: Standard ASP.NET Core claims from JWT tokens or cookies
- **ClaimTypes.NameIdentifier**: User ID (mapped to UserId)
- **ClaimTypes.Email**: User email address
- **ClaimTypes.Name**: User display name
- **ClaimTypes.Role**: User roles

### Tenant Context Data Sources
- **HTTP Headers**: `X-Tenant-Id`, `X-Tenant-Name`
- **Query Parameters**: `tenantId`, `tenant`
- **JWT Claims**: `tenant`, `tenant_id`, `tenant_name`
- **Subdomain**: Extracted from request host

## Security Features

### Authentication Validation
- Automatically checks if user is authenticated
- Provides `IsAuthenticated` property for easy checking
- Handles both authenticated and anonymous requests

### Role-Based Access Control
- `IsInRole(string role)` method for role checking
- `Roles` property returns all user roles
- Works with standard ASP.NET Core role claims

### Tenant Isolation
- Automatic tenant context extraction
- Tenant-specific data filtering
- Multi-tenant application support

## Error Handling

The middleware includes comprehensive error handling:
- Logs context information for debugging
- Handles missing or invalid tenant information
- Gracefully handles authentication failures
- Warns when authenticated users lack tenant context

## Testing

The system includes comprehensive tests:
- Unit tests for context services
- Integration tests for middleware
- Example usage patterns

See `ContextMiddlewareTests.cs` for testing examples.

## Benefits

1. **Clean Architecture**: Context is injected where needed
2. **Consistent Access**: Same interface across controllers, handlers, and resolvers
3. **Security**: Built-in authentication and authorization helpers
4. **Multi-Tenancy**: Automatic tenant context handling
5. **Testability**: Easy to mock and test context behavior
6. **Performance**: Scoped services with minimal overhead

## Configuration

The context system works out of the box with minimal configuration. The middleware and services are automatically registered during application startup.

For custom tenant resolution or additional claims processing, you can extend the `UserContext` and `TenantContext` services by inheriting from them or implementing the interfaces directly.
