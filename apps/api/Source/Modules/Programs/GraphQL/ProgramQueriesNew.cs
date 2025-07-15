using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Queries;
using HotChocolate;
using HotChocolate.Authorization;
using MediatR;

namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL Queries for Program data retrieval using CQRS pattern
/// All queries use MediatR queries for proper separation of concerns
/// </summary>
[ExtendObjectType("Query")]
public class ProgramQueriesNew
{
    // ===== BASIC QUERIES =====

    /// <summary>
    /// Get all programs with pagination and filtering
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireContentTypePermission<Program>(PermissionType.Read)]
    public async Task<IEnumerable<Program>> GetAllPrograms(
        [Service] IMediator mediator,
        int skip = 0,
        int take = 50,
        string? search = null,
        ProgramCategory? category = null,
        ProgramDifficulty? difficulty = null,
        ContentStatus? status = null,
        AccessLevel? visibility = null,
        EnrollmentStatus? enrollmentStatus = null,
        string? creatorId = null,
        bool includeArchived = false,
        string? sortBy = "CreatedAt",
        bool sortDescending = true)
    {
        var query = new GetAllProgramsQuery(
            skip, take, search, category, difficulty, status, visibility,
            enrollmentStatus, creatorId, includeArchived, sortBy, sortDescending
        );

        return await mediator.Send(query);
    }

    /// <summary>
    /// Get a program by ID
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<Program?> GetProgramById(
        [Service] IMediator mediator,
        Guid id,
        bool includeContent = false,
        bool includeEnrollments = false,
        bool includeRatings = false)
    {
        var query = new GetProgramByIdQuery(id, includeContent, includeEnrollments, includeRatings);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get a program by slug
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "slug")]
    public async Task<Program?> GetProgramBySlug(
        [Service] IMediator mediator,
        string slug,
        bool includeContent = false,
        bool includeEnrollments = false,
        bool includeRatings = false)
    {
        var query = new GetProgramBySlugQuery(slug, includeContent, includeEnrollments, includeRatings);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get published program by slug (public access)
    /// </summary>
    [GraphQLDescription("Get a published program by slug - public access")]
    public async Task<Program?> GetPublishedProgramBySlug(
        [Service] IMediator mediator,
        string slug,
        bool includeContent = false)
    {
        var query = new GetPublishedProgramBySlugQuery(slug, includeContent);
        return await mediator.Send(query);
    }

    // ===== SEARCH AND FILTER QUERIES =====

    /// <summary>
    /// Search programs with advanced filtering
    /// </summary>
    [GraphQLDescription("Search programs with advanced filtering - public access")]
    public async Task<IEnumerable<Program>> SearchPrograms(
        [Service] IMediator mediator,
        string searchTerm,
        ProgramCategory? category = null,
        ProgramDifficulty? difficulty = null,
        float? minEstimatedHours = null,
        float? maxEstimatedHours = null,
        decimal? minRating = null,
        bool availableForEnrollment = false,
        int skip = 0,
        int take = 50)
    {
        var query = new SearchProgramsQuery(
            searchTerm, category, difficulty, minEstimatedHours, maxEstimatedHours,
            minRating, availableForEnrollment, skip, take
        );

        return await mediator.Send(query);
    }

    /// <summary>
    /// Get programs by category
    /// </summary>
    [GraphQLDescription("Get programs by category - public access")]
    public async Task<IEnumerable<Program>> GetProgramsByCategory(
        [Service] IMediator mediator,
        ProgramCategory category,
        int skip = 0,
        int take = 50,
        bool onlyPublished = true)
    {
        var query = new GetProgramsByCategoryQuery(category, skip, take, onlyPublished);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get programs by difficulty
    /// </summary>
    [GraphQLDescription("Get programs by difficulty - public access")]
    public async Task<IEnumerable<Program>> GetProgramsByDifficulty(
        [Service] IMediator mediator,
        ProgramDifficulty difficulty,
        int skip = 0,
        int take = 50,
        bool onlyPublished = true)
    {
        var query = new GetProgramsByDifficultyQuery(difficulty, skip, take, onlyPublished);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get programs by creator
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireContentTypePermission<Program>(PermissionType.Read)]
    public async Task<IEnumerable<Program>> GetProgramsByCreator(
        [Service] IMediator mediator,
        string creatorId,
        int skip = 0,
        int take = 50,
        bool onlyPublished = false)
    {
        var query = new GetProgramsByCreatorQuery(creatorId, skip, take, onlyPublished);
        return await mediator.Send(query);
    }

    // ===== ENROLLMENT QUERIES =====

    /// <summary>
    /// Get enrolled programs for current user
    /// </summary>
    [Authorize]
    public async Task<IEnumerable<Program>> GetMyEnrolledPrograms(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        int skip = 0,
        int take = 50,
        bool onlyActive = true)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new GetUserEnrolledProgramsQuery(userId, skip, take, onlyActive);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get enrolled programs for a specific user
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireTenantPermission(PermissionType.Read)]
    public async Task<IEnumerable<Program>> GetUserEnrolledPrograms(
        [Service] IMediator mediator,
        string userId,
        int skip = 0,
        int take = 50,
        bool onlyActive = true)
    {
        var query = new GetUserEnrolledProgramsQuery(userId, skip, take, onlyActive);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get program enrollments
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
    public async Task<IEnumerable<ProgramUser>> GetProgramEnrollments(
        [Service] IMediator mediator,
        Guid programId,
        int skip = 0,
        int take = 50,
        bool onlyActive = true)
    {
        var query = new GetProgramEnrollmentsQuery(programId, skip, take, onlyActive);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Check if current user is enrolled in program
    /// </summary>
    [Authorize]
    public async Task<ProgramUser?> CheckMyEnrollment(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid programId)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new CheckUserEnrollmentQuery(programId, userId);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Check if user is enrolled in program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
    public async Task<ProgramUser?> CheckUserEnrollment(
        [Service] IMediator mediator,
        Guid programId,
        string userId)
    {
        var query = new CheckUserEnrollmentQuery(programId, userId);
        return await mediator.Send(query);
    }

    // ===== CONTENT QUERIES =====

    /// <summary>
    /// Get program content
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
    public async Task<IEnumerable<ProgramContent>> GetProgramContent(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid programId,
        bool onlyVisible = true)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        var query = new GetProgramContentQuery(programId, onlyVisible, userId);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get current user's progress in a program
    /// </summary>
    [Authorize]
    public async Task<ProgramUserProgress?> GetMyProgramProgress(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid programId)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new GetUserProgramProgressQuery(programId, userId);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get user's progress in a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
    public async Task<ProgramUserProgress?> GetUserProgramProgress(
        [Service] IMediator mediator,
        Guid programId,
        string userId)
    {
        var query = new GetUserProgramProgressQuery(programId, userId);
        return await mediator.Send(query);
    }

    // ===== STATISTICS QUERIES =====

    /// <summary>
    /// Get program statistics
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
    public async Task<ProgramStatistics> GetProgramStatistics(
        [Service] IMediator mediator,
        Guid programId)
    {
        var query = new GetProgramStatisticsQuery(programId);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get global program statistics
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireTenantPermission(PermissionType.Read)]
    public async Task<GlobalProgramStatistics> GetGlobalProgramStatistics(
        [Service] IMediator mediator,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = new GetGlobalProgramStatisticsQuery(fromDate, toDate);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get creator's program statistics
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireContentTypePermission<Program>(PermissionType.Read)]
    public async Task<CreatorProgramStatistics> GetCreatorProgramStatistics(
        [Service] IMediator mediator,
        string creatorId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = new GetCreatorProgramStatisticsQuery(creatorId, fromDate, toDate);
        return await mediator.Send(query);
    }

    // ===== TRENDING AND RECOMMENDATIONS =====

    /// <summary>
    /// Get popular programs
    /// </summary>
    [GraphQLDescription("Get popular programs - public access")]
    public async Task<IEnumerable<Program>> GetPopularPrograms(
        [Service] IMediator mediator,
        int skip = 0,
        int take = 10,
        int daysBack = 30)
    {
        var query = new GetPopularProgramsQuery(skip, take, daysBack);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get recent programs
    /// </summary>
    [GraphQLDescription("Get recent programs - public access")]
    public async Task<IEnumerable<Program>> GetRecentPrograms(
        [Service] IMediator mediator,
        int skip = 0,
        int take = 10,
        int daysBack = 7)
    {
        var query = new GetRecentProgramsQuery(skip, take, daysBack);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get featured programs
    /// </summary>
    [GraphQLDescription("Get featured programs - public access")]
    public async Task<IEnumerable<Program>> GetFeaturedPrograms(
        [Service] IMediator mediator,
        int skip = 0,
        int take = 10)
    {
        var query = new GetFeaturedProgramsQuery(skip, take);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get recommended programs for current user
    /// </summary>
    [Authorize]
    public async Task<IEnumerable<Program>> GetRecommendedPrograms(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        int take = 10)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new GetRecommendedProgramsQuery(userId, take);
        return await mediator.Send(query);
    }

    // ===== RATING QUERIES =====

    /// <summary>
    /// Get program ratings
    /// </summary>
    [GraphQLDescription("Get program ratings - public access")]
    public async Task<IEnumerable<ProgramRating>> GetProgramRatings(
        [Service] IMediator mediator,
        Guid programId,
        int skip = 0,
        int take = 50)
    {
        var query = new GetProgramRatingsQuery(programId, skip, take);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get current user's rating for a program
    /// </summary>
    [Authorize]
    public async Task<ProgramRating?> GetMyProgramRating(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid programId)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new GetUserProgramRatingQuery(programId, userId);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get user's rating for a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
    public async Task<ProgramRating?> GetUserProgramRating(
        [Service] IMediator mediator,
        Guid programId,
        string userId)
    {
        var query = new GetUserProgramRatingQuery(programId, userId);
        return await mediator.Send(query);
    }

    // ===== WISHLIST QUERIES =====

    /// <summary>
    /// Get current user's wishlist programs
    /// </summary>
    [Authorize]
    public async Task<IEnumerable<Program>> GetMyWishlist(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        int skip = 0,
        int take = 50)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new GetUserWishlistQuery(userId, skip, take);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Get user's wishlist programs
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireTenantPermission(PermissionType.Read)]
    public async Task<IEnumerable<Program>> GetUserWishlist(
        [Service] IMediator mediator,
        string userId,
        int skip = 0,
        int take = 50)
    {
        var query = new GetUserWishlistQuery(userId, skip, take);
        return await mediator.Send(query);
    }

    /// <summary>
    /// Check if program is in current user's wishlist
    /// </summary>
    [Authorize]
    public async Task<bool> CheckProgramInMyWishlist(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid programId)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated");

        var query = new CheckProgramInWishlistQuery(programId, userId);
        return await mediator.Send(query);
    }
}
