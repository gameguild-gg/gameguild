using Microsoft.Extensions.Configuration;

namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Template builder showing the recommended pattern for all option builders
///     This shows the correct design pattern that should be followed
/// </summary>
public static class TemplateOptionsBuilder
{
    /// <summary>
    ///     Creates template options with default values
    /// </summary>
    public static TemplateOptions CreateDefault() { return new TemplateOptions { Property1 = "default-value", Property2 = true, Property3 = 42 }; }

    /// <summary>
    ///     Creates template options from configuration using standard section name
    /// </summary>
    public static TemplateOptions Create(IConfiguration configuration) { return Create(configuration, "Template"); }

    /// <summary>
    ///     Creates template options from a specific configuration section
    /// </summary>
    public static TemplateOptions Create(IConfiguration configuration, string sectionName) { return OptionBuilderUtilities.CreateAndBind(configuration, sectionName, CreateDefault); }

    /// <summary>
    ///     Creates template options with validation
    /// </summary>
    public static TemplateOptions CreateWithValidation(IConfiguration configuration, string sectionName = "Template")
    {
        return OptionBuilderUtilities.CreateBindAndValidate(configuration, sectionName, CreateDefault, options => options.Validate());
    }
}
