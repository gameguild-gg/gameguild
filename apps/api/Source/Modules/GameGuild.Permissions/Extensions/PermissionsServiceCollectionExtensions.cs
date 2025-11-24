using GameGuild.CQRS;
using GameGuild.Permissions.Application.Services;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Infrastructure.Behaviors;
using GameGuild.Permissions.Identity;
using GameGuild.Permissions.Infrastructure.Identity;
using GameGuild.Permissions.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Permissions.Infrastructure.Extensions;

/// <summary>
///     Extension methods for IServiceCollection to register permission-related services
/// </summary>
public static class PermissionsServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all identity context services (User, Tenant, Permissions, Localization)
    /// </summary>
    public static IServiceCollection AddIdentityContexts(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IPermissionsContext, PermissionsContext>();
        services.AddScoped<ILocalizationContext, LocalizationContext>();

        return services;
    }

    /// <summary>
    ///     Registers the authorization pipeline behavior for CQRS
    /// </summary>
    public static IServiceCollection AddAuthorizationBehavior(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        return services;
    }

    /// <summary>
    ///     Registers all permission-related infrastructure services
    ///     This is a convenience method that calls AddIdentityContexts and AddAuthorizationBehavior
    /// </summary>
    public static IServiceCollection AddPermissionsInfrastructure(this IServiceCollection services)
    {
        services.AddIdentityContexts();
        services.AddAuthorizationBehavior();

        // Register repositories
        services.AddScoped<ITenantPermissionRepository, TenantPermissionRepository>();
        services.AddScoped<IPermissionAuditLogRepository, PermissionAuditLogRepository>();

        // Register services  
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionAuditService, PermissionAuditService>();

        return services;
    }
}
