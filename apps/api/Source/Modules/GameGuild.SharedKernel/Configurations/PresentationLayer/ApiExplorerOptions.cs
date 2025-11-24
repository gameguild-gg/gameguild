namespace GameGuild.SharedKernel.Configuration;

public class ApiExplorerOptions : BaseOptions
{
    public bool GroupByVersion { get; set; } = true;

    public string GroupNameFormat { get; set; } = "v{version}";

    public string DefaultGroupName { get; set; } = "v1";

    public static ApiExplorerOptions CreateDefault() { return new ApiExplorerOptions(); }
}
