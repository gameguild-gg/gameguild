namespace GameGuild.Configuration.PresentationLayer.RequestContext;

public sealed class RequestContextOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "RequestContext";

    public bool EnableTenant { get; set; } = true;

    public bool EnableUser { get; set; } = true;

    public static RequestContextOptions CreateDefault() { return new RequestContextOptions(); }
}
