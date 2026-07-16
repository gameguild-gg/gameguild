using GameGuild.CQRS;





using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// CQRS command handlers for comprehensive program management operations
/// </summary>
/// <remarks>
/// ProgramCommandHandlers implements the command side of CQRS pattern for program operations including:
/// - Program lifecycle management (Create, Update, Delete, Publish, Archive)
/// - Content management and organization within programs
/// - User enrollment and participation management
/// - Rating and wishlist operations
/// - Bulk administrative operations
/// 
/// Each handler encapsulates business logic, validation, and data persistence
/// for specific command operations while maintaining transaction integrity
/// and consistent logging for audit and monitoring purposes.
/// </remarks>
public sealed class ProgramCommandHandlers(IApplicationDbContext context, ILogger<ProgramCommandHandlers> logger) : IRequestHandler<CreateProgramCommand, Program>,
                                                                                                            IRequestHandler<UpdateProgramCommand, Program>,
                                                                                                            IRequestHandler<DeleteProgramCommand, bool>,
                                                                                                            IRequestHandler<PublishProgramCommand, Program>,
                                                                                                            IRequestHandler<UnpublishProgramCommand, Program>,
                                                                                                            IRequestHandler<ArchiveProgramCommand, Program>,
                                                                                                            IRequestHandler<RestoreProgramCommand, Program>,
                                                                                                            IRequestHandler<EnrollUserCommand, ProgramUser>,
                                                                                                            IRequestHandler<UnenrollUserCommand, bool>,
                                                                                                            IRequestHandler<UpdateEnrollmentStatusCommand, Program>,
                                                                                                            IRequestHandler<AddProgramContentCommand, ProgramContent>,
                                                                                                            IRequestHandler<ReorderProgramContentCommand, IEnumerable<ProgramContent>>,
                                                                                                            IRequestHandler<RateProgramCommand, ProgramRating>,
                                                                                                            IRequestHandler<UpdateProgramRatingCommand, ProgramRating>,
                                                                                                            IRequestHandler<DeleteProgramRatingCommand, bool>,
                                                                                                            IRequestHandler<AddToWishlistCommand, ProgramWishlist>,
                                                                                                            IRequestHandler<RemoveFromWishlistCommand, bool>,
                                                                                                            IRequestHandler<BulkUpdateProgramVisibilityCommand, IEnumerable<Program>>,
                                                                                                            IRequestHandler<BulkArchiveProgramsCommand, IEnumerable<Program>> {
  // ===== CONTENT MANAGEMENT HANDLERS =====

  public async Task<ProgramContent> Handle(AddProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding content {ContentId} to program {ProgramId}", request.ContentId, request.ProgramId);

    var programContent = new ProgramContent {
      ProgramId = request.ProgramId,
      // ContentId = request.ContentId,  // This property doesn't exist in the current model
      SortOrder = request.Order,
      IsRequired = request.IsRequired,
      // PointsReward = request.PointsReward,  // This property doesn't exist in the current model
    };

    context.Set<ProgramContent>().Add(programContent);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Added content {ContentId} to program {ProgramId}", request.ContentId, request.ProgramId);

    return programContent;
  }

  // ===== WISHLIST HANDLERS =====

  public async Task<ProgramWishlist> Handle(AddToWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    var existingWishlist = await context.Set<ProgramWishlist>().Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (existingWishlist != null) { throw new InvalidOperationException("Program is already in user's wishlist"); }

    var wishlist = new ProgramWishlist { ProgramId = request.ProgramId, UserId = Guid.Parse(request.UserId) };

    context.Set<ProgramWishlist>().Add(wishlist);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Added program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    return wishlist;
  }

  public async Task<Program> Handle(ArchiveProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Archiving program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Archived;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Archived program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<IEnumerable<Program>> Handle(BulkArchiveProgramsCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Bulk archiving {Count} programs", request.ProgramIds.Count());

    var programs = await context.Set<Program>().Where(p => request.ProgramIds.Contains(p.Id) && p.DeletedAt == null).ToListAsync(cancellationToken);

    foreach (var program in programs) {
      program.Status = ContentStatus.Archived;
      program.Touch();
    }

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Bulk archived {Count} programs", programs.Count);

    return programs;
  }

  // ===== BULK OPERATION HANDLERS =====

  public async Task<IEnumerable<Program>> Handle(BulkUpdateProgramVisibilityCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Bulk updating visibility for {Count} programs", request.ProgramIds.Count());

    var programs = await context.Set<Program>().Where(p => request.ProgramIds.Contains(p.Id) && p.DeletedAt == null).ToListAsync(cancellationToken);

    foreach (var program in programs) {
      program.Visibility = request.Visibility;
      program.Touch();
    }

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Bulk updated visibility for {Count} programs", programs.Count);

    return programs;
  }
  // ===== CRUD HANDLERS =====

  public async Task<Program> Handle(CreateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Creating new program: {Title}", request.Title);

    // Generate slug from title
    var slug = request.Title.ToSlugCase();

    // Ensure slug uniqueness
    var existingSlug = await context.Set<Program>().Where(p => p.Slug == slug && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (existingSlug != null) { slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}"; }

    var program = new Program {
      Id = Guid.NewGuid(),
      Title = request.Title,
      Description = request.Description,
      Slug = slug,
      Thumbnail = request.Thumbnail,
      VideoShowcaseUrl = request.VideoShowcaseUrl,
      EstimatedHours = (int?)request.EstimatedHours,
      Category = request.Category,
      Difficulty = request.Difficulty,
      EnrollmentStatus = request.EnrollmentStatus,
      MaxEnrollments = request.MaxEnrollments,
      EnrollmentDeadline = request.EnrollmentDeadline,
      Status = ContentStatus.Draft,
      Visibility = ContentVisibility.Private,
    };

    context.Set<Program>().Add(program);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Created program with ID: {ProgramId}", program.Id);

    return program;
  }

  public async Task<bool> Handle(DeleteProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Deleting program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { return false; }

    // Soft delete
    program.SoftDelete();
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Deleted program: {ProgramId}", program.Id);

    return true;
  }

  public async Task<bool> Handle(DeleteProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Deleting rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { return false; }

    context.Set<ProgramRating>().Remove(rating);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Deleted rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return true;
  }

  // ===== ENROLLMENT HANDLERS =====

  public async Task<ProgramUser> Handle(EnrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Enrolling user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    var program = await context.Set<Program>().Where(p => p.Id == request.ProgramId && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    if (!program.IsEnrollmentOpen) { throw new InvalidOperationException("Program enrollment is not open"); }

    // Check if already enrolled
    var existingEnrollment = await context.Set<ProgramUser>().Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (existingEnrollment != null && existingEnrollment.IsActive) { throw new InvalidOperationException("User is already enrolled in this program"); }

    var enrollment = new ProgramUser { ProgramId = request.ProgramId, UserId = Guid.Parse(request.UserId), JoinedAt = request.EnrollmentDate ?? SystemClock.UtcNow, IsActive = true };

    context.Set<ProgramUser>().Add(enrollment);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Enrolled user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    return enrollment;
  }

  // ===== STATUS HANDLERS =====

  public async Task<Program> Handle(PublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Publishing program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Published;
    program.Visibility = ContentVisibility.Public;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Published program: {ProgramId}", program.Id);

    return program;
  }

  // ===== RATING HANDLERS =====

  public async Task<ProgramRating> Handle(RateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var existingRating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (existingRating != null) { throw new InvalidOperationException("User has already rated this program"); }

    ProgramUser? enrollment = null;
    if (Guid.TryParse(request.UserId, out var userGuid))
    {
      enrollment = await context.Set<ProgramUser>()
                                .FirstOrDefaultAsync(
                                  pu => pu.ProgramId == request.ProgramId && pu.UserId == userGuid,
                                  cancellationToken)
                                .ConfigureAwait(false);
    }

    var rating = new ProgramRating {
      ProgramId = request.ProgramId,
      UserId = request.UserId,
      ProgramUserId = enrollment?.Id,
      Rating = request.Rating,
      Review = request.Review,
      IsVerified = enrollment?.CompletedAt is not null
    };

    context.Set<ProgramRating>().Add(rating);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Added rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return rating;
  }

  public async Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Removing program {ProgramId} from wishlist for user {UserId}", request.ProgramId, request.UserId);

    var wishlist = await context.Set<ProgramWishlist>().Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (wishlist == null) { return false; }

    context.Set<ProgramWishlist>().Remove(wishlist);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Removed program {ProgramId} from wishlist for user {UserId}", request.ProgramId, request.UserId);

    return true;
  }

  public async Task<IEnumerable<ProgramContent>> Handle(ReorderProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Reordering content for program {ProgramId}", request.ProgramId);

    var programContents = await context.Set<ProgramContent>().Where(pc => pc.ProgramId == request.ProgramId && request.ContentOrders.Keys.Contains(pc.Id)).ToListAsync(cancellationToken);

    foreach (var programContent in programContents) {
      if (request.ContentOrders.TryGetValue(programContent.Id, out var newOrder)) {
        programContent.SortOrder = newOrder;
        programContent.Touch();
      }
    }

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Reordered content for program {ProgramId}", request.ProgramId);

    return programContents;
  }

  public async Task<Program> Handle(RestoreProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Restoring program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Restored program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<bool> Handle(UnenrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unenrolling user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    var enrollment = await context.Set<ProgramUser>().Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == Guid.Parse(request.UserId) && pu.IsActive).FirstOrDefaultAsync(cancellationToken);

    if (enrollment == null) { return false; }

    enrollment.IsActive = false;
    enrollment.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Unenrolled user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    return true;
  }

  public async Task<Program> Handle(UnpublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unpublishing program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Unpublished program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<Program> Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating enrollment status for program: {ProgramId}", request.ProgramId);

    var program = await context.Set<Program>().Where(p => p.Id == request.ProgramId && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    program.EnrollmentStatus = (EnrollmentStatus)request.Status;
    if (request.MaxEnrollments.HasValue) program.MaxEnrollments = request.MaxEnrollments;
    if (request.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = request.EnrollmentDeadline;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Updated enrollment status for program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<Program> Handle(UpdateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    // Update only provided fields
    if (request.Title != null) {
      program.Title = request.Title;
      program.Slug = request.Title.ToSlugCase();
    }

    if (request.Description != null) program.Description = request.Description;
    if (request.Thumbnail != null) program.Thumbnail = request.Thumbnail;
    if (request.VideoShowcaseUrl != null) program.VideoShowcaseUrl = request.VideoShowcaseUrl;
    if (request.EstimatedHours.HasValue) program.EstimatedHours = (int?)request.EstimatedHours;
    if (request.Category.HasValue) program.Category = request.Category.Value;
    if (request.Difficulty.HasValue) program.Difficulty = request.Difficulty.Value;
    if (request.EnrollmentStatus.HasValue) program.EnrollmentStatus = (EnrollmentStatus)request.EnrollmentStatus.Value;
    if (request.MaxEnrollments.HasValue) program.MaxEnrollments = request.MaxEnrollments;
    if (request.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = request.EnrollmentDeadline;

    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Updated program: {ProgramId}", program.Id);

    return program;
  }

  public async Task<ProgramRating> Handle(UpdateProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { throw new InvalidOperationException("Rating not found"); }

    rating.Rating = request.Rating;
    rating.Review = request.Review;
    rating.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Updated rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return rating;
  }
}
