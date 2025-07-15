using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Commands;
using GameGuild.Modules.Programs.Models;
using HotChocolate;
using HotChocolate.Authorization;
using MediatR;

namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL Mutations for Program management using CQRS pattern
/// All mutations use MediatR commands for proper separation of concerns
/// </summary>
[ExtendObjectType("Mutation")]
public class ProgramMutationsNew
{
    // ===== CRUD MUTATIONS =====

    /// <summary>
    /// Create a new program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireTenantPermission(PermissionType.Create)]
    public async Task<Program> CreateProgram(
        CreateProgramInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        
        var command = new CreateProgramCommand(
            input.Title,
            input.Description,
            input.Summary,
            input.Thumbnail,
            input.VideoShowcaseUrl,
            input.EstimatedHours,
            input.Category,
            input.Difficulty,
            input.EnrollmentStatus,
            input.MaxEnrollments,
            input.EnrollmentDeadline,
            userId
        );

        return await mediator.Send(command);
    }

    /// <summary>
    /// Update an existing program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "input.Id")]
    public async Task<Program> UpdateProgram(
        UpdateProgramInput input,
        [Service] IMediator mediator)
    {
        var command = new UpdateProgramCommand(
            input.Id,
            input.Title,
            input.Description,
            input.Summary,
            input.Thumbnail,
            input.VideoShowcaseUrl,
            input.EstimatedHours,
            input.Category,
            input.Difficulty,
            input.EnrollmentStatus,
            input.MaxEnrollments,
            input.EnrollmentDeadline
        );

        return await mediator.Send(command);
    }

    /// <summary>
    /// Delete a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Delete, "id")]
    public async Task<bool> DeleteProgram(
        Guid id,
        [Service] IMediator mediator)
    {
        var command = new DeleteProgramCommand(id);
        return await mediator.Send(command);
    }

    // ===== STATUS MUTATIONS =====

    /// <summary>
    /// Publish a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<Program> PublishProgram(
        Guid id,
        [Service] IMediator mediator)
    {
        var command = new PublishProgramCommand(id);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Unpublish a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<Program> UnpublishProgram(
        Guid id,
        [Service] IMediator mediator)
    {
        var command = new UnpublishProgramCommand(id);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Archive a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<Program> ArchiveProgram(
        Guid id,
        [Service] IMediator mediator)
    {
        var command = new ArchiveProgramCommand(id);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Restore an archived program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<Program> RestoreProgram(
        Guid id,
        [Service] IMediator mediator)
    {
        var command = new RestoreProgramCommand(id);
        return await mediator.Send(command);
    }

    // ===== ENROLLMENT MUTATIONS =====

    /// <summary>
    /// Enroll user in a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "input.ProgramId")]
    public async Task<ProgramUser> EnrollUser(
        EnrollUserInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = input.UserId ?? httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User ID is required");

        var command = new EnrollUserCommand(
            input.ProgramId,
            userId,
            input.EnrollmentDate
        );

        return await mediator.Send(command);
    }

    /// <summary>
    /// Unenroll user from a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "input.ProgramId")]
    public async Task<bool> UnenrollUser(
        UnenrollUserInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = input.UserId ?? httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User ID is required");

        var command = new UnenrollUserCommand(input.ProgramId, userId);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Update enrollment status for a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "input.ProgramId")]
    public async Task<Program> UpdateEnrollmentStatus(
        UpdateEnrollmentStatusInput input,
        [Service] IMediator mediator)
    {
        var command = new UpdateEnrollmentStatusCommand(
            input.ProgramId,
            input.Status,
            input.MaxEnrollments,
            input.EnrollmentDeadline
        );

        return await mediator.Send(command);
    }

    // ===== CONTENT MANAGEMENT MUTATIONS =====

    /// <summary>
    /// Add content to a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "input.ProgramId")]
    public async Task<ProgramContent> AddProgramContent(
        AddProgramContentInput input,
        [Service] IMediator mediator)
    {
        var command = new AddProgramContentCommand(
            input.ProgramId,
            input.ContentId,
            input.Order,
            input.IsRequired,
            input.PointsReward
        );

        return await mediator.Send(command);
    }

    /// <summary>
    /// Remove content from a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "input.ProgramId")]
    public async Task<bool> RemoveProgramContent(
        RemoveProgramContentInput input,
        [Service] IMediator mediator)
    {
        var command = new RemoveProgramContentCommand(input.ProgramId, input.ContentId);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Reorder program content
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "input.ProgramId")]
    public async Task<IEnumerable<ProgramContent>> ReorderProgramContent(
        ReorderProgramContentInput input,
        [Service] IMediator mediator)
    {
        var command = new ReorderProgramContentCommand(input.ProgramId, input.ContentOrders);
        return await mediator.Send(command);
    }

    // ===== RATING MUTATIONS =====

    /// <summary>
    /// Rate a program
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "input.ProgramId")]
    public async Task<ProgramRating> RateProgram(
        RateProgramInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated to rate programs");

        var command = new RateProgramCommand(
            input.ProgramId,
            userId,
            input.Rating,
            input.Review
        );

        return await mediator.Send(command);
    }

    /// <summary>
    /// Update a program rating
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "input.ProgramId")]
    public async Task<ProgramRating> UpdateProgramRating(
        UpdateProgramRatingInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated to update ratings");

        var command = new UpdateProgramRatingCommand(
            input.ProgramId,
            userId,
            input.Rating,
            input.Review
        );

        return await mediator.Send(command);
    }

    /// <summary>
    /// Delete a program rating
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "input.ProgramId")]
    public async Task<bool> DeleteProgramRating(
        Guid programId,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated to delete ratings");

        var command = new DeleteProgramRatingCommand(programId, userId);
        return await mediator.Send(command);
    }

    // ===== WISHLIST MUTATIONS =====

    /// <summary>
    /// Add program to wishlist
    /// </summary>
    [Authorize]
    public async Task<ProgramWishlist> AddToWishlist(
        Guid programId,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated to manage wishlist");

        var command = new AddToWishlistCommand(programId, userId);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Remove program from wishlist
    /// </summary>
    [Authorize]
    public async Task<bool> RemoveFromWishlist(
        Guid programId,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("User must be authenticated to manage wishlist");

        var command = new RemoveFromWishlistCommand(programId, userId);
        return await mediator.Send(command);
    }

    // ===== BULK OPERATION MUTATIONS =====

    /// <summary>
    /// Bulk update program visibility
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireTenantPermission(PermissionType.Edit)]
    public async Task<IEnumerable<Program>> BulkUpdateProgramVisibility(
        BulkUpdateProgramVisibilityInput input,
        [Service] IMediator mediator)
    {
        var command = new BulkUpdateProgramVisibilityCommand(input.ProgramIds, input.Visibility);
        return await mediator.Send(command);
    }

    /// <summary>
    /// Bulk archive programs
    /// </summary>
    [Authorize]
    [GameGuild.Common.Authorization.RequireTenantPermission(PermissionType.Edit)]
    public async Task<IEnumerable<Program>> BulkArchivePrograms(
        BulkArchiveProgramsInput input,
        [Service] IMediator mediator)
    {
        var command = new BulkArchiveProgramsCommand(input.ProgramIds);
        return await mediator.Send(command);
    }
}

