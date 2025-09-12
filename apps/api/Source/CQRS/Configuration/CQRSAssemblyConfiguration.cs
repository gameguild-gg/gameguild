namespace GameGuild.CQRS;

/// <summary>
/// Configuration for CQRS assembly scanning
/// </summary>
public class CQRSAssemblyConfiguration
{
    /// <summary>
    /// Gets or sets whether to include request handlers
    /// </summary>
    public bool IncludeRequestHandlers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include notification handlers
    /// </summary>
    public bool IncludeNotificationHandlers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include pre-processors
    /// </summary>
    public bool IncludePreProcessors { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include post-processors
    /// </summary>
    public bool IncludePostProcessors { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include exception handlers
    /// </summary>
    public bool IncludeExceptionHandlers { get; set; } = true;

    /// <summary>
    /// Gets or sets the service lifetime for handlers
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Gets or sets assembly filtering predicate
    /// </summary>
    public Func<Type, bool>? TypeFilter { get; set; }

    /// <summary>
    /// Gets or sets whether to register open generic types
    /// </summary>
    public bool RegisterOpenGenericTypes { get; set; } = false;
}
