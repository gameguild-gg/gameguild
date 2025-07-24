# Clean Architecture Integration Guide

## Overview

The new DependencyInjection.cs file introduces a Clean Architecture approach that can be integrated with the existing
GameGuild architecture. Here's how to modernize the current setup:

## Current vs. Improved Architecture

### Current Program.cs Integration

```csharp
// Replace the existing service registration pattern with:

var builder = WebApplication.CreateBuilder(args);

// Add Clean Architecture layers
builder.Services.AddPresentation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Add existing database and authentication (keep existing)
builder.Services.AddDbContext<ApplicationDbContext>(options => { /* existing config */ });
builder.Services.AddAuthentication(/* existing config */);
builder.Services.AddAuthorization(/* existing config */);

var app = builder.Build();

// Configure Clean Architecture pipeline
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Add both traditional controllers and modern endpoints
app.MapControllers(); // Existing REST controllers
app.MapEndpoints();   // New Clean Architecture endpoints
app.MapGraphQL();     // Existing GraphQL

app.Run();
```

## Benefits of the New Architecture

### 1. **Dual API Support**

- **Legacy Controllers**: Existing REST API controllers remain functional
- **Modern Endpoints**: New minimal API endpoints with better performance
- **Gradual Migration**: Can migrate endpoints one by one

### 2. **Enhanced ErrorMessage Handling**

```csharp
// Old approach
catch (Exception ex)
{
    return BadRequest(ex.Message);
}

// New approach with Result pattern
try 
{
    var result = await mediator.Send(command);
    return result.IsSuccess 
        ? TypedResults.Ok(result.Value)
        : TypedResults.BadRequest(result.ErrorMessage.Description);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
{
    return TypedResults.PageNotFound();
}
```

### 3. **CQRS with Result Pattern**

```csharp
// Handler returns Result<T> for better error handling
public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    if (await EmailExists(request.Email))
        return Result.Failure<User>(UserErrors.EmailAlreadyExists);
        
    var user = new User(request.Name, request.Email);
    await _repository.AddAsync(user);
    
    return Result.Success(user);
}
```

### 4. **Domain Events Integration**

```csharp
// Entities can raise domain events
public class User : BaseEntity, IHasDomainEvents
{
    public void ChangeEmail(string newEmail)
    {
        if (Email != newEmail)
        {
            Email = newEmail;
            RaiseDomainEvent(new UserEmailChangedEvent(Id, Email, newEmail));
        }
    }
}
```

## Migration Strategy

### Phase 1: Infrastructure Setup

1. ✅ Add new DI container structure
2. ✅ Add enhanced exception handling
3. ✅ Add domain event infrastructure
4. ✅ Add Result pattern abstractions

### Phase 2: Endpoint Migration

1. Create new endpoints alongside existing controllers
2. Migrate high-traffic endpoints first
3. Update frontend to use new endpoints
4. Remove old controllers gradually

### Phase 3: Domain Events

1. Add IHasDomainEvents to entities
2. Implement domain event handlers
3. Replace direct service calls with domain events

## Example Implementation

### New User Endpoint (Clean Architecture)

```csharp
group.MapPost("/", async (CreateUserDto dto, IMediator mediator) =>
{
    var command = new CreateUserCommand(dto.Name, dto.Email);
    var result = await mediator.Send(command);
    
    return result.IsSuccess
        ? Results.Created($"/api/users/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.ErrorMessage);
})
.WithName("CreateUser")
.Produces<UserDto>(201)
.ProducesValidationProblem(400);
```

### Domain Event Handler

```csharp
public class UserCreatedHandler : IDomainEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Send welcome email
        // Create user profile
        // Log audit event
    }
}
```

## Performance Benefits

### 1. **Minimal API Performance**

- 30% faster than controllers
- Lower memory allocation
- Better throughput

### 2. **Response Compression**

- Automatic Brotli/Gzip compression
- Reduces bandwidth usage
- Faster client response times

### 3. **Rate Limiting**

- Built-in rate limiting
- Prevents API abuse
- Configurable per endpoint

## Testing Improvements

### 1. **Better Testability**

```csharp
[Test]
public async Task CreateUser_WithValidData_ShouldReturnSuccess()
{
    // Arrange
    var command = new CreateUserCommand("John Doe", "john@example.com");
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("John Doe", result.Value.Name);
}
```

### 2. **Integration Testing**

```csharp
[Test]
public async Task POST_Users_ShouldCreateUser()
{
    // Arrange
    var request = new CreateUserDto { Name = "John", Email = "john@test.com" };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/users", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

## Recommendations

### Immediate Actions

1. **Keep existing controllers** for backward compatibility
2. **Add new endpoints** for new features
3. **Implement gradual migration** strategy

### Long-term Goals

1. **Complete CQRS implementation** across all modules
2. **Domain events** for cross-cutting concerns
3. **Result pattern** for all operations
4. **Clean Architecture** principles throughout

This approach provides a smooth transition path while immediately benefiting from modern patterns and improved
performance.
