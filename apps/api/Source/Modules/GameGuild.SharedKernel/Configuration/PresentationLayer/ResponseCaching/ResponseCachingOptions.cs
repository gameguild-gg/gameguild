namespace GameGuild.Configuration.PresentationLayer.ResponseCaching;

public sealed class ResponseCachingOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "ResponseCaching";

    public bool EnableResponseCaching { get; set; } = false;

    public int DurationSeconds { get; set; } = 60;

    public int MaximumBodySize { get; set; } = 4096;

    public bool UseCaseSensitivePaths { get; set; } = false;

    public static ResponseCachingOptions CreateDefault() { return new ResponseCachingOptions(); }
}
