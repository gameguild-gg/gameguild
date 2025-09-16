using System.Collections.Concurrent;


namespace GameGuild;

/// <summary>
/// Implementation of resource context for tracking current resource access
/// Provides thread-safe resource context management for request lifecycle
/// </summary>
public class ResourceContext : IResourceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ResourceContext> _logger;
    private readonly ConcurrentDictionary<string, object> _metadata = new();
    private readonly List<ResourceInfo> _resourceHierarchy = new();
    private readonly object _lock = new();

    private Guid? _resourceId;
    private string? _resourceType;
    private string? _resourceName;
    private Guid? _parentResourceId;
    private string? _parentResourceType;
    private string? _currentAction;

    public ResourceContext(IHttpContextAccessor httpContextAccessor, ILogger<ResourceContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        // Initialize from HTTP context
        InitializeFromHttpContext();
    }

    // === PROPERTIES ===

    public Guid? ResourceId => _resourceId;

    public string? ResourceType => _resourceType;

    public string? ResourceName => _resourceName;

    public Guid? ParentResourceId => _parentResourceId;

    public string? ParentResourceType => _parentResourceType;

    public string? CurrentAction => _currentAction;

    public string? HttpMethod => _httpContextAccessor.HttpContext?.Request.Method;

    public string? RequestPath => _httpContextAccessor.HttpContext?.Request.Path;

    public IDictionary<string, object> Metadata => _metadata;

    public bool IsSubResource => _parentResourceId.HasValue;

    public int HierarchyDepth
    {
        get
        {
            lock (_lock)
            {
                return _resourceHierarchy.Count;
            }
        }
    }

    // === RESOURCE HIERARCHY ===

    public IEnumerable<ResourceInfo> GetResourceHierarchy()
    {
        lock (_lock)
        {
            return _resourceHierarchy.ToList(); // Return copy to prevent modification
        }
    }

    // === RESOURCE CONTEXT MANAGEMENT ===

    public void SetResourceContext(Guid? resourceId, string? resourceType, string? resourceName = null, string? action = null, IDictionary<string, object>? metadata = null)
    {
        lock (_lock)
        {
            _resourceId = resourceId;
            _resourceType = resourceType;
            _resourceName = resourceName ?? _resourceName;
            _currentAction = action ?? _currentAction;

            // Add to hierarchy if this is a new resource
            if (resourceId.HasValue && !_resourceHierarchy.Any(r => r.Id == resourceId))
            {
                var resourceInfo = new ResourceInfo(resourceId, resourceType, resourceName, _resourceHierarchy.Count);
                _resourceHierarchy.Add(resourceInfo);

                _logger.LogDebug("Added resource to hierarchy: {ResourceInfo}", resourceInfo);
            }

            // Update metadata
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    _metadata.AddOrUpdate(kvp.Key, kvp.Value, (key, oldValue) => kvp.Value);
                }
            }

            // Store in HTTP context items for easy access
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Items["ResourceContext.ResourceId"] = _resourceId;
                httpContext.Items["ResourceContext.ResourceType"] = _resourceType;
                httpContext.Items["ResourceContext.ResourceName"] = _resourceName;
                httpContext.Items["ResourceContext.CurrentAction"] = _currentAction;
            }

            _logger.LogDebug("Set resource context: Id={ResourceId}, Type={ResourceType}, Name={ResourceName}, Action={Action}",
                _resourceId, _resourceType, _resourceName, _currentAction);
        }
    }

    public void SetParentResourceContext(Guid? parentResourceId, string? parentResourceType)
    {
        lock (_lock)
        {
            _parentResourceId = parentResourceId;
            _parentResourceType = parentResourceType;

            // Store in HTTP context items
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Items["ResourceContext.ParentResourceId"] = _parentResourceId;
                httpContext.Items["ResourceContext.ParentResourceType"] = _parentResourceType;
            }

            _logger.LogDebug("Set parent resource context: Id={ParentResourceId}, Type={ParentResourceType}",
                _parentResourceId, _parentResourceType);
        }
    }

    public void AddMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _metadata.AddOrUpdate(key, value, (existingKey, existingValue) => value);

        _logger.LogDebug("Added metadata: {Key} = {Value}", key, value);
    }

    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_metadata.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // Try to convert
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert metadata value for key {Key} to type {Type}", key, typeof(T).Name);
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public void ClearResourceContext()
    {
        lock (_lock)
        {
            _resourceId = null;
            _resourceType = null;
            _resourceName = null;
            _parentResourceId = null;
            _parentResourceType = null;
            _currentAction = null;
            _metadata.Clear();
            _resourceHierarchy.Clear();

            // Clear HTTP context items
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var keysToRemove = httpContext.Items.Keys
                    .Where(k => k.ToString()?.StartsWith("ResourceContext.") == true)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    httpContext.Items.Remove(key);
                }
            }

            _logger.LogDebug("Cleared resource context");
        }
    }

    // === HELPER METHODS ===

    public bool IsAccessingResource(Guid resourceId)
    {
        return _resourceId == resourceId;
    }

    public bool IsAccessingResourceType(string resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        return string.Equals(_resourceType, resourceType, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsPerformingAction(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return string.Equals(_currentAction, action, StringComparison.OrdinalIgnoreCase);
    }

    public string GetResourceIdentifier()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(_resourceType))
        {
            parts.Add(_resourceType);
        }

        if (_resourceId.HasValue)
        {
            parts.Add(_resourceId.Value.ToString());
        }
        else if (!string.IsNullOrEmpty(_resourceName))
        {
            parts.Add(_resourceName);
        }

        if (!string.IsNullOrEmpty(_currentAction))
        {
            parts.Add($"action:{_currentAction}");
        }

        return parts.Any() ? string.Join(":", parts) : "Unknown";
    }

    // === PRIVATE METHODS ===

    private void InitializeFromHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        try
        {
            // Try to extract resource information from route data
            var routeData = httpContext.GetRouteData();
            if (routeData?.Values != null)
            {
                // Look for common route parameters
                ExtractFromRouteValue(routeData.Values, "id", "resourceId", "Id");
                ExtractFromRouteValue(routeData.Values, "controller", null, "controller");
                ExtractFromRouteValue(routeData.Values, "action", null, "action");

                // Look for parent resource IDs
                ExtractFromRouteValue(routeData.Values, "parentId", "parentResourceId", "parentId");
            }

            // Try to extract from headers
            ExtractFromHeader("X-Resource-Id", "resourceId");
            ExtractFromHeader("X-Resource-Type", "resourceType");
            ExtractFromHeader("X-Resource-Name", "resourceName");
            ExtractFromHeader("X-Parent-Resource-Id", "parentResourceId");
            ExtractFromHeader("X-Current-Action", "currentAction");

            // Try to extract from query parameters
            ExtractFromQuery("resourceId", "resourceId");
            ExtractFromQuery("resourceType", "resourceType");
            ExtractFromQuery("parentId", "parentResourceId");

            _logger.LogDebug("Initialized resource context from HTTP context: ResourceId={ResourceId}, Type={ResourceType}, Name={ResourceName}",
                _resourceId, _resourceType, _resourceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error initializing resource context from HTTP context");
        }
    }

    private void ExtractFromRouteValue(RouteValueDictionary values, string routeKey, string? metadataKey, string? propertyName)
    {
        if (values.TryGetValue(routeKey, out var value) && value != null)
        {
            var stringValue = value.ToString();
            if (!string.IsNullOrEmpty(stringValue))
            {
                // Set property if specified
                if (!string.IsNullOrEmpty(propertyName))
                {
                    SetPropertyFromString(propertyName, stringValue);
                }

                // Add to metadata if specified
                if (!string.IsNullOrEmpty(metadataKey))
                {
                    _metadata.TryAdd(metadataKey, stringValue);
                }

                _logger.LogDebug("Extracted from route {RouteKey}: {Value}", routeKey, stringValue);
            }
        }
    }

    private void ExtractFromHeader(string headerName, string propertyName)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request.Headers.TryGetValue(headerName, out var headerValue) == true)
        {
            var stringValue = headerValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(stringValue))
            {
                SetPropertyFromString(propertyName, stringValue);
                _logger.LogDebug("Extracted from header {HeaderName}: {Value}", headerName, stringValue);
            }
        }
    }

    private void ExtractFromQuery(string queryKey, string propertyName)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request.Query.TryGetValue(queryKey, out var queryValue) == true)
        {
            var stringValue = queryValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(stringValue))
            {
                SetPropertyFromString(propertyName, stringValue);
                _logger.LogDebug("Extracted from query {QueryKey}: {Value}", queryKey, stringValue);
            }
        }
    }

    private void SetPropertyFromString(string propertyName, string value)
    {
        switch (propertyName.ToLowerInvariant())
        {
            case "resourceid":
            case "id":
                if (Guid.TryParse(value, out var resourceId))
                {
                    _resourceId = resourceId;
                }
                break;

            case "resourcetype":
            case "controller":
                _resourceType = value;
                break;

            case "resourcename":
                _resourceName = value;
                break;

            case "parentresourceid":
            case "parentid":
                if (Guid.TryParse(value, out var parentResourceId))
                {
                    _parentResourceId = parentResourceId;
                }
                break;

            case "currentaction":
            case "action":
                _currentAction = value;
                break;
        }
    }
}
