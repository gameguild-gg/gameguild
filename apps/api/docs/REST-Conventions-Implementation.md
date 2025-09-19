# REST API Conventions Implementation

This document describes the comprehensive REST API conventions implemented for the GameGuild API, including versioning, status codes, ETag support, and response standardization.

## Overview

The REST conventions implementation provides:

1. **API Versioning** - Multiple versioning strategies with URL segments, query parameters, and headers
2. **Standardized Status Codes** - Consistent HTTP status code usage across all endpoints
3. **ETag Support** - Optimistic concurrency control with If-Match headers
4. **Response Standardization** - Uniform response format across all endpoints
5. **Error Handling** - Consistent error responses with proper status codes

## API Versioning

### Supported Strategies

The API supports multiple versioning strategies that can be combined:

- **URL Segment**: `/api/v1/users` or `/api/v2/users`
- **Query Parameter**: `/api/users?version=1.0`
- **Header**: `X-Version: 1.0`
- **Media Type**: `Accept: application/vnd.api.v1+json`

### Configuration

```csharp
services.AddRestApiVersioning(options =>
{
    options.Strategy = ApiVersionStrategy.UrlSegmentAndQuery;
    options.SupportedVersions = new List<ApiVersion> 
    { 
        new(1, 0),
        new(1, 1),
        new(2, 0)
    };
    options.DeprecatedVersions = new List<ApiVersion>();
    options.IncludeVersionInSwagger = true;
});
```

### Version Headers

The API automatically adds version information to response headers:

- `X-API-Version`: Current API version used
- `X-Supported-Versions`: All supported versions
- `Warning`: Deprecation warnings for deprecated versions

## HTTP Status Codes

### Success Codes (2xx)

| Code | Usage | When to Use |
|------|-------|-------------|
| 200 OK | Standard success response | GET requests, PUT/PATCH updates that return data |
| 201 Created | Resource creation success | POST requests that create new resources |
| 202 Accepted | Async processing | Long-running operations, background processing |
| 204 No Content | Success without response body | DELETE requests, PUT updates without returned data |

### Client Error Codes (4xx)

| Code | Usage | When to Use |
|------|-------|-------------|
| 400 Bad Request | Invalid request format | Malformed JSON, invalid parameters |
| 401 Unauthorized | Authentication required | Missing or invalid authentication |
| 403 Forbidden | Insufficient permissions | Valid auth but no access to resource |
| 404 Not Found | Resource doesn't exist | Non-existent endpoints or resources |
| 409 Conflict | Resource conflict | Duplicate creation, concurrent modifications |
| 412 Precondition Failed | ETag validation failed | If-Match header mismatch |
| 422 Unprocessable Entity | Business rule violation | Valid format but invalid business logic |

### Server Error Codes (5xx)

| Code | Usage | When to Use |
|------|-------|-------------|
| 500 Internal Server Error | Unexpected server error | Unhandled exceptions |
| 502 Bad Gateway | External service failure | Database/external API failures |
| 503 Service Unavailable | Temporary unavailability | Maintenance mode, overload |

## ETag and Optimistic Concurrency Control

### Automatic ETag Generation

ETags are automatically generated for resources with version numbers:

```csharp
var etag = ETagHelper.GenerateETag(resource.Version);
Response.Headers.ETag = etag;
```

### If-Match Validation

Update operations validate If-Match headers:

```csharp
[HttpPut("{id}")]
public async Task<ActionResult> UpdateResource(int id, UpdateDto dto)
{
    if (dto.ExpectedVersion.HasValue && !ValidateIfMatch(dto.ExpectedVersion.Value))
    {
        return PreconditionFailedError("Resource has been modified");
    }
    
    // Proceed with update...
}
```

### ETag Attributes

Use attributes for automatic ETag handling:

```csharp
[ETag]                    // Automatic ETag generation
[IfMatchValidation]       // Automatic If-Match validation
public class MyController : VersionedRestController
```

## Response Standardization

### Standard Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "data": { /* actual response data */ },
  "message": "Operation completed successfully",
  "errors": null,
  "metadata": {
    "totalCount": 100,
    "page": 1,
    "hasMore": true
  }
}
```

### Success Response Examples

#### GET Request (200 OK)
```json
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "John Doe",
    "email": "john@example.com",
    "version": 5
  },
  "message": null,
  "metadata": null
}
```

#### POST Request (201 Created)
```json
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "John Doe",
    "email": "john@example.com",
    "version": 1
  },
  "message": "User created successfully",
  "metadata": null
}
```

### Error Response Examples

#### Validation Error (400 Bad Request)
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Name: Name is required",
    "Email: Invalid email format"
  ]
}
```

#### Not Found Error (404 Not Found)
```json
{
  "success": false,
  "data": null,
  "message": "Resource not found",
  "errors": [
    "User with ID 123 not found"
  ]
}
```

#### Conflict Error (409 Conflict)
```json
{
  "success": false,
  "data": null,
  "message": "Resource conflict",
  "errors": [
    "Email address already exists"
  ]
}
```

## Controller Implementation

