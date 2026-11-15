using GameGuild.Configuration.ConfigurationFromAPI.InfrastructureLayer;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.InfrastructureLayer;

/// <summary>
///     Builder for InfrastructureLayerOptions using SharedKernel configuration patterns.
/// </summary>
public static class InfrastructureLayerOptionsBuilder
{
    /// <summary>
    ///     Creates InfrastructureLayerOptions with default values.
    /// </summary>
    public static InfrastructureLayerOptions CreateDefault()
    {
        return new InfrastructureLayerOptions
        {
            EnableDatabase = true,
            EnableMemoryCaching = true
        };
    }

    /// <summary>
    ///     Creates InfrastructureLayerOptions from configuration.
    /// </summary>
    public static InfrastructureLayerOptions Create(IConfiguration configuration)
    {
        return OptionBuilderUtilities.CreateAndBind(configuration, InfrastructureLayerOptions.SectionName, CreateDefault);
    }

    /// <summary>
    ///     Creates InfrastructureLayerOptions with validation.
    /// </summary>
    public static InfrastructureLayerOptions CreateWithValidation(IConfiguration configuration)
    {
        return OptionBuilderUtilities.CreateBindAndValidate(
            configuration,
            InfrastructureLayerOptions.SectionName,
            CreateDefault,
            options => options.Validate());
    }
}
