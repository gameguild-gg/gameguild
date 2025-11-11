namespace GameGuild.SharedKernel.Configuration;

public class AuthorizationOptions : BaseOptions
{
    public string DefaultPolicy { get; set; } = "Default";

    public bool RequireAuthenticatedUser { get; set; } = true;

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(DefaultPolicy)) throw new InvalidOperationException("Default policy cannot be null or empty.");
    }

    public static AuthorizationOptions CreateDefault() { return new AuthorizationOptions(); }
}
