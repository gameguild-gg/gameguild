using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<OpenAiCompatibleProtocolClient>();
        services.AddScoped<AiAdapterRegistry>();
        services.AddScoped<AiBackendCatalog>();
        services.AddScoped<IAiBackendCatalog>(static provider => provider.GetRequiredService<AiBackendCatalog>());
        services.AddScoped<IAiPromptTemplateService, AiPromptTemplateService>();
        services.AddScoped<IAiConversationHistoryReader, AiConversationHistoryRepository>();
        services.AddScoped<IAiConversationHistoryRepository, AiConversationHistoryRepository>();
        services.AddScoped<IAiAdapter, OpenAiAdapter>();
        services.AddScoped<IAiAdapter, StandardAiAdapter>();
        services.AddScoped<IAiAdapter, AnthropicAdapter>();
        services.AddScoped<IAiAdapter, GoogleAiAdapter>();
        services.TryAddScoped<IAiExecutionBillingRecorder, NoOpAiExecutionBillingRecorder>();

        return services;
    }
}
