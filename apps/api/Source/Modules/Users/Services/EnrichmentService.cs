using System.Text.Json;
using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Users.Services;

/// <summary>
/// Service interface for behavioral analytics and profile enrichment.
/// </summary>
public interface IEnrichmentService {
    /// <summary>
    /// Ingests a user behavior event.
    /// </summary>
    Task<Result<UserBehaviorEvent>> IngestEventAsync(Guid userId, string eventType, Dictionary<string, object> properties, string? sessionId = null, string? source = null);

    /// <summary>
    /// Ingests multiple events in batch.
    /// </summary>
    Task<Result<int>> BatchIngestEventsAsync(List<UserBehaviorEvent> events);

    /// <summary>
    /// Extracts profile attributes from user behavior events.
    /// </summary>
    Task<Result<List<ProfileAttribute>>> ExtractAttributesAsync(Guid userId);

    /// <summary>
    /// Gets all profile attributes for a user.
    /// </summary>
    Task<List<ProfileAttribute>> GetUserAttributesAsync(Guid userId, bool activeOnly = true);

    /// <summary>
    /// Updates a profile attribute.
    /// </summary>
    Task<Result> UpdateAttributeAsync(Guid attributeId, string newValue, double confidence, string? metadata = null);

    /// <summary>
    /// Deletes a profile attribute.
    /// </summary>
    Task<Result> DeleteAttributeAsync(Guid attributeId);

    /// <summary>
    /// Gets event history for a user.
    /// </summary>
    Task<List<UserBehaviorEvent>> GetEventHistoryAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);

    /// <summary>
    /// Gets attributes by source.
    /// </summary>
    Task<List<ProfileAttribute>> GetAttributesBySourceAsync(Guid userId, string source);

    /// <summary>
    /// Calculates attribute confidence based on supporting data.
    /// </summary>
    Task<double> CalculateAttributeConfidenceAsync(Guid userId, string attributeKey);

    /// <summary>
    /// Purges expired events (background job).
    /// </summary>
    Task<Result<int>> PurgeExpiredEventsAsync();
}

/// <summary>
/// Service implementation for behavioral analytics enrichment.
/// </summary>
public sealed class EnrichmentService : IEnrichmentService {
    private readonly IRepository<UserBehaviorEvent> _eventRepository;
    private readonly IRepository<ProfileAttribute> _attributeRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<EnrichmentService> _logger;

