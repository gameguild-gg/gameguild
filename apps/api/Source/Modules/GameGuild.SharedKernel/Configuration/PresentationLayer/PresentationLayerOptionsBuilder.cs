using Microsoft.Extensions.Configuration;
using GameGuild.Configuration.PresentationLayer.ApiExplorer;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using GameGuild.Configuration.PresentationLayer.OpenAPI;

namespace GameGuild.Configuration.PresentationLayer;

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
            EnableMemoryCaching = true,
            ApiVersioning = ApiVersioningOptions.CreateDefault(),
            OpenApi = OpenApiOptions.CreateDefault(),
            ApiExplorer = ApiExplorerOptions.CreateDefault()
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
