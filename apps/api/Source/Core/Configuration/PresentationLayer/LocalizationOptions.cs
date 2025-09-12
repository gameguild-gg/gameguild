namespace GameGuild;

public class LocalizationOptions
{
    public string DefaultCulture { get; set; } = "en-US";

    public string[] SupportedCultures { get; set; } = ["en-US"];

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultCulture))
            throw new InvalidOperationException("Default culture cannot be null or empty.");

        if (SupportedCultures == null || SupportedCultures.Length == 0)
            throw new InvalidOperationException("At least one supported culture must be specified.");

        if (!SupportedCultures.Contains(DefaultCulture))
            throw new InvalidOperationException("Default culture must be included in supported cultures.");
    }
}
