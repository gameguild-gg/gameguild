using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.HttpLogging;

/// <summary>
///     Builder for HTTP logging options following the standard Create/Validate/Build pattern
/// </summary>
public static class HttpLoggingOptionsBuilder
{
    /// <summary>
    ///     Creates HTTP logging options with default values
    /// </summary>
    public static HttpLoggingOptions CreateDefault() { return new HttpLoggingOptions { LogRequestHeaders = true, LogResponseHeaders = true, LogRequestBody = false, LogResponseBody = false }; }

    /// <summary>
    ///     Creates HTTP logging options from configuration
    /// </summary>
    public static HttpLoggingOptions Create(IConfiguration configuration, string sectionName = "HttpLogging") { return OptionBuilderUtilities.CreateAndBind(configuration, sectionName, CreateDefault); }

    /// <summary>
    ///     Creates HTTP logging options with validation
    /// </summary>
    public static HttpLoggingOptions CreateWithValidation(IConfiguration configuration, string sectionName = "HttpLogging")
    {
        return OptionBuilderUtilities.CreateBindAndValidate(configuration, sectionName, CreateDefault, options => options.Validate());
    }
}
