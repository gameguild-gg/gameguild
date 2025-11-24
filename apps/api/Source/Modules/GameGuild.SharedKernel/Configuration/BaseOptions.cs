namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Base class for all configuration options providing common validation infrastructure
/// </summary>
public abstract class BaseOptions
{
    /// <summary>
    ///     Gets a value indicating whether the configuration is enabled
    /// </summary>
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Validates the configuration options. Override in derived classes to implement specific validation logic.
    /// </summary>
    public virtual void Validate()
    {
        // Default implementation - can be overridden by derived classes
    }
}

/// <summary>
///     Base class for module-specific configuration options
/// </summary>
public abstract class ModuleOptions : BaseOptions
{
    /// <summary>
    ///     The name of the module this configuration applies to
    /// </summary>
    public virtual string ModuleName { get => GetType().Assembly.GetName().Name ?? "Unknown"; }
}
