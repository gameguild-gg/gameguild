using System;

namespace GameGuild.Core.Modules;

/// <summary>
/// Attribute to specify the version of a module.
/// Used for module identification and compatibility tracking.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModuleVersionAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the ModuleVersionAttribute.
    /// </summary>
    /// <param name="version">The version of the module (e.g., "1.0.0")</param>
    public ModuleVersionAttribute(string version) {
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    public string Version { get; }
}

/// <summary>
/// Attribute to mark a module as using the standardized IModule pattern.
/// This helps identify modules that follow the current architectural standards.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StandardizedModuleAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the StandardizedModuleAttribute.
    /// </summary>
    /// <param name="description">Optional description of the module's purpose</param>
    public StandardizedModuleAttribute(string? description = null) {
        Description = description;
    }

    /// <summary>
    /// Gets the description of the module's purpose.
    /// </summary>
    public string? Description { get; }
}
