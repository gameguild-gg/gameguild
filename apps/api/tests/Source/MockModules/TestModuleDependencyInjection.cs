using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using MediatR;
using System.Reflection;


namespace GameGuild.Tests.MockModules;

/// <summary>
/// Dependency injection setup for the test module
/// </summary>
public static class TestModuleDependencyInjection
{
    /// <summary>
    /// Add test module services to DI container
    /// </summary>
    public static IServiceCollection AddTestModule(this IServiceCollection services)
    {
        // Register MediatR for the test module assembly
        services.AddMediatR(Assembly.GetExecutingAssembly());
        
        // Explicitly register test handlers to ensure they're available
        services.AddScoped<IRequestHandler<GetTestItemsQuery, IEnumerable<TestItem>>, GetTestItemsHandler>();
        services.AddScoped<IRequestHandler<GetTestItemByIdQuery, TestItem?>, GetTestItemByIdHandler>();
        
        return services;
    }
    
    /// <summary>
    /// Add test module GraphQL types to GraphQL schema builder
    /// </summary>
    public static IRequestExecutorBuilder AddTestModuleGraphQL(this IRequestExecutorBuilder builder)
    {
        return builder
            .AddType<TestItemType>()
            .AddTypeExtension<TestModuleQueries>();
    }
}