    public EnrichmentService(
        IRepository<UserBehaviorEvent> eventRepository,
        IRepository<ProfileAttribute> attributeRepository,
        IRepository<User> userRepository,
        ILogger<EnrichmentService> logger) {
        _eventRepository = eventRepository;
        _attributeRepository = attributeRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserBehaviorEvent>> IngestEventAsync(
        Guid userId,
        string eventType,
        Dictionary<string, object> properties,
        string? sessionId = null,
        string? source = null) {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) {
            return Result<UserBehaviorEvent>.Failure("User not found");
        }

        var behaviorEvent = new UserBehaviorEvent {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Properties = JsonSerializer.Serialize(properties),
            SessionId = sessionId,
            Source = source ?? "Unknown",
            ExpiresAt = DateTime.UtcNow.AddDays(90), // 90-day retention
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(behaviorEvent);

        _logger.LogInformation(
            "Ingested event {EventType} for user {UserId} in session {SessionId}",
            eventType, userId, sessionId);

        return Result<UserBehaviorEvent>.Success(behaviorEvent);
    }

    public async Task<Result<int>> BatchIngestEventsAsync(List<UserBehaviorEvent> events) {
        if (!events.Any()) {
            return Result<int>.Success(0);
        }

        foreach (var behaviorEvent in events) {
            if (behaviorEvent.Id == Guid.Empty) {
                behaviorEvent.Id = Guid.NewGuid();
            }
            behaviorEvent.CreatedAt = DateTime.UtcNow;
            behaviorEvent.UpdatedAt = DateTime.UtcNow;

            if (!behaviorEvent.ExpiresAt.HasValue) {
                behaviorEvent.ExpiresAt = DateTime.UtcNow.AddDays(90);
            }
        }

        await _eventRepository.AddRangeAsync(events);

        _logger.LogInformation("Batch ingested {Count} events", events.Count);

        return Result<int>.Success(events.Count);
    }

    public async Task<Result<List<ProfileAttribute>>> ExtractAttributesAsync(Guid userId) {
        // Get recent events for the user
        var events = await _eventRepository.FindAsync(
            e => e.UserId == userId && !e.IsProcessed);

        if (!events.Any()) {
            _logger.LogInformation("No unprocessed events for user {UserId}", userId);
            return Result<List<ProfileAttribute>>.Success(new List<ProfileAttribute>());
        }

        var extractedAttributes = new List<ProfileAttribute>();

        // Extract activity level
        var activityLevel = CalculateActivityLevel(events.ToList());
        extractedAttributes.Add(await CreateOrUpdateAttributeAsync(
            userId, "ActivityLevel", activityLevel, "BehaviorAnalysis", 0.85));

        // Extract preferred time of day
        var preferredTime = CalculatePreferredTimeOfDay(events.ToList());
        extractedAttributes.Add(await CreateOrUpdateAttributeAsync(
            userId, "PreferredTimeOfDay", preferredTime, "BehaviorAnalysis", 0.75));        // Extract most viewed category (if page views exist)
        var pageViews = events.Where(e => e.EventType == "PageView").ToList();
        if (pageViews.Any()) {
            var mostViewedCategory = ExtractMostViewedCategory(pageViews);
            if (!string.IsNullOrEmpty(mostViewedCategory)) {
                extractedAttributes.Add(await CreateOrUpdateAttributeAsync(
                    userId, "PreferredCategory", mostViewedCategory, "BehaviorAnalysis", 0.70));
            }
        }

        // Mark events as processed
        foreach (var behaviorEvent in events) {
            behaviorEvent.IsProcessed = true;
            behaviorEvent.ProcessedAt = DateTime.UtcNow;
            await _eventRepository.UpdateAsync(behaviorEvent);
        }

        _logger.LogInformation(
            "Extracted {Count} attributes from {EventCount} events for user {UserId}",
            extractedAttributes.Count, events.Count, userId);

        return Result<List<ProfileAttribute>>.Success(extractedAttributes);
    }

    public async Task<List<ProfileAttribute>> GetUserAttributesAsync(Guid userId, bool activeOnly = true) {
        if (activeOnly) {
            return await _attributeRepository.FindAsync(
                a => a.UserId == userId && (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow));
        }

        return await _attributeRepository.FindAsync(a => a.UserId == userId);
    }

    public async Task<Result> UpdateAttributeAsync(
        Guid attributeId,
        string newValue,
        double confidence,
        string? metadata = null) {
        var attribute = await _attributeRepository.GetByIdAsync(attributeId);
        if (attribute == null) {
            return Result.Failure("Attribute not found");
        }

        attribute.Update(newValue, confidence, metadata);
        await _attributeRepository.UpdateAsync(attribute);

        _logger.LogInformation(
            "Updated attribute {AttributeId} for user {UserId}: {Key} = {Value}",
            attributeId, attribute.UserId, attribute.AttributeKey, newValue);

        return Result.Success();
    }

    public async Task<Result> DeleteAttributeAsync(Guid attributeId) {
        var attribute = await _attributeRepository.GetByIdAsync(attributeId);
        if (attribute == null) {
            return Result.Failure("Attribute not found");
        }

        await _attributeRepository.DeleteAsync(attribute);

        _logger.LogInformation(
            "Deleted attribute {AttributeId} for user {UserId}",
            attributeId, attribute.UserId);

        return Result.Success();
    }

    public async Task<List<UserBehaviorEvent>> GetEventHistoryAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100) {
        var events = await _eventRepository.FindAsync(e => e.UserId == userId);

        if (startDate.HasValue) {
            events = events.Where(e => e.Timestamp >= startDate.Value).ToList();
        }

        if (endDate.HasValue) {
            events = events.Where(e => e.Timestamp <= endDate.Value).ToList();
        }

        return events
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList();
    }

