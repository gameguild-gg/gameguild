namespace GameGuild.Modules.Product.Roadmap;

/// <summary>
/// Represents a roadmap item.
/// </summary>
public sealed class RoadmapItem
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid CategoryId { get; set; }
    public RoadmapStatus Status { get; set; }
    public int VoteCount { get; set; }
    public DateTime? PlannedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Status of a roadmap item.
/// </summary>
public enum RoadmapStatus
{
    UnderReview,
    Planned,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Represents a user vote on a roadmap item.
/// </summary>
public sealed class Vote
{
    public Guid Id { get; set; }
    public Guid RoadmapItemId { get; set; }
    public Guid UserId { get; set; }
    public VoteType Type { get; set; }
    public DateTime VotedAt { get; set; }
}

/// <summary>
/// Type of vote.
/// </summary>
public enum VoteType
{
    Upvote,
    Downvote
}

/// <summary>
/// Represents user feedback on a roadmap item.
/// </summary>
public sealed class FeedbackEntry
{
    public Guid Id { get; set; }
    public Guid RoadmapItemId { get; set; }
    public Guid UserId { get; set; }
    public required string Content { get; set; }
    public FeedbackCategory Category { get; set; }
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Category of feedback.
/// </summary>
public enum FeedbackCategory
{
    FeatureRequest,
    ImprovementSuggestion,
    BugReport,
    GeneralComment
}

/// <summary>
/// Represents a category for roadmap items.
/// </summary>
public sealed class RoadmapCategory
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Result of voting operation.
/// </summary>
public sealed class VoteResult
{
    public Guid RoadmapItemId { get; set; }
    public int TotalVotes { get; set; }
    public bool UserHasVoted { get; set; }
    public VoteType? UserVoteType { get; set; }
}

/// <summary>
/// Roadmap statistics.
/// </summary>
public sealed class RoadmapStatistics
{
    public int TotalItems { get; set; }
    public int PlannedItems { get; set; }
    public int InProgressItems { get; set; }
    public int CompletedItems { get; set; }
    public int MostVotedItemId { get; set; }
    public Dictionary<RoadmapStatus, int> ItemsByStatus { get; set; } = new();
}

/// <summary>
/// Service interface for product roadmap operations.
/// </summary>
public interface IRoadmapService
{
    /// <summary>
    /// Creates a new roadmap item.
    /// </summary>
    Task<RoadmapItem> CreateItemAsync(
        string title,
        string description,
        Guid categoryId,
        Guid createdBy,
        List<string>? tags = null,
        DateTime? plannedDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a roadmap item.
    /// </summary>
    Task<RoadmapItem> UpdateItemAsync(
        Guid itemId,
        string? title = null,
        string? description = null,
        Guid? categoryId = null,
        RoadmapStatus? status = null,
        DateTime? plannedDate = null,
        DateTime? completedDate = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a roadmap item.
    /// </summary>
    Task DeleteItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a roadmap item by ID.
    /// </summary>
    Task<RoadmapItem?> GetItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roadmap items with optional filtering.
    /// </summary>
    Task<IReadOnlyList<RoadmapItem>> GetItemsAsync(
        RoadmapStatus? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Casts a vote on a roadmap item.
    /// </summary>
    Task<VoteResult> VoteAsync(
        Guid itemId,
        Guid userId,
        VoteType voteType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a vote from a roadmap item.
    /// </summary>
    Task<VoteResult> RemoveVoteAsync(
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets vote result for a roadmap item.
    /// </summary>
    Task<VoteResult> GetVoteResultAsync(
        Guid itemId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits feedback for a roadmap item.
    /// </summary>
    Task<FeedbackEntry> SubmitFeedbackAsync(
        Guid itemId,
        Guid userId,
        string content,
        FeedbackCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feedback for a roadmap item.
    /// </summary>
    Task<IReadOnlyList<FeedbackEntry>> GetFeedbackAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a roadmap category.
    /// </summary>
    Task<RoadmapCategory> CreateCategoryAsync(
        string name,
        string? description = null,
        string? color = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roadmap categories.
    /// </summary>
    Task<IReadOnlyList<RoadmapCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roadmap statistics.
    /// </summary>
    Task<RoadmapStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets most voted items.
    /// </summary>
    Task<IReadOnlyList<RoadmapItem>> GetMostVotedItemsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of product roadmap service with voting and feedback.
/// </summary>
public sealed class RoadmapService : IRoadmapService
{
    private readonly ILogger<RoadmapService> _logger;
    private readonly Dictionary<Guid, RoadmapItem> _items = new();
    private readonly Dictionary<Guid, Vote> _votes = new();
    private readonly Dictionary<Guid, FeedbackEntry> _feedback = new();
    private readonly Dictionary<Guid, RoadmapCategory> _categories = new();

    public RoadmapService(ILogger<RoadmapService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<RoadmapItem> CreateItemAsync(
        string title,
        string description,
        Guid categoryId,
        Guid createdBy,
        List<string>? tags = null,
        DateTime? plannedDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating roadmap item: {Title}", title);

        var item = new RoadmapItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CategoryId = categoryId,
            CreatedBy = createdBy,
            Status = RoadmapStatus.UnderReview,
            PlannedDate = plannedDate,
            Tags = tags ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        _items[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<RoadmapItem> UpdateItemAsync(
        Guid itemId,
        string? title = null,
        string? description = null,
        Guid? categoryId = null,
        RoadmapStatus? status = null,
        DateTime? plannedDate = null,
        DateTime? completedDate = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(itemId, out var item))
        {
            throw new InvalidOperationException($"Roadmap item {itemId} not found");
        }

        if (title != null) item.Title = title;
        if (description != null) item.Description = description;
        if (categoryId.HasValue) item.CategoryId = categoryId.Value;
        if (status.HasValue)
        {
            item.Status = status.Value;
            if (status.Value == RoadmapStatus.Completed)
            {
                item.CompletedDate = DateTime.UtcNow;
            }
        }
        if (plannedDate.HasValue) item.PlannedDate = plannedDate;
        if (completedDate.HasValue) item.CompletedDate = completedDate;
        if (tags != null) item.Tags = tags;

        item.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Updated roadmap item: {ItemId}", itemId);
        return Task.FromResult(item);
    }

    public Task DeleteItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        _items.Remove(itemId);

        var votesToRemove = _votes.Where(v => v.Value.RoadmapItemId == itemId).Select(v => v.Key).ToList();
        foreach (var voteId in votesToRemove)
        {
            _votes.Remove(voteId);
        }

        var feedbackToRemove = _feedback.Where(f => f.Value.RoadmapItemId == itemId).Select(f => f.Key).ToList();
        foreach (var feedbackId in feedbackToRemove)
        {
            _feedback.Remove(feedbackId);
        }

        _logger.LogInformation("Deleted roadmap item: {ItemId}", itemId);
        return Task.CompletedTask;
    }

    public Task<RoadmapItem?> GetItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(itemId, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<RoadmapItem>> GetItemsAsync(
        RoadmapStatus? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var items = _items.Values.AsEnumerable();

        if (status.HasValue)
        {
            items = items.Where(i => i.Status == status);
        }

        if (categoryId.HasValue)
        {
            items = items.Where(i => i.CategoryId == categoryId);
        }

        var result = items
            .OrderByDescending(i => i.VoteCount)
            .ThenBy(i => i.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<RoadmapItem>>(result);
    }

    public Task<VoteResult> VoteAsync(
        Guid itemId,
        Guid userId,
        VoteType voteType,
        CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(itemId, out var item))
        {
            throw new InvalidOperationException($"Roadmap item {itemId} not found");
        }

        var existingVote = _votes.Values.FirstOrDefault(v => v.RoadmapItemId == itemId && v.UserId == userId);
        if (existingVote != null)
        {
            if (existingVote.Type == VoteType.Upvote)
                item.VoteCount--;
            else
                item.VoteCount++;

            _votes.Remove(existingVote.Id);
        }

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            RoadmapItemId = itemId,
            UserId = userId,
            Type = voteType,
            VotedAt = DateTime.UtcNow
        };

        _votes[vote.Id] = vote;

        if (voteType == VoteType.Upvote)
            item.VoteCount++;
        else
            item.VoteCount--;

        _logger.LogInformation("User {UserId} voted {VoteType} on roadmap item {ItemId}", userId, voteType, itemId);

        return Task.FromResult(new VoteResult
        {
            RoadmapItemId = itemId,
            TotalVotes = item.VoteCount,
            UserHasVoted = true,
            UserVoteType = voteType
        });
    }

    public Task<VoteResult> RemoveVoteAsync(
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(itemId, out var item))
        {
            throw new InvalidOperationException($"Roadmap item {itemId} not found");
        }

        var vote = _votes.Values.FirstOrDefault(v => v.RoadmapItemId == itemId && v.UserId == userId);
        if (vote != null)
        {
            if (vote.Type == VoteType.Upvote)
                item.VoteCount--;
            else
                item.VoteCount++;

            _votes.Remove(vote.Id);
            _logger.LogInformation("User {UserId} removed vote from roadmap item {ItemId}", userId, itemId);
        }

        return Task.FromResult(new VoteResult
        {
            RoadmapItemId = itemId,
            TotalVotes = item.VoteCount,
            UserHasVoted = false,
            UserVoteType = null
        });
    }

    public Task<VoteResult> GetVoteResultAsync(
        Guid itemId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(itemId, out var item))
        {
            throw new InvalidOperationException($"Roadmap item {itemId} not found");
        }

        Vote? userVote = null;
        if (userId.HasValue)
        {
            userVote = _votes.Values.FirstOrDefault(v => v.RoadmapItemId == itemId && v.UserId == userId);
        }

        return Task.FromResult(new VoteResult
        {
            RoadmapItemId = itemId,
            TotalVotes = item.VoteCount,
            UserHasVoted = userVote != null,
            UserVoteType = userVote?.Type
        });
    }

    public Task<FeedbackEntry> SubmitFeedbackAsync(
        Guid itemId,
        Guid userId,
        string content,
        FeedbackCategory category,
        CancellationToken cancellationToken = default)
    {
        if (!_items.ContainsKey(itemId))
        {
            throw new InvalidOperationException($"Roadmap item {itemId} not found");
        }

        var feedback = new FeedbackEntry
        {
            Id = Guid.NewGuid(),
            RoadmapItemId = itemId,
            UserId = userId,
            Content = content,
            Category = category,
            SubmittedAt = DateTime.UtcNow
        };

        _feedback[feedback.Id] = feedback;
        _logger.LogInformation("User {UserId} submitted feedback for roadmap item {ItemId}", userId, itemId);

        return Task.FromResult(feedback);
    }

    public Task<IReadOnlyList<FeedbackEntry>> GetFeedbackAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var feedback = _feedback.Values
            .Where(f => f.RoadmapItemId == itemId)
            .OrderByDescending(f => f.SubmittedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<FeedbackEntry>>(feedback);
    }

    public Task<RoadmapCategory> CreateCategoryAsync(
        string name,
        string? description = null,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating roadmap category: {Name}", name);

        var category = new RoadmapCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Color = color,
            Order = _categories.Count
        };

        _categories[category.Id] = category;
        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<RoadmapCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = _categories.Values
            .OrderBy(c => c.Order)
            .ToList();

        return Task.FromResult<IReadOnlyList<RoadmapCategory>>(categories);
    }

    public Task<RoadmapStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var itemsByStatus = _items.Values
            .GroupBy(i => i.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var mostVotedItem = _items.Values
            .OrderByDescending(i => i.VoteCount)
            .FirstOrDefault();

        var stats = new RoadmapStatistics
        {
            TotalItems = _items.Count,
            PlannedItems = itemsByStatus.GetValueOrDefault(RoadmapStatus.Planned),
            InProgressItems = itemsByStatus.GetValueOrDefault(RoadmapStatus.InProgress),
            CompletedItems = itemsByStatus.GetValueOrDefault(RoadmapStatus.Completed),
            ItemsByStatus = itemsByStatus
        };

        return Task.FromResult(stats);
    }

    public Task<IReadOnlyList<RoadmapItem>> GetMostVotedItemsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var items = _items.Values
            .OrderByDescending(i => i.VoteCount)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<RoadmapItem>>(items);
    }
}
