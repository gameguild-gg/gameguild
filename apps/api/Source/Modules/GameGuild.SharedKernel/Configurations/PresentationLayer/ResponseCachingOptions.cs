namespace GameGuild.SharedKernel.Configuration;

public class ResponseCachingOptions : BaseOptions
{
    public bool EnableResponseCaching { get; set; } = false;

    public int DurationSeconds { get; set; } = 60;

    public int MaximumBodySize { get; set; } = 4096;

    public bool UseCaseSensitivePaths { get; set; } = false;

    public static ResponseCachingOptions CreateDefault() { return new ResponseCachingOptions(); }
}
