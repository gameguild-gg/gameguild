using FluentValidation;
using GameGuild.Core.Infrastructure.Permissions;
using GameGuild.Core.Modules;
using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Configuration;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Handlers;
using GameGuild.Modules.Permissions.Services;
using GameGuild.Modules.Permissions.Validators;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Modules.Permissions;

/// <summary>
/// Permissions module implementing the standardized IModule interface.
/// Provides comprehensive permission management services following Clean Architecture and DAC patterns.
/// </summary>
[StandardizedModule("Comprehensive permission management services following Clean Architecture and DAC patterns")]
[ModuleVersion("2.0.0")]
public class PermissionsModule : ModuleBase
{
    public override string ModuleName => "Permissions";

    public override string ModuleVersion => "2.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        // Register core Permission services with proper separation
        services.AddScoped<PermissionService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // Register cached permission service separately to avoid circular dependencies
        services.AddScoped<ICachedPermissionService, CachedPermissionService>(provider =>
        {
            var innerService = provider.GetRequiredService<PermissionService>();
            var cache = provider.GetRequiredService<IMemoryCache>();
            var logger = provider.GetRequiredService<ILogger<CachedPermissionService>>();
            return new CachedPermissionService(innerService, cache, logger);
        });

        // Register audit service
        services.AddScoped<IPermissionAuditService, PermissionAuditService>();

        // Register template service
        services.AddScoped<IPermissionTemplateService, PermissionTemplateService>();

        // Register delegation service
        services.AddScoped<IPermissionDelegationService, PermissionDelegationService>();

        // Register analytics service
        services.AddScoped<IPermissionAnalyticsService, PermissionAnalyticsService>();

        // Register CQRS handlers
        services.AddScoped<IRequestHandler<GrantTenantPermissionCommand, TenantPermission>, GrantTenantPermissionHandler>();

        // Register validators
        services.AddScoped<IValidator<GrantTenantPermissionCommand>, GrantTenantPermissionCommandValidator>();

        // Configure cache options
        services.Configure<PermissionCacheOptions>(configuration.GetSection(PermissionCacheOptions.SectionName));

        // Register memory cache for permission caching
        services.AddMemoryCache();

        // Register HTTP context accessor for audit logging
        services.AddHttpContextAccessor();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app)
    {
        base.MapEndpoints(app);

        // Permissions module endpoints can be configured here

        return app;
    }
}
