using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.Controllers;

public static class ControllersOptionsBuilder
{
    public static ControllersOptions CreateWithValidation(IConfiguration configuration)
    {
        var options = OptionBuilderUtilities.CreateAndBind(configuration, "Controllers", ControllersOptions.CreateDefault);
        options.Validate();
        return options;
    }
}
