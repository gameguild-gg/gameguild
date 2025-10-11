using GameGuild.CQRS;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Programs;
using ProgramEntity = GameGuild.Modules.Programs.Program;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Commands for Program management using CQRS pattern
/// All commands implement ICommand pattern for GameGuild.CQRS handling
/// </summary>

// ===== CRUD COMMANDS =====

/// <summary> Command to create a new program </summary>
public record CreateProgramCommand(
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
  DateTime? EnrollmentDeadline = null,
  string? CreatorId = null
) : ICommand<ProgramEntity>;

/// <summary> Command to update an existing program </summary>
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
  EnrollmentStatus? EnrollmentStatus = null,
  int? MaxEnrollments = null,
  DateTime? EnrollmentDeadline = null
) : ICommand<ProgramEntity>;

/// <summary> Command to delete a program (soft delete) </summary>
public record DeleteProgramCommand(Guid Id) : ICommand<bool>;

// ===== STATUS COMMANDS =====

/// <summary> Command to publish a program </summary>
public record PublishProgramCommand(Guid Id) : ICommand<ProgramEntity>;

/// <summary> Command to unpublish a program </summary>
public record UnpublishProgramCommand(Guid Id) : ICommand<ProgramEntity>;

/// <summary> Command to archive a program </summary>
public record ArchiveProgramCommand(Guid Id) : ICommand<ProgramEntity>;

/// <summary> Command to restore a program from archive </summary>
public record RestoreProgramCommand(Guid Id) : ICommand<ProgramEntity>;

// ===== ENROLLMENT COMMANDS =====

/// <summary> Command to enroll a user in a program </summary>
public record EnrollUserCommand(Guid ProgramId, string UserId, DateTime? EnrollmentDate = null) : ICommand<ProgramUser>;

/// <summary> Command to unenroll a user from a program </summary>
public record UnenrollUserCommand(Guid ProgramId, string UserId) : ICommand<bool>;

/// <summary> Command to update enrollment status </summary>
public record UpdateEnrollmentStatusCommand(Guid ProgramId, EnrollmentStatus Status, int? MaxEnrollments = null, DateTime? EnrollmentDeadline = null) : ICommand<ProgramEntity>;

// ===== CONTENT MANAGEMENT COMMANDS =====

/// <summary> Command to add content to a program </summary>
public record AddProgramContentCommand(Guid ProgramId, Guid ContentId, int Order, bool IsRequired = true, int? PointsReward = null) : ICommand<ProgramContent>;

/// <summary> Command to remove content from a program </summary>
public record RemoveProgramContentCommand(Guid ProgramId, Guid ContentId) : ICommand<bool>;

/// <summary> Command to reorder program content </summary>
public record ReorderProgramContentCommand(Guid ProgramId, Dictionary<Guid, int> ContentOrders) : ICommand<IEnumerable<ProgramContent>>;

// ===== RATING COMMANDS =====

/// <summary> Command to rate a program </summary>
public record RateProgramCommand(Guid ProgramId, string UserId, decimal Rating, string? Review = null) : ICommand<ProgramRating>;

/// <summary> Command to update a program rating </summary>
public record UpdateProgramRatingCommand(Guid ProgramId, string UserId, decimal Rating, string? Review = null) : ICommand<ProgramRating>;

/// <summary> Command to delete a program rating </summary>
public record DeleteProgramRatingCommand(Guid ProgramId, string UserId) : ICommand<bool>;

// ===== WISHLIST COMMANDS =====

/// <summary> Command to add program to wishlist </summary>
public record AddToWishlistCommand(Guid ProgramId, string UserId) : ICommand<ProgramWishlist>;

/// <summary> Command to remove program from wishlist </summary>
public record RemoveFromWishlistCommand(Guid ProgramId, string UserId) : ICommand<bool>;

// ===== BULK OPERATIONS =====

/// <summary> Command to bulk update program visibility </summary>
public record BulkUpdateProgramVisibilityCommand(IEnumerable<Guid> ProgramIds, AccessLevel Visibility) : ICommand<IEnumerable<ProgramEntity>>;

/// <summary> Command to bulk archive programs </summary>
public record BulkArchiveProgramsCommand(IEnumerable<Guid> ProgramIds) : ICommand<IEnumerable<ProgramEntity>>;
