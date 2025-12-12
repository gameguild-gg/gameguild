namespace GameGuild.SharedKernel.Configuration;

public class FeatureFlagsOptions : BaseOptions
{
    public bool EnableOpenFeature { get; set; } = false;

    public string? Provider { get; set; }

    public override void Validate()
    {
        base.Validate();
        // additional validation if necessary
    }

    public static FeatureFlagsOptions CreateDefault() { return new FeatureFlagsOptions(); }
}
