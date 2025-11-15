using GameGuild.CQRS;
using GameGuild.Permissions.Abstractions;
using GameGuild.Resources.Behaviors;
using GameGuild.Resources.Data;
using GameGuild.Resources.Extensions;
using GameGuild.Resources.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.Resources.IntegrationTests;

/// <summary>
/// Test fixture for ResourceQuota integration tests with in-memory database
/// </summary>
public class ResourceQuotaTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public ResourceQuotaTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add in-memory database
        services.AddDbContext<ResourceQuotaDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
        });

        // Add CQRS
        services.AddCqrs();

        // Add Resources module services
        services.AddResourceQuotaBehavior();
        services.AddScoped<IResourceQuotaService, ResourceQuotaService>();

        // Register test handlers
        services.AddScoped<ICommandHandler<TestCreateUserCommand, TestCreateUserResponse>, TestCreateUserCommandHandler>();
        services.AddScoped<ICommandHandler<TestUploadFileCommand, TestUploadFileResponse>, TestUploadFileCommandHandler>();
        services.AddScoped<ICommandHandler<TestCheckProjectCommand, TestCheckProjectResponse>, TestCheckProjectCommandHandler>();
        services.AddScoped<ICommandHandler<TestApiCallCommand, TestApiCallResponse>, TestApiCallCommandHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        // Initialize database
        var dbContext = _scope.ServiceProvider.GetRequiredService<ResourceQuotaDbContext>();
        dbContext.Database.EnsureCreated();
    }

    public ICommandSender GetCommandSender(Guid tenantId)
    {
        var scope = _serviceProvider.CreateScope();
        
        // Mock tenant context for this scope
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenantId);

        // Create a custom service provider with the mocked tenant context
        var serviceCollection = new ServiceCollection();
        
        // Copy services from main provider
        foreach (var service in _serviceProvider.GetServices<ServiceDescriptor>())
        {
            serviceCollection.Add(service);
        }

        // Override tenant context
        serviceCollection.AddScoped(_ => tenantContext.Object);

        var customProvider = serviceCollection.BuildServiceProvider();
        return customProvider.GetRequiredService<ICommandSender>();
    }

    public T? GetService<T>()
    {
        return _scope.ServiceProvider.GetService<T>();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}
