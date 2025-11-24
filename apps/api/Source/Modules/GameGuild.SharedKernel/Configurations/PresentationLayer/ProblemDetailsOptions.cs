namespace GameGuild.SharedKernel.Configuration;

public class ProblemDetailsOptions : BaseOptions
{
    public bool IncludeExceptionDetails { get; set; } = false;

    public override void Validate() { base.Validate(); }

    public static ProblemDetailsOptions CreateDefault() { return new ProblemDetailsOptions(); }
}