    public async Task<List<ProfileAttribute>> GetAttributesBySourceAsync(Guid userId, string source) {
        return await _attributeRepository.FindAsync(
            a => a.UserId == userId && a.Source == source);
    }

    public async Task<double> CalculateAttributeConfidenceAsync(Guid userId, string attributeKey) {
        var attribute = await _attributeRepository.FindAsync(
            a => a.UserId == userId && a.AttributeKey == attributeKey);

        if (!attribute.Any()) {
            return 0.0;
        }

        var latest = attribute.OrderByDescending(a => a.UpdatedAt).First();

        // Confidence degrades over time
        var daysSinceUpdate = (DateTime.UtcNow - latest.UpdatedAt).TotalDays;
        var timeFactor = Math.Max(0, 1.0 - (daysSinceUpdate / 90.0)); // Degrade over 90 days

        // Confidence increases with recalculation count
        var recalculationFactor = Math.Min(1.0, latest.RecalculationCount / 10.0);

        return latest.Confidence * timeFactor * (0.5 + (recalculationFactor * 0.5));
    }

    public async Task<Result<int>> PurgeExpiredEventsAsync() {
        var expiredEvents = await _eventRepository.FindAsync(
            e => e.ExpiresAt.HasValue && e.ExpiresAt < DateTime.UtcNow);

        if (!expiredEvents.Any()) {
            return Result<int>.Success(0);
        }

        foreach (var expiredEvent in expiredEvents) {
            await _eventRepository.DeleteAsync(expiredEvent);
        }

        _logger.LogInformation("Purged {Count} expired events", expiredEvents.Count);

        return Result<int>.Success(expiredEvents.Count);
    }

    private async Task<ProfileAttribute> CreateOrUpdateAttributeAsync(
        Guid userId,
        string key,
        string value,
        string source,
        double confidence) {
        var existing = await _attributeRepository.FindAsync(
            a => a.UserId == userId && a.AttributeKey == key);

        if (existing.Any()) {
            var attribute = existing.First();
            attribute.Update(value, confidence);
            attribute.SetExpiration(TimeSpan.FromDays(30));
            await _attributeRepository.UpdateAsync(attribute);
            return attribute;
        }

        var newAttribute = new ProfileAttribute {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttributeKey = key,
            AttributeValue = value,
            Source = source,
            Confidence = confidence,
            UpdatedAt = DateTime.UtcNow,
            RecalculationCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        newAttribute.SetExpiration(TimeSpan.FromDays(30));

        await _attributeRepository.AddAsync(newAttribute);
        return newAttribute;
    }

    private string CalculateActivityLevel(List<UserBehaviorEvent> events) {
        var eventCount = events.Count;
        var daySpan = (events.Max(e => e.Timestamp) - events.Min(e => e.Timestamp)).TotalDays + 1;
        var eventsPerDay = eventCount / daySpan;

        return eventsPerDay switch {
            >= 50 => "VeryHigh",
            >= 20 => "High",
            >= 5 => "Medium",
            >= 1 => "Low",
            _ => "VeryLow"
        };
    }

    private string CalculatePreferredTimeOfDay(List<UserBehaviorEvent> events) {
        var hourCounts = events
            .GroupBy(e => e.Timestamp.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .First();

        return hourCounts.Hour switch {
            >= 6 and < 12 => "Morning",
            >= 12 and < 17 => "Afternoon",
            >= 17 and < 22 => "Evening",
            _ => "Night"
        };
    }

    private string ExtractMostViewedCategory(List<UserBehaviorEvent> pageViews) {
        // Parse page URLs to extract categories
        var categories = new Dictionary<string, int>();

        foreach (var view in pageViews) {
            try {
                var props = JsonSerializer.Deserialize<Dictionary<string, object>>(view.Properties);
                if (props != null && props.TryGetValue("category", out var category)) {
                    var categoryStr = category.ToString() ?? "Unknown";
                    categories[categoryStr] = categories.GetValueOrDefault(categoryStr, 0) + 1;
                }
            }
            catch {
                // Ignore parsing errors
            }
        }

        return categories.Any()
            ? categories.OrderByDescending(kv => kv.Value).First().Key
            : string.Empty;
    }
}
