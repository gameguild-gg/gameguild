using GameGuild.Database;

namespace GameGuild.Modules.Users;

/// <summary>
/// Service for managing user segmentation, tags, and cohorts
/// </summary>
public interface ISegmentationService {
    // Tag operations
    Task<Result<UserTag>> AssignTagAsync(Guid userId, string tagName, string? category = null, string? value = null,
        DateTime? expiresAt = null, string source = "manual", CancellationToken cancellationToken = default);
    Task<Result> RemoveTagAsync(Guid userId, string tagName, CancellationToken cancellationToken = default);
    Task<Result<List<UserTag>>> GetUserTagsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> RemoveExpiredTagsAsync(CancellationToken cancellationToken = default);

    // Segment operations
    Task<Result<UserSegment>> CreateSegmentAsync(string name, string? description, string rules,
        SegmentType type = SegmentType.Dynamic, int refreshIntervalMinutes = 60, CancellationToken cancellationToken = default);
    Task<Result<UserSegment>> UpdateSegmentAsync(Guid segmentId, string? name = null, string? description = null,
        string? rules = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<Result> RefreshSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default);
    Task<Result<List<UserSegment>>> GetActiveSegmentsAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> GetSegmentMemberCountAsync(Guid segmentId, CancellationToken cancellationToken = default);

    // Cohort operations
    Task<Result<UserCohort>> AssignToCohortAsync(Guid userId, string cohortName, CohortType type = CohortType.Behavioral,
        string? metadata = null, CancellationToken cancellationToken = default);
    Task<Result> RemoveFromCohortAsync(Guid userId, string cohortName, CancellationToken cancellationToken = default);
    Task<Result<List<UserCohort>>> GetUserCohortsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<List<Guid>>> GetCohortMemberIdsAsync(string cohortName, CancellationToken cancellationToken = default);
}

public sealed class SegmentationService : ISegmentationService {
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SegmentationService> _logger;
    private readonly ApplicationDbContext _context;

    public SegmentationService(
        IUserRepository userRepository,
        ILogger<SegmentationService> logger,
        ApplicationDbContext context) {
        _userRepository = userRepository;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<UserTag>> AssignTagAsync(Guid userId, string tagName, string? category = null,
        string? value = null, DateTime? expiresAt = null, string source = "manual",
        CancellationToken cancellationToken = default) {
        try {
            // Verify user exists
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) {
                return Result<UserTag>.Failure(Error.NotFound("User.NotFound", $"User with ID {userId} not found"));
            }

            // Check if tag already exists
            var existingTag = await _context.Set<UserTag>()
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TagName == tagName, cancellationToken);

            if (existingTag != null) {
                // Update existing tag
                existingTag.Category = category;
                existingTag.Value = value;
                existingTag.ExpiresAt = expiresAt;
                existingTag.Source = source;
                existingTag.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated tag {TagName} for user {UserId}", tagName, userId);
                return Result<UserTag>.Success(existingTag);
            }

            // Create new tag
            var tag = new UserTag {
                UserId = userId,
                TagName = tagName,
                Category = category,
                Value = value,
                ExpiresAt = expiresAt,
                Source = source
            };

            await _context.Set<UserTag>().AddAsync(tag, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Assigned tag {TagName} to user {UserId}", tagName, userId);
            return Result<UserTag>.Success(tag);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error assigning tag {TagName} to user {UserId}", tagName, userId);
            return Result<UserTag>.Failure(Error.Failure("UserTag.AssignFailed", $"Failed to assign tag: {ex.Message}"));
        }
    }

