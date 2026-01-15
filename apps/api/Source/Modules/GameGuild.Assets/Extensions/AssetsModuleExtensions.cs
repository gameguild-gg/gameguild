using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.Modules;
using GameGuild.Assets.Configuration;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;
using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Extensions;

/// <summary>
/// Module registration for Assets module.
/// </summary>
public static class AssetsModuleExtensions
{
    /// <summary>
    /// Adds Assets module services to the DI container.
    /// </summary>
    public static IServiceCollection AddAssetsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<AssetTokenOptions>(
            configuration.GetSection(AssetTokenOptions.SectionName));
        services.Configure<AssetStorageOptions>(
            configuration.GetSection(AssetStorageOptions.SectionName));
        services.Configure<AssetUploadOptions>(
            configuration.GetSection(AssetUploadOptions.SectionName));
        services.Configure<AssetAccessOptions>(
            configuration.GetSection(AssetAccessOptions.SectionName));

        // S3 Client
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = configuration.GetSection(AssetStorageOptions.SectionName)
                .Get<AssetStorageOptions>() ?? new AssetStorageOptions();

            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle,
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrEmpty(options.AccessKey) && !string.IsNullOrEmpty(options.SecretKey))
            {
                return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
            }

            return new AmazonS3Client(config);
        });

        // Repositories
        services.AddScoped<IAssetContentRepository, AssetContentRepository>();
        services.AddScoped<IAssetReferenceRepository, AssetReferenceRepository>();
        services.AddScoped<ITransformedAssetRepository, TransformedAssetRepository>();
        services.AddScoped<IAssetReportRepository, AssetReportRepository>();

        // Services
        services.AddScoped<IAssetTokenService, AssetTokenService>();
        services.AddScoped<IAssetStorageService, AssetStorageService>();
        services.AddScoped<IAssetUploadService, AssetUploadService>();
        services.AddScoped<IAssetAccessService, AssetAccessService>();
        services.AddScoped<IAssetModerationService, AssetModerationService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<UploadAssetValidator>(ServiceLifetime.Scoped);

        // Command/Query Handlers
        services.AddScoped<IRequestHandler<UploadAssetCommand, Result<UploadAssetResponse>>, UploadAssetHandler>();
        services.AddScoped<IRequestHandler<GenerateAccessUrlCommand, Result<GenerateAccessUrlResponse>>, GenerateAccessUrlHandler>();
        services.AddScoped<IRequestHandler<UpdateAssetCommand, Result<UpdateAssetResponse>>, UpdateAssetHandler>();
        services.AddScoped<IRequestHandler<DeleteAssetCommand, Result<DeleteAssetResponse>>, DeleteAssetHandler>();
        services.AddScoped<IRequestHandler<ReportAssetCommand, Result<ReportAssetResponse>>, ReportAssetHandler>();
        services.AddScoped<IRequestHandler<ReviewReportCommand, Result<ReviewReportResponse>>, ReviewReportHandler>();
        
        services.AddScoped<IRequestHandler<GetAssetQuery, Result<AssetDto?>>, GetAssetHandler>();
        services.AddScoped<IRequestHandler<GetAssetsByParentQuery, Result<IReadOnlyList<AssetDto>>>, GetAssetsByParentHandler>();
        services.AddScoped<IRequestHandler<GetUserAssetsQuery, Result<IReadOnlyList<AssetDto>>>, GetUserAssetsHandler>();
        services.AddScoped<IRequestHandler<GetModerationQueueQuery, Result<IReadOnlyList<ReportDto>>>, GetModerationQueueHandler>();
        services.AddScoped<IRequestHandler<GetAssetReportsQuery, Result<IReadOnlyList<ReportDto>>>, GetAssetReportsHandler>();

        return services;
    }

    /// <summary>
    /// Configures EF Core model for Assets entities.
    /// </summary>
    public static ModelBuilder ConfigureAssetsEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("assets");

        modelBuilder.ApplyConfiguration(new AssetContentConfiguration());
        modelBuilder.ApplyConfiguration(new AssetReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new TransformedAssetConfiguration());
        modelBuilder.ApplyConfiguration(new AssetReportConfiguration());

        return modelBuilder;
    }
}

/// <summary>
/// Module implementation for Assets.
/// </summary>
public class AssetsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAssetsModule(configuration);
    }

    public void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        // Controllers handle routing via attributes
    }
}
