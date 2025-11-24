namespace GameGuild.SharedKernel.Configuration;

public class LocalizationOptions : BaseOptions
{
    public string DefaultCulture { get; set; } = "en-US";

    public string[ ] SupportedCultures { get; set; } = new[ ] { "en-US" };

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(DefaultCulture)) throw new InvalidOperationException("DefaultCulture must be configured.");

        if (SupportedCultures == null || SupportedCultures.Length == 0) throw new InvalidOperationException("SupportedCultures must contain at least one culture.");
    }

    public static LocalizationOptions CreateDefault() { return new LocalizationOptions(); }
}
