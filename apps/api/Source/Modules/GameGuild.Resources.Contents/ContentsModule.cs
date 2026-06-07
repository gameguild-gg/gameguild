using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.CQRS;

namespace GameGuild.Resources.Contents;

/// <summary>
///     Contents module - provides content versioning, review workflow, and publishing
/// </summary>
public class ContentsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Contents";

    /// <inheritdoc />
    public IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register focused sub-services
        services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
        services.AddScoped<IContractGenerationService, ContractGenerationService>();
        services.AddScoped<ICommandHandler<GenerateContractCommand, Result<GeneratedContractResult>>, GenerateContractCommandHandler>();
        services.AddScoped<ICommandHandler<BulkGenerateContractsCommand, BulkGeneratedContractsResult>, BulkGenerateContractsCommandHandler>();
        services.AddScoped<IContentDraftService, ContentDraftService>();
        services.AddScoped<IContentReviewPublishingService, ContentReviewPublishingService>();
        services.AddScoped<IContentVersionQueryService, ContentVersionQueryService>();

        // Facade for backward compatibility
        services.AddScoped<IContentVersioningService, ContentVersioningService>();

        return services;
    }

    /// <inheritdoc />
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map endpoints here
        return endpoints;
    }
}