### Base Controllers

Use the provided base controllers for consistent behavior:

```csharp
// For versioned APIs
public class MyController : VersionedRestController
{
    public MyController(ILogger<MyController> logger) : base(logger) { }
}

// For non-versioned APIs
public class MyController : RestControllerBase
{
    public MyController(ILogger<MyController> logger) : base(logger) { }
}
```

### Attribute Usage

Apply attributes for enhanced functionality:

```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ETag]                      // Automatic ETag support
[IfMatchValidation]         // Automatic If-Match validation
[EnforceStatusCodes]        // Status code validation
public class UsersV2Controller : VersionedRestController
```

### Standard Methods

Implement standard CRUD operations with proper status codes:

```csharp
[HttpGet]
[ProducesResponseType<ApiResponse<IEnumerable<UserDto>>>(200)]
[ProducesResponseType<ApiResponse<object>>(403)]
public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers()
{
    var users = await GetUsersFromService();
    return OkWithETag(users, "Users retrieved successfully");
}

[HttpPost]
[ProducesResponseType<ApiResponse<UserDto>>(201)]
[ProducesResponseType<ApiResponse<object>>(400)]
[ProducesResponseType<ApiResponse<object>>(409)]
public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto dto)
{
    if (!ModelState.IsValid)
        return ValidationError(ModelState);
        
    var user = await CreateUserInService(dto);
    var location = Url.Action(nameof(GetUser), new { id = user.Id });
    return CreatedWithLocation(user, location!);
}

[HttpPut("{id}")]
[ProducesResponseType<ApiResponse<UserDto>>(200)]
[ProducesResponseType<ApiResponse<object>>(400)]
[ProducesResponseType<ApiResponse<object>>(404)]
[ProducesResponseType<ApiResponse<object>>(412)]
public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto dto)
{
    if (!ValidateIfMatch(dto.ExpectedVersion))
        return PreconditionFailedError();
        
    var user = await UpdateUserInService(id, dto);
    return OkWithETag(user, "User updated successfully");
}

[HttpDelete("{id}")]
[ProducesResponseType(204)]
[ProducesResponseType<ApiResponse<object>>(404)]
public async Task<IActionResult> DeleteUser(int id)
{
    await DeleteUserInService(id);
    return NoContent();
}
```

## Middleware Configuration

### Service Registration

Add REST conventions to the service collection:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.SetupRestConventions(configuration);
}
```

### Pipeline Configuration

Configure the middleware pipeline:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseRestConventions();      // Error handling and response formatting
    app.UseRestApiVersioning();    // Version headers and validation
    
    // Other middleware...
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

## Documentation Integration

### Swagger/OpenAPI

The implementation automatically configures Swagger for multiple versions:

- Separate Swagger documents for each API version
- Deprecation warnings in documentation
- Version-specific endpoint grouping

### Version Documentation

Access different API versions in Swagger:
- `/swagger/v1/swagger.json` - API v1.0 documentation
- `/swagger/v2/swagger.json` - API v2.0 documentation

## Best Practices

### 1. Version Management

- Start with v1.0 for initial release
- Use semantic versioning (major.minor)
- Deprecate old versions gradually
- Provide migration guides

### 2. Status Code Usage

- Always use semantically correct status codes
- Include descriptive error messages
- Provide actionable error information
- Use 4xx for client errors, 5xx for server errors

### 3. ETag Implementation

- Always include ETags for resources with versions
- Validate If-Match headers for updates
- Return 412 Precondition Failed for ETag mismatches
- Use strong ETags based on version numbers

### 4. Response Consistency

- Always use the standard response format
- Include meaningful messages
- Provide metadata for collection responses
- Use consistent field naming (camelCase)

### 5. Error Handling

- Return appropriate status codes
- Include correlation IDs for debugging
- Provide specific error messages
- Log errors with sufficient context

## Migration Guide

### Existing Controllers

To migrate existing controllers to use REST conventions:

1. **Change base class**: Inherit from `VersionedRestController` or `RestControllerBase`
2. **Add attributes**: Apply `[ETag]`, `[IfMatchValidation]`, and `[EnforceStatusCodes]`
3. **Update responses**: Use helper methods like `OkWithETag()`, `CreatedWithLocation()`
4. **Add versioning**: Include `[ApiVersion]` and update routes
5. **Update status codes**: Use proper HTTP status codes for each scenario

### Example Migration

Before:
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await GetUserFromService(id);
        return Ok(user);
    }
}
```

After:
```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ETag]
public class UsersController : VersionedRestController
{
    [HttpGet("{id}")]
    [ProducesResponseType<ApiResponse<UserDto>>(200)]
    [ProducesResponseType<ApiResponse<object>>(404)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var user = await GetUserFromService(id);
        if (user == null)
            return NotFoundError($"User with ID {id} not found");
            
        var etag = ETagHelper.GenerateETag(user.Version);
        return OkWithETag(user, etag);
    }
}
```

This implementation provides a comprehensive, production-ready REST API with proper versioning, status codes, concurrency control, and response standardization.
