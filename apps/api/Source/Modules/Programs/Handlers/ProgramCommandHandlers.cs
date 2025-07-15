using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Command handlers for Program management operations
/// Implements business logic and data persistence for program commands
/// </summary>
public class ProgramCommandHandlers(
  ApplicationDbContext context,
  ILogger<ProgramCommandHandlers> logger
) :
  IRequestHandler<CreateProgramCommand, GameGuild.Program>,
  IRequestHandler<UpdateProgramCommand, GameGuild.Program>,
  IRequestHandler<DeleteProgramCommand, bool>,
  IRequestHandler<PublishProgramCommand, GameGuild.Program>,
  IRequestHandler<UnpublishProgramCommand, GameGuild.Program>,
  IRequestHandler<ArchiveProgramCommand, GameGuild.Program>,
  IRequestHandler<RestoreProgramCommand, GameGuild.Program>,
  IRequestHandler<EnrollUserCommand, ProgramUser>,
  IRequestHandler<UnenrollUserCommand, bool>,
  IRequestHandler<UpdateEnrollmentStatusCommand, GameGuild.Program>,
  IRequestHandler<AddProgramContentCommand, ProgramContent>,
  IRequestHandler<RemoveProgramContentCommand, bool>,
  IRequestHandler<ReorderProgramContentCommand, IEnumerable<ProgramContent>>,
  IRequestHandler<RateProgramCommand, ProgramRating>,
  IRequestHandler<UpdateProgramRatingCommand, ProgramRating>,
  IRequestHandler<DeleteProgramRatingCommand, bool>,
  IRequestHandler<AddToWishlistCommand, ProgramWishlist>,
  IRequestHandler<RemoveFromWishlistCommand, bool>,
  IRequestHandler<BulkUpdateProgramVisibilityCommand, IEnumerable<GameGuild.Program>>,
  IRequestHandler<BulkArchiveProgramsCommand, IEnumerable<GameGuild.Program>> {
  // ===== CRUD HANDLERS =====

  public async Task<GameGuild.Program> Handle(CreateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Creating new program: {Title}", request.Title);

    // Generate slug from title
    var slug = request.Title.ToSlugCase();

    // Ensure slug uniqueness
    var existingSlug = await context.Programs
                                    .Where(p => p.Slug == slug && p.DeletedAt == null)
                                    .FirstOrDefaultAsync(cancellationToken);

    if (existingSlug != null) { slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}"; }

    var program = new GameGuild.Program {
      Id = Guid.NewGuid(),
      Title = request.Title,
      Description = request.Description,
      Summary = request.Summary,
      Slug = slug,
      Thumbnail = request.Thumbnail,
      VideoShowcaseUrl = request.VideoShowcaseUrl,
      EstimatedHours = request.EstimatedHours,
      Category = request.Category,
      Difficulty = request.Difficulty,
      EnrollmentStatus = request.EnrollmentStatus,
      MaxEnrollments = request.MaxEnrollments,
      EnrollmentDeadline = request.EnrollmentDeadline,
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Private,
      CreatorId = request.CreatorId,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    context.Programs.Add(program);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Created program with ID: {ProgramId}", program.Id);

    return program;
  }

  public async Task<GameGuild.Program> Handle(UpdateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating program: {ProgramId}", request.Id);

    var program = await context.Programs
                               .Where(p => p.Id == request.Id && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    // Update only provided fields
    if (request.Title != null) {
      program.Title = request.Title;
      program.Slug = request.Title.ToSlugCase();
    }

    if (request.Description != null) program.Description = request.Description;
    if (request.Summary != null) program.Summary = request.Summary;
    if (request.Thumbnail != null) program.Thumbnail = request.Thumbnail;
    if (request.VideoShowcaseUrl != null) program.VideoShowcaseUrl = request.VideoShowcaseUrl;
    if (request.EstimatedHours.HasValue) program.EstimatedHours = request.EstimatedHours;
    if (request.Category.HasValue) program.Category = request.Category.Value;
    if (request.Difficulty.HasValue) program.Difficulty = request.Difficulty.Value;
    if (request.EnrollmentStatus.HasValue) program.EnrollmentStatus = request.EnrollmentStatus.Value;
    if (request.MaxEnrollments.HasValue) program.MaxEnrollments = request.MaxEnrollments;
    if (request.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = request.EnrollmentDeadline;

    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Updated program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<bool> Handle(DeleteProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Deleting program: {ProgramId}", request.Id);

    var program = await context.Programs
                               .Where(p => p.Id == request.Id && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { return false; }

    // Soft delete
    program.DeletedAt = DateTime.UtcNow;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Deleted program: {ProgramId}", program.Id);

    return true;
  }

  // ===== STATUS HANDLERS =====

  public async Task<GameGuild.Program> Handle(PublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Publishing program: {ProgramId}", request.Id);

    var program = await context.Programs
                               .Where(p => p.Id == request.Id && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Published;
    program.Visibility = AccessLevel.Public;
    program.PublishedAt = DateTime.UtcNow;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Published program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<GameGuild.Program> Handle(UnpublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unpublishing program: {ProgramId}", request.Id);

    var program = await context.Programs
                               .Where(p => p.Id == request.Id && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.Visibility = AccessLevel.Private;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Unpublished program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<GameGuild.Program> Handle(ArchiveProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Archiving program: {ProgramId}", request.Id);

    var program = await context.Programs
                               .Where(p => p.Id == request.Id && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Archived;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Archived program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<GameGuild.Program> Handle(RestoreProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Restoring program: {ProgramId}", request.Id);

    var program = await context.Programs
                               .Where(p => p.Id == request.Id && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Restored program: {ProgramId}", program.Id);

    return program;
  }

  // ===== ENROLLMENT HANDLERS =====

  public async Task<ProgramUser> Handle(EnrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Enrolling user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    var program = await context.Programs
                               .Where(p => p.Id == request.ProgramId && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    if (!program.IsEnrollmentOpen) { throw new InvalidOperationException("Program enrollment is not open"); }

    // Check if already enrolled
    var existingEnrollment = await context.ProgramUsers
                                          .Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == request.UserId)
                                          .FirstOrDefaultAsync(cancellationToken);

    if (existingEnrollment != null && existingEnrollment.IsActive) { throw new InvalidOperationException("User is already enrolled in this program"); }

    var enrollment = new ProgramUser {
      ProgramId = request.ProgramId,
      UserId = request.UserId,
      EnrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    context.ProgramUsers.Add(enrollment);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Enrolled user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    return enrollment;
  }

  public async Task<bool> Handle(UnenrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unenrolling user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    var enrollment = await context.ProgramUsers
                                  .Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == request.UserId && pu.IsActive)
                                  .FirstOrDefaultAsync(cancellationToken);

    if (enrollment == null) { return false; }

    enrollment.IsActive = false;
    enrollment.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Unenrolled user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    return true;
  }

  public async Task<GameGuild.Program> Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating enrollment status for program: {ProgramId}", request.ProgramId);

    var program = await context.Programs
                               .Where(p => p.Id == request.ProgramId && p.DeletedAt == null)
                               .FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    program.EnrollmentStatus = request.Status;
    if (request.MaxEnrollments.HasValue) program.MaxEnrollments = request.MaxEnrollments;
    if (request.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = request.EnrollmentDeadline;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Updated enrollment status for program: {ProgramId}", program.Id);

    return program;
  }

  // ===== CONTENT MANAGEMENT HANDLERS =====

  public async Task<ProgramContent> Handle(AddProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding content {ContentId} to program {ProgramId}", request.ContentId, request.ProgramId);

    var programContent = new ProgramContent {
      ProgramId = request.ProgramId,
      ContentId = request.ContentId,
      Order = request.Order,
      IsRequired = request.IsRequired,
      PointsReward = request.PointsReward,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    context.ProgramContents.Add(programContent);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Added content {ContentId} to program {ProgramId}", request.ContentId, request.ProgramId);

    return programContent;
  }

  public async Task<bool> Handle(RemoveProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Removing content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    var programContent = await context.ProgramContents
                                      .Where(pc => pc.ProgramId == request.ProgramId && pc.ContentId == request.ContentId)
                                      .FirstOrDefaultAsync(cancellationToken);

    if (programContent == null) { return false; }

    context.ProgramContents.Remove(programContent);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Removed content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    return true;
  }

  public async Task<IEnumerable<ProgramContent>> Handle(ReorderProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Reordering content for program {ProgramId}", request.ProgramId);

    var programContents = await context.ProgramContents
                                       .Where(pc => pc.ProgramId == request.ProgramId && request.ContentOrders.Keys.Contains(pc.ContentId))
                                       .ToListAsync(cancellationToken);

    foreach (var programContent in programContents) {
      if (request.ContentOrders.TryGetValue(programContent.ContentId, out var newOrder)) {
        programContent.Order = newOrder;
        programContent.UpdatedAt = DateTime.UtcNow;
      }
    }

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Reordered content for program {ProgramId}", request.ProgramId);

    return programContents;
  }

  // ===== RATING HANDLERS =====

  public async Task<ProgramRating> Handle(RateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var existingRating = await context.ProgramRatings
                                      .Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId)
                                      .FirstOrDefaultAsync(cancellationToken);

    if (existingRating != null) { throw new InvalidOperationException("User has already rated this program"); }

    var rating = new ProgramRating {
      ProgramId = request.ProgramId,
      UserId = request.UserId,
      Rating = request.Rating,
      Review = request.Review,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    context.ProgramRatings.Add(rating);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Added rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return rating;
  }

  public async Task<ProgramRating> Handle(UpdateProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.ProgramRatings
                              .Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId)
                              .FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { throw new InvalidOperationException("Rating not found"); }

    rating.Rating = request.Rating;
    rating.Review = request.Review;
    rating.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Updated rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return rating;
  }

  public async Task<bool> Handle(DeleteProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Deleting rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.ProgramRatings
                              .Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId)
                              .FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { return false; }

    context.ProgramRatings.Remove(rating);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Deleted rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return true;
  }

  // ===== WISHLIST HANDLERS =====

  public async Task<ProgramWishlist> Handle(AddToWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    var existingWishlist = await context.ProgramWishlists
                                        .Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == request.UserId)
                                        .FirstOrDefaultAsync(cancellationToken);

    if (existingWishlist != null) { throw new InvalidOperationException("Program is already in user's wishlist"); }

    var wishlist = new ProgramWishlist { ProgramId = request.ProgramId, UserId = request.UserId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    context.ProgramWishlists.Add(wishlist);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Added program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    return wishlist;
  }

  public async Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Removing program {ProgramId} from wishlist for user {UserId}", request.ProgramId, request.UserId);

    var wishlist = await context.ProgramWishlists
                                .Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == request.UserId)
                                .FirstOrDefaultAsync(cancellationToken);

    if (wishlist == null) { return false; }

    context.ProgramWishlists.Remove(wishlist);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Removed program {ProgramId} from wishlist for user {UserId}", request.ProgramId, request.UserId);

    return true;
  }

  // ===== BULK OPERATION HANDLERS =====

  public async Task<IEnumerable<GameGuild.Program>> Handle(BulkUpdateProgramVisibilityCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Bulk updating visibility for {Count} programs", request.ProgramIds.Count());

    var programs = await context.Programs
                                .Where(p => request.ProgramIds.Contains(p.Id) && p.DeletedAt == null)
                                .ToListAsync(cancellationToken);

    foreach (var program in programs) {
      program.Visibility = request.Visibility;
      program.UpdatedAt = DateTime.UtcNow;
    }

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Bulk updated visibility for {Count} programs", programs.Count);

    return programs;
  }

  public async Task<IEnumerable<GameGuild.Program>> Handle(BulkArchiveProgramsCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Bulk archiving {Count} programs", request.ProgramIds.Count());

    var programs = await context.Programs
                                .Where(p => request.ProgramIds.Contains(p.Id) && p.DeletedAt == null)
                                .ToListAsync(cancellationToken);

    foreach (var program in programs) {
      program.Status = ContentStatus.Archived;
      program.UpdatedAt = DateTime.UtcNow;
    }

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Bulk archived {Count} programs", programs.Count);

    return programs;
  }
}
