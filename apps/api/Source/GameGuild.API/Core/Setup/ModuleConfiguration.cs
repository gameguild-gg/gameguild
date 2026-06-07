namespace GameGuild.API.Setup;

/// <summary>
///     Configuration for module discovery and registration.
///     Follows Open/Closed Principle - extend by adding to configuration, not modifying code.
/// </summary>
public sealed class ModuleConfiguration
{
    /// <summary>
    ///     Default enabled modules for the application.
    /// </summary>
    public static readonly string[] DefaultEnabledModules =
        ["AI", "Assessments", "Authentication", "Authorization", "Billing", "Compliance.FERPA", "ContentPages", "Courses", "Features", "GameJams", "Learning.Enrollments", "Notifications", "Payments", "Products", "Resources", "Social.Blog", "Social.Feed", "Social.Groups", "Social.Profiles", "Social.Reactions", "Subscriptions", "Tags", "Tenants", "Users"];

    /// <summary>
    ///     Gets or sets the list of enabled module names.
    /// </summary>
    public string[] EnabledModules { get; set; } = DefaultEnabledModules;

    /// <summary>
    ///     Gets or sets the assembly name prefix pattern for module discovery.
    /// </summary>
    public string AssemblyPrefix { get; set; } = "GameGuild.";

    /// <summary>
    ///     Gets or sets whether to exclude test assemblies from discovery.
    /// </summary>
    public bool ExcludeTestAssemblies { get; set; } = true;

    /// <summary>
    ///     Handler interface type names used for counting/logging purposes.
    /// </summary>
    public static readonly string[] HandlerTypeNames =
        ["ICommandHandler", "IQueryHandler", "IRequestHandler"];
}
