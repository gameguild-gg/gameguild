namespace GameGuild.Modules.Api.Changelog;

/// <summary>
/// Represents an API version.
/// </summary>
public sealed class ApiVersion
{
    public Guid Id { get; set; }
    public required string VersionNumber { get; set; }
    public DateTime ReleaseDate { get; set; }
    public VersionStatus Status { get; set; }
    public DateTime? DeprecationDate { get; set; }
    public DateTime? SunsetDate { get; set; }
    public List<ChangelogEntry> Changes { get; set; } = new();
    public string? ReleaseNotes { get; set; }
}

/// <summary>
/// Status of an API version.
/// </summary>
public enum VersionStatus
{
    InDevelopment,
    Released,
    Deprecated,
    Sunset
}

/// <summary>
/// Represents a changelog entry.
/// </summary>
public sealed class ChangelogEntry
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public ChangeType Type { get; set; }
    public ChangeSeverity Severity { get; set; }
    public string? EndpointPath { get; set; }
    public string? Component { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Type of API change.
/// </summary>
public enum ChangeType
{
    Addition,
    Modification,
    Deprecation,
    Removal,
    BugFix,
    SecurityFix
}

/// <summary>
/// Severity of a change.
/// </summary>
public enum ChangeSeverity
{
    Patch,
    Minor,
    Major,
    Breaking
}

/// <summary>
/// Represents a breaking change.
/// </summary>
public sealed class BreakingChange
{
    public Guid Id { get; set; }
    public Guid ChangelogEntryId { get; set; }
    public required string ImpactDescription { get; set; }
    public required string MigrationPath { get; set; }
    public DateTime EffectiveDate { get; set; }
    public List<string> AffectedEndpoints { get; set; } = new();
}

/// <summary>
/// Represents a migration guide.
/// </summary>
public sealed class MigrationGuide
{
    public Guid Id { get; set; }
    public required string FromVersion { get; set; }
    public required string ToVersion { get; set; }
    public required string Content { get; set; }
    public ContentFormat Format { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MigrationStep> Steps { get; set; } = new();
}

/// <summary>
/// Content format for documentation.
/// </summary>
public enum ContentFormat
{
    Markdown,
    HTML,
    PlainText
}

/// <summary>
/// Represents a migration step.
/// </summary>
public sealed class MigrationStep
{
    public int Order { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? CodeExample { get; set; }
    public bool IsRequired { get; set; }
}

/// <summary>
/// Result of changelog generation.
/// </summary>
public sealed class ChangelogGenerationResult
{
    public required string Content { get; set; }
    public ContentFormat Format { get; set; }
    public int TotalChanges { get; set; }
    public Dictionary<ChangeType, int> ChangesByType { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Version comparison result.
/// </summary>
public sealed class VersionComparison
{
    public required string FromVersion { get; set; }
    public required string ToVersion { get; set; }
    public List<ChangelogEntry> Changes { get; set; } = new();
    public List<BreakingChange> BreakingChanges { get; set; } = new();
    public bool HasBreakingChanges { get; set; }
}

/// <summary>
/// Service interface for API changelog operations.
/// </summary>
public interface IApiChangelogService
{
    /// <summary>
    /// Creates a new API version.
    /// </summary>
    Task<ApiVersion> CreateVersionAsync(
        string versionNumber,
        DateTime releaseDate,
        string? releaseNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an API version.
    /// </summary>
    Task<ApiVersion> UpdateVersionAsync(
        Guid versionId,
        string? versionNumber = null,
        DateTime? releaseDate = null,
        VersionStatus? status = null,
        string? releaseNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a changelog entry to a version.
    /// </summary>
    Task<ChangelogEntry> AddChangeAsync(
        Guid versionId,
        string title,
        string description,
        ChangeType type,
        ChangeSeverity severity,
        string? endpointPath = null,
        string? component = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a change as breaking and adds migration details.
    /// </summary>
    Task<BreakingChange> MarkAsBreakingChangeAsync(
        Guid changelogEntryId,
        string impactDescription,
        string migrationPath,
        DateTime effectiveDate,
        List<string> affectedEndpoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprecates an API version.
    /// </summary>
    Task<ApiVersion> DeprecateVersionAsync(
        Guid versionId,
        DateTime deprecationDate,
        DateTime? sunsetDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API version by version number.
    /// </summary>
    Task<ApiVersion?> GetVersionAsync(
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API versions.
    /// </summary>
    Task<IReadOnlyList<ApiVersion>> GetVersionsAsync(
        VersionStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a migration guide.
    /// </summary>
    Task<MigrationGuide> CreateMigrationGuideAsync(
        string fromVersion,
        string toVersion,
        string content,
        ContentFormat format,
        List<MigrationStep>? steps = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a migration guide between two versions.
    /// </summary>
    Task<MigrationGuide?> GetMigrationGuideAsync(
        string fromVersion,
        string toVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates changelog in specified format.
    /// </summary>
    Task<ChangelogGenerationResult> GenerateChangelogAsync(
        Guid versionId,
        ContentFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two versions and returns the differences.
    /// </summary>
    Task<VersionComparison> CompareVersionsAsync(
        string fromVersion,
        string toVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all breaking changes for a version.
    /// </summary>
    Task<IReadOnlyList<BreakingChange>> GetBreakingChangesAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of API changelog service with version tracking and documentation.
/// </summary>
public sealed class ApiChangelogService : IApiChangelogService
{
    private readonly ILogger<ApiChangelogService> _logger;
    private readonly Dictionary<Guid, ApiVersion> _versions = new();
    private readonly Dictionary<Guid, BreakingChange> _breakingChanges = new();
    private readonly Dictionary<string, MigrationGuide> _migrationGuides = new();

    public ApiChangelogService(ILogger<ApiChangelogService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ApiVersion> CreateVersionAsync(
        string versionNumber,
        DateTime releaseDate,
        string? releaseNotes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating API version: {Version}", versionNumber);

        var version = new ApiVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = versionNumber,
            ReleaseDate = releaseDate,
            Status = releaseDate <= DateTime.UtcNow ? VersionStatus.Released : VersionStatus.InDevelopment,
            ReleaseNotes = releaseNotes
        };

        _versions[version.Id] = version;
        return Task.FromResult(version);
    }

    public Task<ApiVersion> UpdateVersionAsync(
        Guid versionId,
        string? versionNumber = null,
        DateTime? releaseDate = null,
        VersionStatus? status = null,
        string? releaseNotes = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        if (versionNumber != null) version.VersionNumber = versionNumber;
        if (releaseDate.HasValue) version.ReleaseDate = releaseDate.Value;
        if (status.HasValue) version.Status = status.Value;
        if (releaseNotes != null) version.ReleaseNotes = releaseNotes;

        _logger.LogInformation("Updated API version: {VersionId}", versionId);
        return Task.FromResult(version);
    }

    public Task<ChangelogEntry> AddChangeAsync(
        Guid versionId,
        string title,
        string description,
        ChangeType type,
        ChangeSeverity severity,
        string? endpointPath = null,
        string? component = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        var entry = new ChangelogEntry
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            Title = title,
            Description = description,
            Type = type,
            Severity = severity,
            EndpointPath = endpointPath,
            Component = component,
            Tags = tags ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        version.Changes.Add(entry);
        _logger.LogInformation("Added changelog entry to version {VersionId}: {Title}", versionId, title);

        return Task.FromResult(entry);
    }

    public Task<BreakingChange> MarkAsBreakingChangeAsync(
        Guid changelogEntryId,
        string impactDescription,
        string migrationPath,
        DateTime effectiveDate,
        List<string> affectedEndpoints,
        CancellationToken cancellationToken = default)
    {
        var entry = _versions.Values
            .SelectMany(v => v.Changes)
            .FirstOrDefault(c => c.Id == changelogEntryId);

        if (entry == null)
        {
            throw new InvalidOperationException($"Changelog entry {changelogEntryId} not found");
        }

        entry.Severity = ChangeSeverity.Breaking;

        var breakingChange = new BreakingChange
        {
            Id = Guid.NewGuid(),
            ChangelogEntryId = changelogEntryId,
            ImpactDescription = impactDescription,
            MigrationPath = migrationPath,
            EffectiveDate = effectiveDate,
            AffectedEndpoints = affectedEndpoints
        };

        _breakingChanges[breakingChange.Id] = breakingChange;
        _logger.LogInformation("Marked changelog entry {EntryId} as breaking change", changelogEntryId);

        return Task.FromResult(breakingChange);
    }

    public Task<ApiVersion> DeprecateVersionAsync(
        Guid versionId,
        DateTime deprecationDate,
        DateTime? sunsetDate = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        version.Status = VersionStatus.Deprecated;
        version.DeprecationDate = deprecationDate;
        version.SunsetDate = sunsetDate;

        _logger.LogInformation("Deprecated API version {VersionId}", versionId);
        return Task.FromResult(version);
    }

    public Task<ApiVersion?> GetVersionAsync(
        string versionNumber,
        CancellationToken cancellationToken = default)
    {
        var version = _versions.Values.FirstOrDefault(v => v.VersionNumber == versionNumber);
        return Task.FromResult(version);
    }

    public Task<IReadOnlyList<ApiVersion>> GetVersionsAsync(
        VersionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var versions = _versions.Values.AsEnumerable();

        if (status.HasValue)
        {
            versions = versions.Where(v => v.Status == status);
        }

        var result = versions
            .OrderByDescending(v => v.ReleaseDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<ApiVersion>>(result);
    }

    public Task<MigrationGuide> CreateMigrationGuideAsync(
        string fromVersion,
        string toVersion,
        string content,
        ContentFormat format,
        List<MigrationStep>? steps = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating migration guide from {From} to {To}", fromVersion, toVersion);

        var guide = new MigrationGuide
        {
            Id = Guid.NewGuid(),
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Content = content,
            Format = format,
            Steps = steps ?? new List<MigrationStep>(),
            CreatedAt = DateTime.UtcNow
        };

        var key = $"{fromVersion}_{toVersion}";
        _migrationGuides[key] = guide;

        return Task.FromResult(guide);
    }

    public Task<MigrationGuide?> GetMigrationGuideAsync(
        string fromVersion,
        string toVersion,
        CancellationToken cancellationToken = default)
    {
        var key = $"{fromVersion}_{toVersion}";
        _migrationGuides.TryGetValue(key, out var guide);
        return Task.FromResult(guide);
    }

    public Task<ChangelogGenerationResult> GenerateChangelogAsync(
        Guid versionId,
        ContentFormat format,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        var changesByType = version.Changes
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var content = format switch
        {
            ContentFormat.Markdown => GenerateMarkdownChangelog(version),
            ContentFormat.HTML => GenerateHtmlChangelog(version),
            _ => GeneratePlainTextChangelog(version)
        };

        var result = new ChangelogGenerationResult
        {
            Content = content,
            Format = format,
            TotalChanges = version.Changes.Count,
            ChangesByType = changesByType,
            GeneratedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    public Task<VersionComparison> CompareVersionsAsync(
        string fromVersion,
        string toVersion,
        CancellationToken cancellationToken = default)
    {
        var from = _versions.Values.FirstOrDefault(v => v.VersionNumber == fromVersion);
        var to = _versions.Values.FirstOrDefault(v => v.VersionNumber == toVersion);

        if (from == null || to == null)
        {
            throw new InvalidOperationException("One or both versions not found");
        }

        var changes = to.Changes.Where(c => c.CreatedAt > from.ReleaseDate).ToList();
        var breakingChangeIds = changes.Where(c => c.Severity == ChangeSeverity.Breaking).Select(c => c.Id).ToList();
        var breakingChanges = _breakingChanges.Values.Where(bc => breakingChangeIds.Contains(bc.ChangelogEntryId)).ToList();

        var comparison = new VersionComparison
        {
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Changes = changes,
            BreakingChanges = breakingChanges,
            HasBreakingChanges = breakingChanges.Any()
        };

        return Task.FromResult(comparison);
    }

    public Task<IReadOnlyList<BreakingChange>> GetBreakingChangesAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        var changeIds = version.Changes.Select(c => c.Id).ToList();
        var breakingChanges = _breakingChanges.Values
            .Where(bc => changeIds.Contains(bc.ChangelogEntryId))
            .ToList();

        return Task.FromResult<IReadOnlyList<BreakingChange>>(breakingChanges);
    }

    private string GenerateMarkdownChangelog(ApiVersion version)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Version {version.VersionNumber}");
        sb.AppendLine($"Released: {version.ReleaseDate:yyyy-MM-dd}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(version.ReleaseNotes))
        {
            sb.AppendLine(version.ReleaseNotes);
            sb.AppendLine();
        }

        foreach (var group in version.Changes.GroupBy(c => c.Type))
        {
            sb.AppendLine($"## {group.Key}");
            foreach (var change in group)
            {
                sb.AppendLine($"- **{change.Title}**: {change.Description}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateHtmlChangelog(ApiVersion version)
    {
        return $"<h1>Version {version.VersionNumber}</h1><p>Released: {version.ReleaseDate:yyyy-MM-dd}</p>";
    }

    private string GeneratePlainTextChangelog(ApiVersion version)
    {
        return $"Version {version.VersionNumber}\nReleased: {version.ReleaseDate:yyyy-MM-dd}\n\n";
    }
}