// ===== INPUT TYPES =====

public record CreateProgramInput(
    string Title,
    string Description,
    string? Summary = null,
    string? Thumbnail = null,
    string? VideoShowcaseUrl = null,
    float? EstimatedHours = null,
    ProgramCategory Category = ProgramCategory.Other,
    ProgramDifficulty Difficulty = ProgramDifficulty.Beginner,
    EnrollmentStatus EnrollmentStatus = EnrollmentStatus.Open,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
);

public record UpdateProgramInput(
    Guid Id,
    string? Title = null,
    string? Description = null,
    string? Summary = null,
    string? Thumbnail = null,
    string? VideoShowcaseUrl = null,
    float? EstimatedHours = null,
    ProgramCategory? Category = null,
    ProgramDifficulty? Difficulty = null,
    EnrollmentStatus? EnrollmentStatus = null,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
);

public record EnrollUserInput(
    Guid ProgramId,
    string? UserId = null,
    DateTime? EnrollmentDate = null
);

public record UnenrollUserInput(
    Guid ProgramId,
    string? UserId = null
);

public record UpdateEnrollmentStatusInput(
    Guid ProgramId,
    EnrollmentStatus Status,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
);

public record AddProgramContentInput(
    Guid ProgramId,
    Guid ContentId,
    int Order,
    bool IsRequired = true,
    int? PointsReward = null
);

public record RemoveProgramContentInput(
    Guid ProgramId,
    Guid ContentId
);

public record ReorderProgramContentInput(
    Guid ProgramId,
    Dictionary<Guid, int> ContentOrders
);

public record RateProgramInput(
    Guid ProgramId,
    decimal Rating,
    string? Review = null
);

public record UpdateProgramRatingInput(
    Guid ProgramId,
    decimal Rating,
    string? Review = null
);

public record BulkUpdateProgramVisibilityInput(
    IEnumerable<Guid> ProgramIds,
    AccessLevel Visibility
);

public record BulkArchiveProgramsInput(
    IEnumerable<Guid> ProgramIds
);
