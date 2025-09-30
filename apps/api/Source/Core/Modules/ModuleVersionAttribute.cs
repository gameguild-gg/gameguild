namespace GameGuild.Core.Modules;

/// <summary>
/// Attribute to specify the version of a module.
/// Used for module identification and compatibility tracking.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModuleVersionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the ModuleVersionAttribute.
    /// </summary>
    /// <param name="version">The version of the module (e.g., "1.0.0")</param>
    public ModuleVersionAttribute(string version) { Version = version ?? throw new ArgumentNullException(nameof(version)); }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    public string Version { get; }
}
