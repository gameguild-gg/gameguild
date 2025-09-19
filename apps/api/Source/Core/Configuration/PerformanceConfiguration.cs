using GameGuild.Core.Performance;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Posts.Models;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using GameGuild.Source.Modules.Programs.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Extension methods for configuring performance optimizations
/// </summary>
public static class PerformanceConfiguration {

    /// <summary>
    /// Adds performance optimization services including compiled queries
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPerformanceOptimizations(this IServiceCollection services) {
        // TEMPORARILY DISABLED: Compiled queries service disabled due to type conflicts
        // services.AddSingleton<ICompiledQueriesService, CompiledQueriesService>();

        return services;
    }
}

/// <summary>
/// Interface for compiled queries service - TEMPORARILY DISABLED DUE TO TYPE CONFLICTS
/// </summary>
/*
public interface ICompiledQueriesService {
    /// <summary>
    /// Gets user by ID using compiled query
    /// </summary>
    Task<User?> GetUserByIdAsync(ApplicationDbContext context, Guid userId);

    /// <summary>
    /// Gets user by email using compiled query
    /// </summary>
    Task<User?> GetUserByEmailAsync(ApplicationDbContext context, string email);

    /// <summary>
    /// Gets tenant by ID using compiled query
    /// </summary>
    Task<Tenant?> GetTenantByIdAsync(ApplicationDbContext context, Guid tenantId);

    /// <summary>
    /// Gets published program by slug using compiled query  
    /// </summary>
    Task<GameGuild.Modules.Programs.Program?> GetPublishedProgramBySlugAsync(ApplicationDbContext context, string slug);    /// <summary>
                                                                                                                            /// Gets published project by slug using compiled query
                                                                                                                            /// </summary>
    Task<Project?> GetPublishedProjectBySlugAsync(ApplicationDbContext context, string slug);

    /// <summary>
    /// Checks user permission using compiled query
    /// </summary>
    Task<bool> HasPermissionAsync(ApplicationDbContext context, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId);

    /// <summary>
    /// Gets published posts with pagination using compiled query
    /// </summary>
    Task<List<Post>> GetPublishedPostsAsync(ApplicationDbContext context, int skip, int take);

    /// <summary>
    /// Gets program content using compiled query
    /// </summary>
    Task<List<ProgramContent>> GetProgramContentAsync(ApplicationDbContext context, Guid programId);
}
*/

/// <summary>
/// Implementation of compiled queries service - TEMPORARILY DISABLED DUE TO TYPE CONFLICTS
/// </summary>
/*
public class CompiledQueriesService : ICompiledQueriesService {

    public Task<User?> GetUserByIdAsync(ApplicationDbContext context, Guid userId) {
        return CompiledQueries.GetUserByIdAsync(context, userId);
    }

    public Task<User?> GetUserByEmailAsync(ApplicationDbContext context, string email) {
        return CompiledQueries.GetUserByEmailAsync(context, email);
    }

    public Task<Tenant?> GetTenantByIdAsync(ApplicationDbContext context, Guid tenantId) {
        return CompiledQueries.GetTenantByIdAsync(context, tenantId);
    }

    public Task<GameGuild.Modules.Programs.Program?> GetPublishedProgramBySlugAsync(ApplicationDbContext context, string slug) {
        return CompiledQueries.GetPublishedProgramBySlugAsync(context, slug);
    }

    public Task<Project?> GetPublishedProjectBySlugAsync(ApplicationDbContext context, string slug) {
        return CompiledQueries.GetPublishedProjectBySlugAsync(context, slug);
    }

    public Task<bool> HasPermissionAsync(ApplicationDbContext context, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId) {
        return CompiledQueries.HasPermissionAsync(context, userId, tenantId, permission, resourceId);
    }

    public Task<List<Post>> GetPublishedPostsAsync(ApplicationDbContext context, int skip, int take) {
        return CompiledQueries.GetPublishedPostsAsync(context, skip, take);
    }

    public Task<List<ProgramContent>> GetProgramContentAsync(ApplicationDbContext context, Guid programId) {
        return CompiledQueries.GetProgramContentAsync(context, programId);
    }
}
*/
