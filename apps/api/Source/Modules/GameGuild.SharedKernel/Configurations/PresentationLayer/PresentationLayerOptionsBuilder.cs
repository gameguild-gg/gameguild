using Microsoft.Extensions.Configuration;

namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Builder for PresentationLayerOptions using SharedKernel configuration patterns
/// </summary>
public static class PresentationLayerOptionsBuilder
{
    /// <summary>
    ///     Creates PresentationLayerOptions with default values
    /// </summary>
    public static PresentationLayerOptions CreateDefault()
    {
        return new PresentationLayerOptions
        {
            EnableOpenApi = true,
            EnableApiVersioning = true,
            EnableApiExplorer = true,
            EnableCors = true,
            EnableAuthentication = true,
            EnableAuthorization = true,
            EnableResponseCompression = true,
            EnableProblemDetails = true,
            EnableModelValidation = true,
            EnableHealthChecks = true,
            EnableRequestContext = true,
            EnableResponseCaching = true,
            EnableMemoryCaching = true
        };
    }

    /// <summary>
    ///     Creates PresentationLayerOptions from configuration
    /// </summary>
    public static PresentationLayerOptions Create(IConfiguration configuration) { return OptionBuilderUtilities.CreateAndBind(configuration, "PresentationLayer", CreateDefault); }

    /// <summary>
    ///     Creates PresentationLayerOptions with validation
    /// </summary>
    public static PresentationLayerOptions CreateWithValidation(IConfiguration configuration)
    {
        return OptionBuilderUtilities.CreateBindAndValidate(configuration, "PresentationLayer", CreateDefault, options => options.Validate());
    }
}
