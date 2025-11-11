namespace GameGuild.SharedKernel.Configuration;

public class RequestContextOptions : BaseOptions
{
    public bool EnableTenant { get; set; } = true;

    public bool EnableUser { get; set; } = true;

    public override void Validate() { base.Validate(); }

    public static RequestContextOptions CreateDefault() { return new RequestContextOptions(); }
}
