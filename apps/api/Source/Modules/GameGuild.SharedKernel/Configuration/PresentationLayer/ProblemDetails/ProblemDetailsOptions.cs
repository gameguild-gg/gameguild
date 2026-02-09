namespace GameGuild.Configuration.PresentationLayer.ProblemDetails;

public sealed class ProblemDetailsOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "ProblemDetails";

    public bool IncludeExceptionDetails { get; set; } = false;

    public static ProblemDetailsOptions CreateDefault() { return new ProblemDetailsOptions(); }
}
