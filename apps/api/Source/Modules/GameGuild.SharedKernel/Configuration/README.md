# GameGuild SharedKernel Configuration

This directory contains shared configuration utilities and option classes that can be used across all GameGuild modules.

## Core Utilities

### IOptionBuilder<T>

Interface that defines the contract for option builders. While not directly implemented by static classes, it documents the expected method signatures.

### OptionBuilderUtilities

Static utility class with common configuration binding methods:

- `CreateAndBind<T>()` - Binds configuration to options with defaults
- `CreateBindAndValidate<T>()` - Binds with optional validation

### SharedConfigurationExtensions

Extension methods for `IServiceCollection`:

- `ConfigureOptions<T>()` - Configures options with automatic section detection
- `ConfigureOptionsFromSection<T>()` - Configures from specific section

### BaseOptions & ModuleOptions

Base classes for configuration options:

- `BaseOptions` - Provides common validation infrastructure
- `ModuleOptions` - Extends BaseOptions with module-specific features

## Presentation Layer Configurations

### Available Option Types

- `CorsOptions` + `CorsOptionsBuilder` - Cross-Origin Resource Sharing
- `HealthCheckOptions` - Health check configuration
- `PresentationLayerOptions` + `PresentationLayerOptionsBuilder` - Main presentation settings
- `HttpLoggingOptions` + `HttpLoggingOptionsBuilder` - HTTP request/response logging
- `AuthenticationOptions` - Authentication and JWT configuration
- `ApiVersioningOptions` - API versioning configuration
- `MemoryCachingOptions` - Memory caching configuration

## Infrastructure Configurations

### Available Option Types

- `DatabaseOptions` - Database connection and EF Core settings
- `CachingOptions` - Memory and distributed caching configuration
- `ExternalApiOptions` - External API integration and HTTP client settings
- `FileStorageOptions` + `FileStorageProvider` enum - File storage configuration (Local, Azure Blob, S3, Google Cloud)
- `MessageQueueOptions` - Message queue services configuration (RabbitMQ, Azure Service Bus)
- `MonitoringOptions` - Application monitoring, logging, and health checks
- `InfrastructureLayerOptions` - Main infrastructure layer configuration that combines all above options

## Application Layer Configurations

### Available Option Types

- `ApplicationLayerOptions` - Main application layer configuration with MediatR, AutoMapper, FluentValidation
- `BackgroundServiceOptions` - Background task and service configuration

## Usage Examples

### Basic Module Configuration

```csharp
using GameGuild.Configuration;

public class MyModuleOptions : ModuleOptions
{
    public string ConnectionString { get; set; } = "";
    public int MaxRetries { get; set; } = 3;
    
    public override void Validate()
    {
        base.Validate();
        if (string.IsNullOrEmpty(ConnectionString))
            throw new InvalidOperationException("ConnectionString is required");
    }
}

// In DependencyInjection:
services.ConfigureOptions<MyModuleOptions>(
    configuration,
    () => new MyModuleOptions(),
    options => options.Validate()
);
```

### Using Specific Builders

```csharp
// CORS configuration
var corsOptions = CorsOptionsBuilder.Create(configuration);
services.AddSingleton(corsOptions);

// Presentation layer
var presentationOptions = PresentationLayerOptionsBuilder.CreateWithValidation(configuration);
```

## Benefits

1. **Consistency** - All modules use the same configuration patterns
2. **Reusability** - Common configuration types shared across modules
3. **Validation** - Built-in validation infrastructure
4. **Type Safety** - Strongly-typed configuration with compile-time checking
5. **Testability** - Easy to mock and test configuration