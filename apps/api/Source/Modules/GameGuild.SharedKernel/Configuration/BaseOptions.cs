namespace GameGuild.Configuration;

/// <summary>
///     Base class for all configuration options providing common validation infrastructure.
/// </summary>
public abstract class BaseOptions
{
    /// <summary>
    ///     Gets a value indicating whether the configuration is enabled.
    /// </summary>
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Validates the configuration options. Override in derived classes to implement specific validation logic.
    /// </summary>
    public virtual void Validate() { }
}

/// <summary>
///     Base class for module-specific configuration options.
/// </summary>
public abstract class ModuleOptions : BaseOptions
{
    /// <summary>
    ///     The name of the module this configuration applies to.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public virtual string ModuleName => GetType().Assembly.GetName().Name ?? "Unknown";
}
