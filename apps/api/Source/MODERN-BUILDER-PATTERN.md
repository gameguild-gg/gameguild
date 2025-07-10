# GameGuild API - Modern Builder Pattern

This document explains the modern, fluent builder pattern implementation for the GameGuild API, following .NET best practices and clean architecture principles.

## Overview

The GameGuild API has been refactored to use a modern builder pattern with extension methods, providing:

- ✅ **Clean separation of concerns**
- ✅ **Fluent, readable configuration**
- ✅ **Environment-specific setups**
- ✅ **Testable and maintainable code**
- ✅ **Type-safe configuration**

## Basic Usage

### Standard Production Setup

```csharp
// Program.cs - Minimal and focused
await WebApplication
    .CreateBuilder(args)
    .ConfigureGameGuildApi()
    .BuildAndConfigureAsync()
    .ContinueWith(task => task.Result.RunGameGuildApiAsync())
    .Unwrap();
```

## Builder Factory Methods

### Development Environment

```csharp
await GameGuildApiBuilderFactory
    .CreateForDevelopment(args)
    .BuildAndConfigureAsync()
    .ContinueWith(task => task.Result.RunGameGuildApiAsync())
    .Unwrap();
```

### Production Environment

```csharp
await GameGuildApiBuilderFactory
    .CreateForProduction(args)
    .BuildAndConfigureAsync()
    .ContinueWith(task => task.Result.RunGameGuildApiAsync())
    .Unwrap();
```

### Testing Environment

```csharp
var app = await GameGuildApiBuilderFactory
    .CreateForTesting(args)
    .BuildAndConfigureAsync();
    
// Use for integration tests
```

## Advanced Configuration

### Custom Builder Configuration

```csharp
await GameGuildApiBuilderFactory
    .CreateCustom(args, builder =>
    {
        builder.WebHost.UseUrls("http://localhost:5000");
        // Additional custom configuration
    })
    .ConfigureGameGuildApi(options =>
    {
        options.EnableSwagger = true;
        options.AllowedOrigins = new[] { "https://app.gameguild.com" };
    })
    .BuildAndConfigureAsync()
    .ContinueWith(task => task.Result.RunGameGuildApiAsync())
    .Unwrap();
```

### Custom Middleware

```csharp
var app = await builder
    .BuildAndConfigureAsync();

app.UseCustomMiddleware(app =>
{
    app.Use(async (context, next) =>
    {
        // Custom middleware logic
        await next();
    });
});

await app.RunGameGuildApiAsync();
```

### Health Checks

```csharp
app.MapHealthChecks("/health")  // JSON health status
   .MapHealthChecks("/ready");  // Readiness probe
```

## Architecture Layers

The builder pattern organizes services by architectural layers:

### 1. Presentation Layer (`AddPresentation`)
- Controllers and API endpoints
- CORS configuration
- Swagger/OpenAPI documentation
- Rate limiting
- Response compression

### 2. Application Layer (`AddApplication`)
- MediatR for CQRS
- Validation behaviors
- Domain event handling
- Pipeline behaviors

### 3. Infrastructure Layer (`AddInfrastructure`)
- Database configuration
- Domain modules
- Authentication services
- Health checks

## Configuration Sources

Configuration follows the .NET precedence order:

1. **Environment Variables** (highest precedence)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **.env file** (loaded first for local development)

## Environment-Specific Features

### Development
- Enhanced Swagger UI
- Detailed error pages
- Sensitive data logging
- CORS allow all origins

### Production
- Response compression
- Rate limiting
- Security headers
- Optimized logging

### Testing
- In-memory database
- Simplified authentication
- Fast startup

## Extension Points

### Custom Authentication

```csharp
builder.ConfigureAuthentication(excludeAuth: false)
```

### Custom Presentation Options

```csharp
builder.ConfigureGameGuildApi(options =>
{
    options.EnableSwagger = true;
    options.EnableRateLimiting = false;
    options.ApiTitle = "Custom API Title";
});
```

## Benefits

1. **Maintainability**: Clear separation of configuration concerns
2. **Testability**: Easy to create test instances with different configurations
3. **Flexibility**: Multiple configuration entry points for different scenarios
4. **Type Safety**: Strongly-typed configuration options
5. **Performance**: Conditional service registration based on environment
6. **Security**: Environment variables take precedence for sensitive settings

## Migration from Legacy Approach

The old imperative approach:

```csharp
// Old way - imperative, mixed concerns
var builder = WebApplication.CreateBuilder(args);
Env.Load();
builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.AddCors(/* complex setup */);
builder.Services.AddDbContext(/* complex setup */);
// ... many more manual configurations
var app = builder.Build();
// ... complex pipeline setup
```

New fluent approach:

```csharp
// New way - declarative, separated concerns
await WebApplication
    .CreateBuilder(args)
    .ConfigureGameGuildApi()
    .BuildAndConfigureAsync()
    .ContinueWith(task => task.Result.RunGameGuildApiAsync())
    .Unwrap();
```

## Best Practices

1. **Use factory methods** for environment-specific setups
2. **Leverage extension methods** for custom configuration
3. **Keep Program.cs minimal** - delegate to extensions
4. **Use strongly-typed options** for configuration
5. **Follow the builder pattern** for fluent configuration
6. **Separate concerns** by architectural layer

This modern approach provides a robust, maintainable foundation that scales with your application's complexity while maintaining clean, readable code.
