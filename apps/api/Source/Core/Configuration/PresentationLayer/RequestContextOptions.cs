namespace GameGuild;

public class RequestContextOptions
{
    public bool EnableUserContext { get; set; } = true;

    public bool EnableTenantContext { get; set; } = true;

    public bool EnableLocationContext { get; set; } = true;

    public bool EnableFeatureFlags { get; set; } = true;

    public void Validate()
    {
        // RequestContextOptions validation is optional - all features can be enabled or disabled independently
    }
}
