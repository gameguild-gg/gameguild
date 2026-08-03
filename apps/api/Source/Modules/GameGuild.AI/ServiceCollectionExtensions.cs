using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.AI;

/// <summary>
///     Dependency injection registration for the AI module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Add AI module services to the service collection.
    /// </summary>
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddHttpClient();

        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        services.AddScoped<IAiPromptTemplateService, AiPromptTemplateService>();
        services.AddScoped<IAiConversationHistoryReader, AiConversationHistoryRepository>();
        services.AddScoped<IAiConversationHistoryRepository, AiConversationHistoryRepository>();
        services.AddScoped<IAiProviderCostFactStore, EfAiProviderCostFactStore>();
        services.AddScoped<IAiProviderAdapter, OpenAiAdapter>();
        services.AddScoped<IAiProviderAdapter, AnthropicAdapter>();
        services.AddScoped<IAiProviderAdapter, GoogleAiAdapter>();

        return services;
    }
}
