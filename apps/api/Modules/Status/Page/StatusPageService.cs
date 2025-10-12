namespace GameGuild.Modules.Status.Page;

/// <summary>
/// Represents a service incident.
/// </summary>
public sealed class Incident
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public IncidentStatus Status { get; set; }
    public IncidentSeverity Severity { get; set; }
    public List<Guid> AffectedComponentIds { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public List<StatusUpdate> Updates { get; set; } = new();
}

/// <summary>
/// Status of an incident.
/// </summary>
public enum IncidentStatus
{
    Investigating,
    Identified,
    Monitoring,
    Resolved
}

/// <summary>
/// Severity level of an incident.
/// </summary>
public enum IncidentSeverity
{
    Minor,
    Major,
    Critical
}

/// <summary>
/// Represents a system component.
/// </summary>
public sealed class Component
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ComponentStatus Status { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public DateTime LastChecked { get; set; }
}

/// <summary>
/// Status of a component.
/// </summary>
public enum ComponentStatus
{
    Operational,
    DegradedPerformance,
    PartialOutage,
    MajorOutage,
    UnderMaintenance
}

/// <summary>
/// Represents a status update for an incident.
/// </summary>
public sealed class StatusUpdate
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public required string Message { get; set; }
    public IncidentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Represents a subscriber to status updates.
/// </summary>
public sealed class Subscriber
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public List<Guid> SubscribedComponentIds { get; set; } = new();
    public bool ReceiveAllIncidents { get; set; }
    public DateTime SubscribedAt { get; set; }
    public bool IsConfirmed { get; set; }
}

/// <summary>
/// Represents a scheduled maintenance window.
/// </summary>
public sealed class MaintenanceWindow
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public List<Guid> AffectedComponentIds { get; set; } = new();
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public MaintenanceStatus Status { get; set; }
}

/// <summary>
/// Status of a maintenance window.
/// </summary>
public enum MaintenanceStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Uptime statistics for a component.
/// </summary>
public sealed class UptimeStatistics
{
    public Guid ComponentId { get; set; }
    public double UptimePercentage { get; set; }
    public TimeSpan TotalDowntime { get; set; }
    public int IncidentCount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Dictionary<DateTime, double> DailyUptime { get; set; } = new();
}

