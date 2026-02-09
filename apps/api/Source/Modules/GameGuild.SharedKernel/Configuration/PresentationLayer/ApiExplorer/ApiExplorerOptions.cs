namespace GameGuild.Configuration.PresentationLayer.ApiExplorer;

public sealed class ApiExplorerOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "ApiExplorer";

    public bool GroupByVersion { get; set; } = true;

    public string GroupNameFormat { get; set; } = "v{version}";

    public string DefaultGroupName { get; set; } = "v1";

    public static ApiExplorerOptions CreateDefault() { return new ApiExplorerOptions(); }
}
