using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Programs.Models;
using MediatR;
using ProgramAvailabilityStatus = GameGuild.Common.EnrollmentStatus;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Commands for Program management using CQRS pattern
/// All commands implement IRequest pattern for MediatR handling
/// </summary>

// ===== CRUD COMMANDS =====

/// <summary>
/// Command to create a new program
/// </summary>
public record CreateProgramCommand(
    string Title,
    string Description,
    string? Summary = null,
    string? Thumbnail = null,
    string? VideoShowcaseUrl = null,
    float? EstimatedHours = null,
    ProgramCategory Category = ProgramCategory.Other,
    ProgramDifficulty Difficulty = ProgramDifficulty.Beginner,
    ProgramAvailabilityStatus EnrollmentStatus = ProgramAvailabilityStatus.Open,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null,
    string? CreatorId = null
) : IRequest<Program>;

/// <summary>
/// Command to update an existing program
/// </summary>
public record UpdateProgramCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    string? Summary = null,
    string? Thumbnail = null,
    string? VideoShowcaseUrl = null,
    float? EstimatedHours = null,
    ProgramCategory? Category = null,
    ProgramDifficulty? Difficulty = null,
    ProgramAvailabilityStatus? EnrollmentStatus = null,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
) : IRequest<Program>;

/// <summary>
/// Command to delete a program (soft delete)
/// </summary>
public record DeleteProgramCommand(Guid Id) : IRequest<bool>;

// ===== STATUS COMMANDS =====

/// <summary>
/// Command to publish a program
/// </summary>
public record PublishProgramCommand(Guid Id) : IRequest<Program>;

/// <summary>
/// Command to unpublish a program
/// </summary>
public record UnpublishProgramCommand(Guid Id) : IRequest<Program>;

/// <summary>
/// Command to archive a program
/// </summary>
public record ArchiveProgramCommand(Guid Id) : IRequest<Program>;

/// <summary>
/// Command to restore an archived program
/// </summary>
public record RestoreProgramCommand(Guid Id) : IRequest<Program>;

// ===== ENROLLMENT COMMANDS =====

/// <summary>
/// Command to enroll a user in a program
/// </summary>
public record EnrollUserCommand(
    Guid ProgramId,
    string UserId,
    DateTime? EnrollmentDate = null
) : IRequest<ProgramUser>;

/// <summary>
/// Command to unenroll a user from a program
/// </summary>
public record UnenrollUserCommand(
    Guid ProgramId,
    string UserId
) : IRequest<bool>;

/// <summary>
/// Command to update enrollment status
/// </summary>
public record UpdateEnrollmentStatusCommand(
    Guid ProgramId,
    ProgramAvailabilityStatus Status,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
) : IRequest<Program>;

// ===== CONTENT MANAGEMENT COMMANDS =====

/// <summary>
/// Command to add content to a program
/// </summary>
public record AddProgramContentCommand(
    Guid ProgramId,
    Guid ContentId,
    int Order,
    bool IsRequired = true,
    int? PointsReward = null
) : IRequest<ProgramContent>;

/// <summary>
/// Command to remove content from a program
/// </summary>
public record RemoveProgramContentCommand(
    Guid ProgramId,
    Guid ContentId
) : IRequest<bool>;

/// <summary>
/// Command to reorder program content
/// </summary>
public record ReorderProgramContentCommand(
    Guid ProgramId,
    Dictionary<Guid, int> ContentOrders
) : IRequest<IEnumerable<ProgramContent>>;

// ===== RATING COMMANDS =====

/// <summary>
/// Command to rate a program
/// </summary>
public record RateProgramCommand(
    Guid ProgramId,
    string UserId,
    decimal Rating,
    string? Review = null
) : IRequest<ProgramRating>;

/// <summary>
/// Command to update a program rating
/// </summary>
public record UpdateProgramRatingCommand(
    Guid ProgramId,
    string UserId,
    decimal Rating,
    string? Review = null
) : IRequest<ProgramRating>;

/// <summary>
/// Command to delete a program rating
/// </summary>
public record DeleteProgramRatingCommand(
    Guid ProgramId,
    string UserId
) : IRequest<bool>;

// ===== WISHLIST COMMANDS =====

/// <summary>
/// Command to add program to wishlist
/// </summary>
public record AddToWishlistCommand(
    Guid ProgramId,
    string UserId
) : IRequest<ProgramWishlist>;

/// <summary>
/// Command to remove program from wishlist
/// </summary>
public record RemoveFromWishlistCommand(
    Guid ProgramId,
    string UserId
) : IRequest<bool>;

// ===== BULK OPERATIONS =====

/// <summary>
/// Command to bulk update program visibility
/// </summary>
public record BulkUpdateProgramVisibilityCommand(
    IEnumerable<Guid> ProgramIds,
    AccessLevel Visibility
) : IRequest<IEnumerable<Program>>;

/// <summary>
/// Command to bulk archive programs
/// </summary>
public record BulkArchiveProgramsCommand(
    IEnumerable<Guid> ProgramIds
) : IRequest<IEnumerable<Program>>;
