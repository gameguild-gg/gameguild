using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.Endpoints;

public static class EndpointsOptionsBuilder
{
    public static EndpointsOptions CreateWithValidation(IConfiguration configuration)
    {
        var options = OptionBuilderUtilities.CreateAndBind(configuration, "Endpoints", EndpointsOptions.CreateDefault);
        options.Validate();
        return options;
    }
}
