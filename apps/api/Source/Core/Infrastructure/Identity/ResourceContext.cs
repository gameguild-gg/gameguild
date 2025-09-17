using System.Collections.Concurrent;
using GameGuild.Core.Domain.Identity;


namespace GameGuild.Core.Infrastructure.Identity;

/// <summary>
/// Implementation of resource context for tracking current resource access
/// Provides thread-safe resource context management for request lifecycle
/// </summary>
public class ResourceContext : IResourceContext {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ResourceContext> _logger;
    private readonly ConcurrentDictionary<string, object> _metadata = new();
    private readonly object _lock = new();

    private Guid? _resourceId;
    private string? _resourceType;

    public ResourceContext(IHttpContextAccessor httpContextAccessor, ILogger<ResourceContext> logger) {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        // Initialize from HTTP context
        InitializeFromHttpContext();
    }

    // === PROPERTIES ===

    public Guid? ResourceId => _resourceId;

    public string? ResourceType => _resourceType;

    // === PUBLIC METHODS ===

    public string GetResourceIdentifier() {
        if (_resourceId.HasValue && !string.IsNullOrEmpty(_resourceType)) {
            return $"{_resourceType}:{_resourceId}";
        }

        return "unknown";
    }

    public void SetResource(string resourceType, Guid resourceId) {
        lock (_lock) {
            _resourceType = resourceType;
            _resourceId = resourceId;

            _logger.LogDebug("Resource context set to {ResourceType}:{ResourceId}", resourceType, resourceId);
        }
    }

    public void ClearResource() {
        lock (_lock) {
            _resourceType = null;
            _resourceId = null;

            _logger.LogDebug("Resource context cleared");
        }
    }

    // === PRIVATE METHODS ===

    private void InitializeFromHttpContext() {
        try {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // Try to extract resource information from route data
            var routeData = httpContext.Request.RouteValues;

            // Look for common route parameters
            if (routeData.TryGetValue("id", out var idValue) && Guid.TryParse(idValue?.ToString(), out var resourceGuid)) {
                _resourceId = resourceGuid;
            }

            // Try to determine resource type from controller name or route
            if (routeData.TryGetValue("controller", out var controllerValue)) {
                _resourceType = controllerValue?.ToString();
            }

            if (_resourceId.HasValue && !string.IsNullOrEmpty(_resourceType)) {
                _logger.LogDebug("Initialized resource context from HTTP: {ResourceType}:{ResourceId}", _resourceType, _resourceId);
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to initialize resource context from HTTP context");
        }
    }
}