    public async Task<Result> RemoveTagAsync(Guid userId, string tagName, CancellationToken cancellationToken = default) {
        try {
            var tag = await _context.Set<UserTag>()
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TagName == tagName, cancellationToken);

            if (tag == null) {
                return Result.Failure(Error.NotFound("UserTag.NotFound", $"Tag {tagName} not found for user {userId}"));
            }

            _context.Set<UserTag>().Remove(tag);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed tag {TagName} from user {UserId}", tagName, userId);
            return Result.Success();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error removing tag {TagName} from user {UserId}", tagName, userId);
            return Result.Failure(Error.Failure("UserTag.RemoveFailed", $"Failed to remove tag: {ex.Message}"));
        }
    }

    public async Task<Result<List<UserTag>>> GetUserTagsAsync(Guid userId, CancellationToken cancellationToken = default) {
        try {
            var tags = await _context.Set<UserTag>()
                .Where(t => t.UserId == userId)
                .Where(t => !t.ExpiresAt.HasValue || t.ExpiresAt.Value > DateTime.UtcNow)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.TagName)
                .ToListAsync(cancellationToken);

            return Result<List<UserTag>>.Success(tags);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving tags for user {UserId}", userId);
            return Result<List<UserTag>>.Failure($"Failed to retrieve tags: {ex.Message}");
        }
    }

    public async Task<Result> RemoveExpiredTagsAsync(CancellationToken cancellationToken = default) {
        try {
            var expiredTags = await _context.Set<UserTag>()
                .Where(t => t.ExpiresAt.HasValue && t.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredTags.Any()) {
                _context.Set<UserTag>().RemoveRange(expiredTags);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Removed {Count} expired tags", expiredTags.Count);
            }

            return Result.Success();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error removing expired tags");
            return Result.Failure($"Failed to remove expired tags: {ex.Message}");
        }
    }

    public async Task<Result<UserSegment>> CreateSegmentAsync(string name, string? description, string rules,
        SegmentType type = SegmentType.Dynamic, int refreshIntervalMinutes = 60,
        CancellationToken cancellationToken = default) {
        try {
            // Check if segment with same name exists
            var exists = await _context.Set<UserSegment>()
                .AnyAsync(s => s.Name == name, cancellationToken);

            if (exists) {
                return Result<UserSegment>.Failure($"Segment with name '{name}' already exists");
            }

            var segment = new UserSegment {
                Name = name,
                Description = description,
                Rules = rules,
                Type = type,
                RefreshIntervalMinutes = refreshIntervalMinutes,
                IsActive = true
            };

            await _context.Set<UserSegment>().AddAsync(segment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created segment {SegmentName} with type {SegmentType}", name, type);
            return Result<UserSegment>.Success(segment);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error creating segment {SegmentName}", name);
            return Result<UserSegment>.Failure($"Failed to create segment: {ex.Message}");
        }
    }

    public async Task<Result<UserSegment>> UpdateSegmentAsync(Guid segmentId, string? name = null,
        string? description = null, string? rules = null, bool? isActive = null,
        CancellationToken cancellationToken = default) {
        try {
            var segment = await _context.Set<UserSegment>()
                .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);

            if (segment == null) {
                return Result<UserSegment>.Failure($"Segment with ID {segmentId} not found");
            }

            if (name != null) segment.Name = name;
            if (description != null) segment.Description = description;
            if (rules != null) segment.Rules = rules;
            if (isActive.HasValue) segment.IsActive = isActive.Value;

            segment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated segment {SegmentId}", segmentId);
            return Result<UserSegment>.Success(segment);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error updating segment {SegmentId}", segmentId);
            return Result<UserSegment>.Failure($"Failed to update segment: {ex.Message}");
        }
    }

    public async Task<Result> RefreshSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default) {
        try {
            var segment = await _context.Set<UserSegment>()
                .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);

            if (segment == null) {
                return Result.Failure($"Segment with ID {segmentId} not found");
            }

            // For dynamic segments, recalculate membership based on rules
            // Note: This is a simplified implementation. In production, you would parse
            // the Rules JSON and evaluate it against user data
            if (segment.Type == SegmentType.Dynamic) {
                // Placeholder for rule evaluation logic
                // In a real implementation, you'd parse segment.Rules and execute the query
                segment.MemberCount = 0; // Would be calculated based on actual rule evaluation
                segment.LastCalculatedAt = DateTime.UtcNow;
                segment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Refreshed segment {SegmentId}, member count: {Count}",
                    segmentId, segment.MemberCount);
            }

            return Result.Success();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error refreshing segment {SegmentId}", segmentId);
            return Result.Failure($"Failed to refresh segment: {ex.Message}");
        }
    }

    public async Task<Result<List<UserSegment>>> GetActiveSegmentsAsync(CancellationToken cancellationToken = default) {
        try {
            var segments = await _context.Set<UserSegment>()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return Result<List<UserSegment>>.Success(segments);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving active segments");
            return Result<List<UserSegment>>.Failure($"Failed to retrieve segments: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetSegmentMemberCountAsync(Guid segmentId, CancellationToken cancellationToken = default) {
        try {
            var segment = await _context.Set<UserSegment>()
                .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);

            if (segment == null) {
                return Result<int>.Failure($"Segment with ID {segmentId} not found");
            }

            return Result<int>.Success(segment.MemberCount);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving segment member count for {SegmentId}", segmentId);
            return Result<int>.Failure($"Failed to retrieve member count: {ex.Message}");
        }
    }

    public async Task<Result<UserCohort>> AssignToCohortAsync(Guid userId, string cohortName,
        CohortType type = CohortType.Behavioral, string? metadata = null,
        CancellationToken cancellationToken = default) {
        try {
            // Verify user exists
            var userExists = await _userRepository.ExistsAsync(userId, cancellationToken);
            if (!userExists) {
                return Result<UserCohort>.Failure($"User with ID {userId} not found");
            }

            // Check if already in cohort
            var existingCohort = await _context.Set<UserCohort>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CohortName == cohortName, cancellationToken);

            if (existingCohort != null) {
                return Result<UserCohort>.Success(existingCohort);
            }

            // Add to cohort
            var cohort = new UserCohort {
                UserId = userId,
                CohortName = cohortName,
                Type = type,
                JoinedAt = DateTime.UtcNow,
                Metadata = metadata
            };

            await _context.Set<UserCohort>().AddAsync(cohort, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Assigned user {UserId} to cohort {CohortName}", userId, cohortName);
            return Result<UserCohort>.Success(cohort);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error assigning user {UserId} to cohort {CohortName}", userId, cohortName);
            return Result<UserCohort>.Failure($"Failed to assign to cohort: {ex.Message}");
        }
    }

    public async Task<Result> RemoveFromCohortAsync(Guid userId, string cohortName, CancellationToken cancellationToken = default) {
        try {
            var cohort = await _context.Set<UserCohort>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CohortName == cohortName, cancellationToken);

            if (cohort == null) {
                return Result.Failure($"User {userId} not found in cohort {cohortName}");
            }

            _context.Set<UserCohort>().Remove(cohort);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed user {UserId} from cohort {CohortName}", userId, cohortName);
            return Result.Success();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error removing user {UserId} from cohort {CohortName}", userId, cohortName);
            return Result.Failure($"Failed to remove from cohort: {ex.Message}");
        }
    }

    public async Task<Result<List<UserCohort>>> GetUserCohortsAsync(Guid userId, CancellationToken cancellationToken = default) {
        try {
            var cohorts = await _context.Set<UserCohort>()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.JoinedAt)
                .ToListAsync(cancellationToken);

            return Result<List<UserCohort>>.Success(cohorts);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving cohorts for user {UserId}", userId);
            return Result<List<UserCohort>>.Failure($"Failed to retrieve cohorts: {ex.Message}");
        }
    }

    public async Task<Result<List<Guid>>> GetCohortMemberIdsAsync(string cohortName, CancellationToken cancellationToken = default) {
        try {
            var memberIds = await _context.Set<UserCohort>()
                .Where(c => c.CohortName == cohortName)
                .Select(c => c.UserId)
                .ToListAsync(cancellationToken);

            return Result<List<Guid>>.Success(memberIds);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving members for cohort {CohortName}", cohortName);
            return Result<List<Guid>>.Failure($"Failed to retrieve cohort members: {ex.Message}");
        }
    }
}
