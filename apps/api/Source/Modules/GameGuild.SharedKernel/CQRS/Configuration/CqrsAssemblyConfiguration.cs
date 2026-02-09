namespace GameGuild.CQRS;

/// <summary>
///     Configuration for CQRS assembly scanning.
/// </summary>
public sealed class CqrsAssemblyConfiguration
{
    /// <summary>
    ///     Gets or sets whether to include request handlers.
    /// </summary>
    public bool IncludeRequestHandlers { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to include notification handlers.
    /// </summary>
    public bool IncludeNotificationHandlers { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to include pre-processors.
    /// </summary>
    public bool IncludePreProcessors { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to include post-processors.
    /// </summary>
    public bool IncludePostProcessors { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to include exception handlers.
    /// </summary>
    public bool IncludeExceptionHandlers { get; set; } = true;

    /// <summary>
    ///     Gets or sets assembly filtering predicate.
    /// </summary>
    public Func<Type, bool>? TypeFilter { get; set; }
}
