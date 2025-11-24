namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for the Application Layer services.
/// </summary>
public class ApplicationLayerOptions : BaseOptions
{
    /// <summary>
    ///     Enables MediatR for CQRS pattern implementation.
    /// </summary>
    public bool EnableMediatR { get; set; } = true;

    /// <summary>
    ///     Enables AutoMapper for object-to-object mapping.
    /// </summary>
    public bool EnableAutoMapper { get; set; } = true;

    /// <summary>
    ///     Enables FluentValidation for input validation.
    /// </summary>
    public bool EnableFluentValidation { get; set; } = true;

    /// <summary>
    ///     Configuration for caching services.
    /// </summary>
    public CachingOptions? Caching { get; set; }

    /// <summary>
    ///     Configuration for background services.
    /// </summary>
    public BackgroundServiceOptions? BackgroundServices { get; set; }

    /// <summary>
    ///     Validates the application layer options.
    /// </summary>
    public override void Validate()
    {
        base.Validate();
        Caching?.Validate();
        BackgroundServices?.Validate();
    }

    /// <summary>
    ///     Creates default application layer options.
    /// </summary>
    public static ApplicationLayerOptions CreateDefault() { return new ApplicationLayerOptions { Caching = CachingOptions.CreateDefault(), BackgroundServices = BackgroundServiceOptions.CreateDefault() }; }
}
