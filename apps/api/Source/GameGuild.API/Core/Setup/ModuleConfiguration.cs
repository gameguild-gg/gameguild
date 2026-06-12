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
    [
        "AI",
        "Assessments",
        "Authentication",
        "Authorization",
        "Billing",
        "Compliance.FERPA",
        "ContentPages",
        "Courses",
        "Features",
        "GameJams",
        "Learning.Certificates",
        "Learning.Cohorts",
        "Learning.Enrollments",
        "Learning.Experience.Discovery",
        "Learning.Experience.LearningPaths",
        "Learning.Experience.Recommendations",
        "Learning.Experience.Social",
        "Notifications",
        "Payments",
        "Products",
        "Projects",
        "TestingLab",
        "LaunchPad",
        "Resources",
        "Social.Blog",
        "Social.Feed",
        "Social.Groups",
        "Social.Profiles",
        "Social.Reactions",
        "Subscriptions",
        "Tags",
        "Tenants",
        "Users"
    ];

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

    /// <summary>
    ///     Determines whether an assembly name belongs to one of the enabled modules.
    ///     Module aliases may use compact names such as ContentPages while assemblies use
    ///     dotted names such as GameGuild.Content.Pages.
    /// </summary>
    public bool IsEnabledAssembly(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        return EnabledModules.Any(module =>
            assemblyName.EndsWith(module, StringComparison.OrdinalIgnoreCase) ||
            NormalizeModuleName(assemblyName).EndsWith(NormalizeModuleName(module), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeModuleName(string value)
        => value
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
}
