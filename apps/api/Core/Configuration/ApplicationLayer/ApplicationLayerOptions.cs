namespace GameGuild;

/// <summary>
/// Configuration options for the Application Layer services.
/// </summary>
public class ApplicationLayerOptions
{
    /// <summary>
    /// Enables GameGuild.CQRS for CQRS pattern implementation.
    /// </summary>
    public bool EnableMediatR { get; set; } = true;

    /// <summary>
    /// Enables AutoMapper for object-to-object mapping.
    /// </summary>
    public bool EnableAutoMapper { get; set; } = true;

    /// <summary>
    /// Enables FluentValidation for input validation.
    /// </summary>
    public bool EnableFluentValidation { get; set; } = true;

    /// <summary>
    /// Configuration for caching services.
    /// </summary>
    public CachingOptions? Caching { get; set; }

    /// <summary>
    /// Configuration for background services.
    /// </summary>
    public BackgroundServiceOptions? BackgroundServices { get; set; }

    /// <summary>
    /// Validates the application layer options.
    /// </summary>
    public void Validate()
    {
        Caching?.Validate();
        BackgroundServices?.Validate();
    }
}
