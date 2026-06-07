using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.ModelValidation;

/// <summary>
///     Builder for model validation options.
/// </summary>
public static class ModelValidationOptionsBuilder
{
    /// <summary>
    ///     Creates model validation options with default values.
    /// </summary>
    /// <returns>Default model validation options</returns>
    public static ModelValidationOptions Create() { return new ModelValidationOptions { SuppressModelStateInvalidFilter = false, ReturnBadRequestOnFailure = true }; }

    /// <summary>
    ///     Creates model validation options from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured model validation options</returns>
    public static ModelValidationOptions Create(IConfiguration configuration, string sectionName = "ModelValidation")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Create();
        var section = configuration.GetSection(sectionName);

        if (section.Exists()) { section.Bind(options); }

        return options;
    }

    /// <summary>
    ///     Validates the provided model validation options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    public static void Validate(ModelValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        // Model validation options are generally valid with any boolean values
    }

    /// <summary>
    ///     Creates and validates model validation options with default values.
    /// </summary>
    /// <returns>Validated model validation options with default configuration</returns>
    public static ModelValidationOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    ///     Creates and validates model validation options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated model validation options</returns>
    public static ModelValidationOptions Build(IConfiguration configuration, string sectionName = "ModelValidation")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }
}