/// <summary>
/// Result of notification sending.
/// </summary>
public sealed class NotificationResult
{
    public int EmailsSent { get; set; }
    public List<string> FailedRecipients { get; set; } = new();
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Service interface for status page operations.
/// </summary>
public interface IStatusPageService
{
    /// <summary>
    /// Creates a new incident.
    /// </summary>
    Task<Incident> CreateIncidentAsync(
        string title,
        string description,
        IncidentSeverity severity,
        List<Guid> affectedComponentIds,
        Guid createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an incident.
    /// </summary>
    Task<Incident> UpdateIncidentAsync(
        Guid incidentId,
        string? title = null,
        string? description = null,
        IncidentStatus? status = null,
        IncidentSeverity? severity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a status update to an incident.
    /// </summary>
    Task<StatusUpdate> AddStatusUpdateAsync(
        Guid incidentId,
        string message,
        IncidentStatus status,
        Guid createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an incident.
    /// </summary>
    Task<Incident> ResolveIncidentAsync(
        Guid incidentId,
        string resolutionMessage,
        Guid resolvedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an incident by ID.
    /// </summary>
    Task<Incident?> GetIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all incidents with optional filtering.
    /// </summary>
    Task<IReadOnlyList<Incident>> GetIncidentsAsync(
        IncidentStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a component.
    /// </summary>
    Task<Component> CreateComponentAsync(
        string name,
        string? description = null,
        Guid? parentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates component status.
    /// </summary>
    Task<Component> UpdateComponentStatusAsync(
        Guid componentId,
        ComponentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all components.
    /// </summary>
    Task<IReadOnlyList<Component>> GetComponentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to status updates.
    /// </summary>
    Task<Subscriber> SubscribeAsync(
        string email,
        List<Guid>? componentIds = null,
        bool receiveAll = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from status updates.
    /// </summary>
    Task UnsubscribeAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules maintenance.
    /// </summary>
    Task<MaintenanceWindow> ScheduleMaintenanceAsync(
        string title,
        string description,
        List<Guid> affectedComponentIds,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled maintenance windows.
    /// </summary>
    Task<IReadOnlyList<MaintenanceWindow>> GetMaintenanceWindowsAsync(
        MaintenanceStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets uptime statistics for a component.
    /// </summary>
    Task<UptimeStatistics> GetUptimeStatisticsAsync(
        Guid componentId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification to subscribers about an incident.
    /// </summary>
    Task<NotificationResult> NotifySubscribersAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of status page service for incident management and communication.
/// </summary>
public sealed class StatusPageService : IStatusPageService
{
    private readonly ILogger<StatusPageService> _logger;
    private readonly Dictionary<Guid, Incident> _incidents = new();
    private readonly Dictionary<Guid, Component> _components = new();
    private readonly Dictionary<Guid, Subscriber> _subscribers = new();
    private readonly Dictionary<Guid, MaintenanceWindow> _maintenanceWindows = new();

    public StatusPageService(ILogger<StatusPageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Incident> CreateIncidentAsync(
        string title,
        string description,
        IncidentSeverity severity,
        List<Guid> affectedComponentIds,
        Guid createdBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating incident: {Title} with severity {Severity}", title, severity);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Severity = severity,
            Status = IncidentStatus.Investigating,
            AffectedComponentIds = affectedComponentIds,
            CreatedBy = createdBy,
            StartedAt = DateTime.UtcNow
        };

        foreach (var componentId in affectedComponentIds)
        {
            if (_components.TryGetValue(componentId, out var component))
            {
                component.Status = severity switch
                {
                    IncidentSeverity.Critical => ComponentStatus.MajorOutage,
                    IncidentSeverity.Major => ComponentStatus.PartialOutage,
                    _ => ComponentStatus.DegradedPerformance
                };
            }
        }

        _incidents[incident.Id] = incident;
        return Task.FromResult(incident);
    }

    public Task<Incident> UpdateIncidentAsync(
        Guid incidentId,
        string? title = null,
        string? description = null,
        IncidentStatus? status = null,
        IncidentSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident))
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        if (title != null) incident.Title = title;
        if (description != null) incident.Description = description;
        if (status.HasValue) incident.Status = status.Value;
        if (severity.HasValue) incident.Severity = severity.Value;

        _logger.LogInformation("Updated incident: {IncidentId}", incidentId);
        return Task.FromResult(incident);
    }

    public Task<StatusUpdate> AddStatusUpdateAsync(
        Guid incidentId,
        string message,
        IncidentStatus status,
        Guid createdBy,
        CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident))
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        var update = new StatusUpdate
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Message = message,
            Status = status,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        incident.Updates.Add(update);
        incident.Status = status;

        _logger.LogInformation("Added status update to incident {IncidentId}: {Status}", incidentId, status);
        return Task.FromResult(update);
    }

    public Task<Incident> ResolveIncidentAsync(
        Guid incidentId,
        string resolutionMessage,
        Guid resolvedBy,
        CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident))
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAt = DateTime.UtcNow;

        var update = new StatusUpdate
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Message = resolutionMessage,
            Status = IncidentStatus.Resolved,
            CreatedBy = resolvedBy,
            CreatedAt = DateTime.UtcNow
        };

        incident.Updates.Add(update);

        foreach (var componentId in incident.AffectedComponentIds)
        {
            if (_components.TryGetValue(componentId, out var component))
            {
                component.Status = ComponentStatus.Operational;
            }
        }

        _logger.LogInformation("Resolved incident: {IncidentId}", incidentId);
        return Task.FromResult(incident);
    }

    public Task<Incident?> GetIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        _incidents.TryGetValue(incidentId, out var incident);
        return Task.FromResult(incident);
    }

    public Task<IReadOnlyList<Incident>> GetIncidentsAsync(
        IncidentStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var incidents = _incidents.Values.AsEnumerable();

        if (status.HasValue)
        {
            incidents = incidents.Where(i => i.Status == status);
        }

        if (startDate.HasValue)
        {
            incidents = incidents.Where(i => i.StartedAt >= startDate);
        }

        if (endDate.HasValue)
        {
            incidents = incidents.Where(i => i.StartedAt <= endDate);
        }

        var result = incidents
            .OrderByDescending(i => i.StartedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Incident>>(result);
    }

    public Task<Component> CreateComponentAsync(
        string name,
        string? description = null,
        Guid? parentId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating component: {Name}", name);

        var component = new Component
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ParentId = parentId,
            Status = ComponentStatus.Operational,
            Order = _components.Count,
            LastChecked = DateTime.UtcNow
        };

        _components[component.Id] = component;
        return Task.FromResult(component);
    }

    public Task<Component> UpdateComponentStatusAsync(
        Guid componentId,
        ComponentStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!_components.TryGetValue(componentId, out var component))
        {
            throw new InvalidOperationException($"Component {componentId} not found");
        }

        component.Status = status;
        component.LastChecked = DateTime.UtcNow;

        _logger.LogInformation("Updated component {ComponentId} status to {Status}", componentId, status);
        return Task.FromResult(component);
    }

    public Task<IReadOnlyList<Component>> GetComponentsAsync(
        CancellationToken cancellationToken = default)
    {
        var components = _components.Values
            .OrderBy(c => c.Order)
            .ToList();

        return Task.FromResult<IReadOnlyList<Component>>(components);
    }

    public Task<Subscriber> SubscribeAsync(
        string email,
        List<Guid>? componentIds = null,
        bool receiveAll = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating subscription for {Email}", email);

        var subscriber = new Subscriber
        {
            Id = Guid.NewGuid(),
            Email = email,
            SubscribedComponentIds = componentIds ?? new List<Guid>(),
            ReceiveAllIncidents = receiveAll,
            SubscribedAt = DateTime.UtcNow,
            IsConfirmed = false
        };

        _subscribers[subscriber.Id] = subscriber;
        return Task.FromResult(subscriber);
    }

    public Task UnsubscribeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var subscriber = _subscribers.Values.FirstOrDefault(s => s.Email == email);
        if (subscriber != null)
        {
            _subscribers.Remove(subscriber.Id);
            _logger.LogInformation("Unsubscribed {Email}", email);
        }

        return Task.CompletedTask;
    }

    public Task<MaintenanceWindow> ScheduleMaintenanceAsync(
        string title,
        string description,
        List<Guid> affectedComponentIds,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduling maintenance: {Title} from {Start} to {End}", title, scheduledStart, scheduledEnd);

        var maintenance = new MaintenanceWindow
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            AffectedComponentIds = affectedComponentIds,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            Status = MaintenanceStatus.Scheduled
        };

        _maintenanceWindows[maintenance.Id] = maintenance;
        return Task.FromResult(maintenance);
    }

    public Task<IReadOnlyList<MaintenanceWindow>> GetMaintenanceWindowsAsync(
        MaintenanceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var windows = _maintenanceWindows.Values.AsEnumerable();

        if (status.HasValue)
        {
            windows = windows.Where(m => m.Status == status);
        }

        var result = windows
            .OrderBy(m => m.ScheduledStart)
            .ToList();

        return Task.FromResult<IReadOnlyList<MaintenanceWindow>>(result);
    }

    public Task<UptimeStatistics> GetUptimeStatisticsAsync(
        Guid componentId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (!_components.ContainsKey(componentId))
        {
            throw new InvalidOperationException($"Component {componentId} not found");
        }

        var incidents = _incidents.Values
            .Where(i => i.AffectedComponentIds.Contains(componentId))
            .Where(i => i.StartedAt >= startDate && i.StartedAt <= endDate)
            .ToList();

        var totalDowntime = incidents
            .Where(i => i.ResolvedAt.HasValue)
            .Sum(i => (i.ResolvedAt!.Value - i.StartedAt).TotalSeconds);

        var periodSeconds = (endDate - startDate).TotalSeconds;
        var uptimePercentage = periodSeconds > 0 ? ((periodSeconds - totalDowntime) / periodSeconds) * 100 : 100;

        var stats = new UptimeStatistics
        {
            ComponentId = componentId,
            UptimePercentage = uptimePercentage,
            TotalDowntime = TimeSpan.FromSeconds(totalDowntime),
            IncidentCount = incidents.Count,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            DailyUptime = new Dictionary<DateTime, double>()
        };

        return Task.FromResult(stats);
    }

    public Task<NotificationResult> NotifySubscribersAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident))
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        var recipients = _subscribers.Values
            .Where(s => s.IsConfirmed)
            .Where(s => s.ReceiveAllIncidents ||
                       s.SubscribedComponentIds.Intersect(incident.AffectedComponentIds).Any())
            .ToList();

        _logger.LogInformation("Notifying {Count} subscribers about incident {IncidentId}", recipients.Count, incidentId);

        var result = new NotificationResult
        {
            EmailsSent = recipients.Count,
            SentAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }
}
