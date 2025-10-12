namespace GameGuild.Core.Modules;

/// <summary>
/// Attribute to mark a module as using the standardized IModule pattern.
/// This helps identify modules that follow the current architectural standards.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StandardizedModuleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the StandardizedModuleAttribute.
    /// </summary>
    /// <param name="description">Optional description of the module's purpose</param>
    public StandardizedModuleAttribute(string? description = null) { Description = description; }

    /// <summary>
    /// Gets the description of the module's purpose.
    /// </summary>
    public string? Description { get; }
}
