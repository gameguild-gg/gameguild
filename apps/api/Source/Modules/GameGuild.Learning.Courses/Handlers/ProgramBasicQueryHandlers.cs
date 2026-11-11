using GameGuild.CQRS;




using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Query handlers for basic Program retrieval operations:
/// get all, search, filter by category/difficulty/creator.
/// The by-id / by-slug / published-by-slug reads are owned by the dedicated handlers in
/// Queries/GetProgramById, Queries/GetProgramBySlug and Queries/GetPublishedProgramBySlug
/// (issue #1277: this class previously registered verbatim duplicates of those three,
/// which resolved last-wins-silently in DI).
/// </summary>
public sealed class ProgramBasicQueryHandlers(IApplicationDbContext context, ILogger<ProgramBasicQueryHandlers> logger, IRequestContextAccessor? requestContextAccessor = null, IPublisher? publisher = null) : IRequestHandler<GetAllProgramsQuery, IEnumerable<Program>>,
                                                                                                                    IRequestHandler<SearchProgramsQuery, IEnumerable<Program>>,
                                                                                                                    IRequestHandler<GetProgramsByCategoryQuery, IEnumerable<Program>>,
                                                                                                                    IRequestHandler<GetProgramsByDifficultyQuery, IEnumerable<Program>>,
                                                                                                                    IRequestHandler<GetProgramsByCreatorQuery, IEnumerable<Program>> {
  public async Task<IEnumerable<Program>> Handle(GetAllProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting all programs with filters");

    var query = context.Set<Program>().Where(p => p.DeletedAt == null);

    // Apply filters
    if (!string.IsNullOrEmpty(request.Search)) { query = query.Where(p => p.Title.Contains(request.Search) || p.Description != null && p.Description.Contains(request.Search)); }

    if (request.Category.HasValue)
        {
            query = query.Where(p => p.Category == request.Category.Value);
        }

        if (request.Difficulty.HasValue)
        {
            query = query.Where(p => p.Difficulty == request.Difficulty.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (request.Visibility.HasValue)
        {
            query = query.Where(p => p.Visibility == request.Visibility.Value);
        }

        if (request.EnrollmentStatus.HasValue)
        {
            query = query.Where(p => p.EnrollmentStatus == (EnrollmentStatus)request.EnrollmentStatus.Value);
        }

        if (!string.IsNullOrEmpty(request.CreatorId))
        {
            if (!Guid.TryParse(request.CreatorId, out var creatorId) || creatorId == Guid.Empty)
            {
                return Array.Empty<Program>();
            }

            query = query.Where(p => p.CreatorId == creatorId);
        }

        if (!request.IncludeArchived)
        {
            query = query.Where(p => p.Status != ContentStatus.Archived);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch {
      "title" => request.SortDescending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
      "createdat" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
      "updatedat" => request.SortDescending ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
      "category" => request.SortDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
      "difficulty" => request.SortDescending ? query.OrderByDescending(p => p.Difficulty) : query.OrderBy(p => p.Difficulty),
      _ => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
    };

    var programs = await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Retrieved {Count} programs", programs.Count);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(SearchProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Searching programs with term: {SearchTerm}", request.SearchTerm);

    var query = context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public);

    // Text search
    query = query.Where(p => p.Title.Contains(request.SearchTerm) || p.Description != null && p.Description.Contains(request.SearchTerm));

    // Apply filters
    if (request.Category.HasValue)
        {
            query = query.Where(p => p.Category == request.Category.Value);
        }

        if (request.Difficulty.HasValue)
        {
            query = query.Where(p => p.Difficulty == request.Difficulty.Value);
        }

        if (request.MinEstimatedHours.HasValue)
        {
            query = query.Where(p => p.EstimatedHours >= request.MinEstimatedHours.Value);
        }

        if (request.MaxEstimatedHours.HasValue)
        {
            query = query.Where(p => p.EstimatedHours <= request.MaxEstimatedHours.Value);
        }

        if (request.MinRating.HasValue) { query = query.Where(p => p.ProgramRatings.Any() && p.ProgramRatings.Average(pr => pr.Rating) >= request.MinRating.Value); }

    if (request.AvailableForEnrollment) {
      query = query.Where(p => p.EnrollmentStatus == EnrollmentStatus.Open &&
                               (p.MaxEnrollments == null || p.ProgramUsers.Count(pu => pu.IsActive) < p.MaxEnrollments) &&
                               (p.EnrollmentDeadline == null || p.EnrollmentDeadline > SystemClock.UtcNow)
      );
    }

    var programs = await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Found {Count} programs matching search", programs.Count);

    // Issue #1184 G4: SearchPerformedEvent was declared but never raised; the public search
    // query is its producing site (result count feeds the search-history analytics consumer;
    // anonymous searches carry a null user by design).
    if (publisher is not null)
    {
        await publisher.Publish(
            new GameGuild.Learning.SearchPerformedEvent(
                requestContextAccessor?.CurrentUserId,
                request.SearchTerm,
                programs.Count,
                requestContextAccessor?.CurrentTenantId),
            cancellationToken).ConfigureAwait(false);
    }

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetProgramsByCategoryQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting programs by category: {Category}", request.Category);

    var query = context.Set<Program>().Where(p => p.Category == request.Category && p.DeletedAt == null);

    if (request.OnlyPublished) { query = query.Where(p => p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public); }

    var programs = await query.OrderByDescending(p => p.CreatedAt).Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs in category {Category}", programs.Count, request.Category);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetProgramsByDifficultyQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting programs by difficulty: {Difficulty}", request.Difficulty);

    var query = context.Set<Program>().Where(p => p.Difficulty == request.Difficulty && p.DeletedAt == null);

    if (request.OnlyPublished) { query = query.Where(p => p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public); }

    var programs = await query.OrderByDescending(p => p.CreatedAt).Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs with difficulty {Difficulty}", programs.Count, request.Difficulty);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetProgramsByCreatorQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting programs by creator: {CreatorId}", request.CreatorId);

    if (!Guid.TryParse(request.CreatorId, out var creatorId) || creatorId == Guid.Empty)
    {
        return Array.Empty<Program>();
    }

    var query = context.Set<Program>().Where(p => p.CreatorId == creatorId && p.DeletedAt == null);

    if (request.OnlyPublished) { query = query.Where(p => p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public); }

    var programs = await query.OrderByDescending(p => p.CreatedAt).Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs by creator {CreatorId}", programs.Count, request.CreatorId);

    return programs;
  }
}
