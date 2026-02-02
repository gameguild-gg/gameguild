using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning;

/// <summary>
/// Extension methods for registering GameGuild.Learning services
/// </summary>
public static class LearningServiceCollectionExtensions
{
    /// <summary>
    /// Adds GameGuild.Learning core services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLearningCore(this IServiceCollection services)
    {
        // Register core learning services
        // Individual modules will register their own implementations of the provider interfaces
        
        return services;
    }
    
    /// <summary>
    /// Adds a course info provider implementation
    /// </summary>
    public static IServiceCollection AddCourseInfoProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, Abstractions.ICourseInfoProvider
    {
        services.AddScoped<Abstractions.ICourseInfoProvider, TProvider>();
        return services;
    }
    
    /// <summary>
    /// Adds an enrollment info provider implementation
    /// </summary>
    public static IServiceCollection AddEnrollmentInfoProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, Abstractions.IEnrollmentInfoProvider
    {
        services.AddScoped<Abstractions.IEnrollmentInfoProvider, TProvider>();
        return services;
    }
    
    /// <summary>
    /// Adds a progress info provider implementation
    /// </summary>
    public static IServiceCollection AddProgressInfoProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, Abstractions.IProgressInfoProvider
    {
        services.AddScoped<Abstractions.IProgressInfoProvider, TProvider>();
        return services;
    }
    
    /// <summary>
    /// Adds a learner profile provider implementation
    /// </summary>
    public static IServiceCollection AddLearnerProfileProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, Abstractions.ILearnerProfileProvider
    {
        services.AddScoped<Abstractions.ILearnerProfileProvider, TProvider>();
        return services;
    }
    
    /// <summary>
    /// Adds a learning event publisher implementation
    /// </summary>
    public static IServiceCollection AddLearningEventPublisher<TPublisher>(this IServiceCollection services)
        where TPublisher : class, Abstractions.ILearningEventPublisher
    {
        services.AddScoped<Abstractions.ILearningEventPublisher, TPublisher>();
        return services;
    }
    
    /// <summary>
    /// Adds a learning capability service implementation
    /// </summary>
    public static IServiceCollection AddLearningCapabilityService<TService>(this IServiceCollection services)
        where TService : class, Abstractions.ILearningCapabilityService
    {
        services.AddScoped<Abstractions.ILearningCapabilityService, TService>();
        return services;
    }
}
