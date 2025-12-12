namespace GameGuild.SharedKernel.Configuration;

public class OpenApiServerOptions : BaseOptions
{
    public string Url { get; set; } = "https://localhost:5001";

    public string Description { get; set; } = "Local server";

    public static OpenApiServerOptions CreateDefault() { return new OpenApiServerOptions(); }
}
