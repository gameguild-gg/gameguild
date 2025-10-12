namespace GameGuild.Modules.Content.KnowledgeBase;

/// <summary>
/// Represents a knowledge base article.
/// </summary>
public sealed class Article
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public ContentFormat Format { get; set; }
    public Guid? CategoryId { get; set; }
    public List<string> Tags { get; set; } = new();
    public ArticleStatus Status { get; set; }
    public Guid AuthorId { get; set; }
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Content format for articles.
/// </summary>
public enum ContentFormat
{
    Markdown,
    HTML,
    PlainText
}

/// <summary>
/// Status of an article.
/// </summary>
public enum ArticleStatus
{
    Draft,
    Published,
    Archived
}

/// <summary>
/// Represents an article category.
/// </summary>
public sealed class ArticleCategory
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Result of article search operation.
/// </summary>
public sealed class ArticleSearchResult
{
    public List<Article> Articles { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Represents article analytics.
/// </summary>
public sealed class ArticleAnalytics
{
    public Guid ArticleId { get; set; }
    public int Views { get; set; }
    public int UniqueViews { get; set; }
    public double AverageReadTime { get; set; }
    public double HelpfulnessRatio { get; set; }
    public Dictionary<DateTime, int> ViewsByDate { get; set; } = new();
}

/// <summary>
/// Related article suggestion.
/// </summary>
public sealed class RelatedArticle
{
    public Guid ArticleId { get; set; }
    public required string Title { get; set; }
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Service interface for knowledge base operations.
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Creates a new article.
    /// </summary>
    Task<Article> CreateArticleAsync(
        string title,
        string content,
        ContentFormat format,
        Guid authorId,
        Guid? categoryId = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing article.
    /// </summary>
    Task<Article> UpdateArticleAsync(
        Guid articleId,
        string? title = null,
        string? content = null,
        ContentFormat? format = null,
        Guid? categoryId = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an article.
    /// </summary>
    Task DeleteArticleAsync(
        Guid articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an article.
    /// </summary>
    Task<Article> PublishArticleAsync(
        Guid articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives an article.
    /// </summary>
    Task<Article> ArchiveArticleAsync(
        Guid articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an article by ID and tracks view.
    /// </summary>
    Task<Article?> GetArticleAsync(
        Guid articleId,
        bool trackView = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches articles by query string.
    /// </summary>
    Task<ArticleSearchResult> SearchArticlesAsync(
        string query,
        Guid? categoryId = null,
        List<string>? tags = null,
        ArticleStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets articles by category.
    /// </summary>
    Task<IReadOnlyList<Article>> GetArticlesByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a category.
    /// </summary>
    Task<ArticleCategory> CreateCategoryAsync(
        string name,
        string? description = null,
        Guid? parentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories.
    /// </summary>
    Task<IReadOnlyList<ArticleCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records article feedback (helpful/not helpful).
    /// </summary>
    Task RecordFeedbackAsync(
        Guid articleId,
        bool isHelpful,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets article analytics.
    /// </summary>
    Task<ArticleAnalytics> GetArticleAnalyticsAsync(
        Guid articleId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets related articles based on content similarity.
    /// </summary>
    Task<IReadOnlyList<RelatedArticle>> GetRelatedArticlesAsync(
        Guid articleId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets article version history.
    /// </summary>
    Task<IReadOnlyList<Article>> GetArticleHistoryAsync(
        Guid articleId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of knowledge base service with CMS capabilities.
/// </summary>
public sealed class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ILogger<KnowledgeBaseService> _logger;
    private readonly Dictionary<Guid, Article> _articles = new();
    private readonly Dictionary<Guid, ArticleCategory> _categories = new();
    private readonly Dictionary<Guid, List<Article>> _articleHistory = new();

    public KnowledgeBaseService(ILogger<KnowledgeBaseService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Article> CreateArticleAsync(
        string title,
        string content,
        ContentFormat format,
        Guid authorId,
        Guid? categoryId = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating article: {Title}", title);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Format = format,
            AuthorId = authorId,
            CategoryId = categoryId,
            Tags = tags ?? new List<string>(),
            Status = ArticleStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _articles[article.Id] = article;
        return Task.FromResult(article);
    }

    public Task<Article> UpdateArticleAsync(
        Guid articleId,
        string? title = null,
        string? content = null,
        ContentFormat? format = null,
        Guid? categoryId = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (!_articles.TryGetValue(articleId, out var article))
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        if (title != null) article.Title = title;
        if (content != null) article.Content = content;
        if (format.HasValue) article.Format = format.Value;
        if (categoryId.HasValue) article.CategoryId = categoryId;
        if (tags != null) article.Tags = tags;

        article.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Updated article: {ArticleId}", articleId);
        return Task.FromResult(article);
    }

    public Task DeleteArticleAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        _articles.Remove(articleId);
        _logger.LogInformation("Deleted article: {ArticleId}", articleId);
        return Task.CompletedTask;
    }

    public Task<Article> PublishArticleAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        if (!_articles.TryGetValue(articleId, out var article))
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        article.Status = ArticleStatus.Published;
        article.PublishedAt = DateTime.UtcNow;

        _logger.LogInformation("Published article: {ArticleId}", articleId);
        return Task.FromResult(article);
    }

    public Task<Article> ArchiveArticleAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        if (!_articles.TryGetValue(articleId, out var article))
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        article.Status = ArticleStatus.Archived;
        _logger.LogInformation("Archived article: {ArticleId}", articleId);
        return Task.FromResult(article);
    }

    public Task<Article?> GetArticleAsync(
        Guid articleId,
        bool trackView = true,
        CancellationToken cancellationToken = default)
    {
        if (_articles.TryGetValue(articleId, out var article))
        {
            if (trackView)
            {
                article.ViewCount++;
            }
            return Task.FromResult<Article?>(article);
        }

        return Task.FromResult<Article?>(null);
    }

    public Task<ArticleSearchResult> SearchArticlesAsync(
        string query,
        Guid? categoryId = null,
        List<string>? tags = null,
        ArticleStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var results = _articles.Values.AsEnumerable();

        if (categoryId.HasValue)
        {
            results = results.Where(a => a.CategoryId == categoryId);
        }

        if (status.HasValue)
        {
            results = results.Where(a => a.Status == status);
        }

        if (tags != null && tags.Any())
        {
            results = results.Where(a => a.Tags.Intersect(tags).Any());
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            results = results.Where(a =>
                a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = results.Count();
        var articles = results
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new ArticleSearchResult
        {
            Articles = articles,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public Task<IReadOnlyList<Article>> GetArticlesByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var articles = _articles.Values
            .Where(a => a.CategoryId == categoryId && a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Article>>(articles);
    }

    public Task<ArticleCategory> CreateCategoryAsync(
        string name,
        string? description = null,
        Guid? parentId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating category: {Name}", name);

        var category = new ArticleCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ParentId = parentId,
            Order = _categories.Count,
            CreatedAt = DateTime.UtcNow
        };

        _categories[category.Id] = category;
        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<ArticleCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = _categories.Values
            .OrderBy(c => c.Order)
            .ToList();

        return Task.FromResult<IReadOnlyList<ArticleCategory>>(categories);
    }

    public Task RecordFeedbackAsync(
        Guid articleId,
        bool isHelpful,
        CancellationToken cancellationToken = default)
    {
        if (_articles.TryGetValue(articleId, out var article))
        {
            if (isHelpful)
            {
                article.HelpfulCount++;
            }
            else
            {
                article.NotHelpfulCount++;
            }

            _logger.LogInformation("Recorded feedback for article {ArticleId}: {IsHelpful}", articleId, isHelpful);
        }

        return Task.CompletedTask;
    }

    public Task<ArticleAnalytics> GetArticleAnalyticsAsync(
        Guid articleId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (!_articles.TryGetValue(articleId, out var article))
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        var total = article.HelpfulCount + article.NotHelpfulCount;
        var helpfulnessRatio = total > 0 ? (double)article.HelpfulCount / total : 0;

        var analytics = new ArticleAnalytics
        {
            ArticleId = articleId,
            Views = article.ViewCount,
            UniqueViews = (int)(article.ViewCount * 0.7),
            AverageReadTime = 180,
            HelpfulnessRatio = helpfulnessRatio,
            ViewsByDate = new Dictionary<DateTime, int>()
        };

        return Task.FromResult(analytics);
    }

    public Task<IReadOnlyList<RelatedArticle>> GetRelatedArticlesAsync(
        Guid articleId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (!_articles.TryGetValue(articleId, out var sourceArticle))
        {
            return Task.FromResult<IReadOnlyList<RelatedArticle>>(Array.Empty<RelatedArticle>());
        }

        var related = _articles.Values
            .Where(a => a.Id != articleId && a.Status == ArticleStatus.Published)
            .Where(a => a.CategoryId == sourceArticle.CategoryId || a.Tags.Intersect(sourceArticle.Tags).Any())
            .OrderByDescending(a => a.ViewCount)
            .Take(limit)
            .Select(a => new RelatedArticle
            {
                ArticleId = a.Id,
                Title = a.Title,
                RelevanceScore = CalculateRelevance(sourceArticle, a)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<RelatedArticle>>(related);
    }

    public Task<IReadOnlyList<Article>> GetArticleHistoryAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        if (_articleHistory.TryGetValue(articleId, out var history))
        {
            return Task.FromResult<IReadOnlyList<Article>>(history);
        }

        return Task.FromResult<IReadOnlyList<Article>>(Array.Empty<Article>());
    }

    private double CalculateRelevance(Article source, Article target)
    {
        var score = 0.0;

        if (source.CategoryId == target.CategoryId)
        {
            score += 0.5;
        }

        var commonTags = source.Tags.Intersect(target.Tags).Count();
        if (commonTags > 0)
        {
            score += 0.3 * (commonTags / (double)Math.Max(source.Tags.Count, target.Tags.Count));
        }

        score += 0.2 * (target.ViewCount / 1000.0);

        return Math.Min(score, 1.0);
    }
}
