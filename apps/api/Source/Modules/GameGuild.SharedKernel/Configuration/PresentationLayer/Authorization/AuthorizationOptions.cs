namespace GameGuild.Configuration.PresentationLayer.Authorization;

public sealed class AuthorizationOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name.
    /// </summary>
    public const string SectionName = "Authorization";

    public string DefaultPolicy { get; set; } = "Default";

    public bool RequireAuthenticatedUser { get; set; } = true;

    /// <summary>
    ///     The system account ID that receives all permissions (wildcard).
    ///     This should be a well-known GUID configured in environment settings.
    /// </summary>
    /// <remarks>
    ///     Default is a well-known system account GUID. Override in production via configuration.
    /// </remarks>
    public Guid SystemAccountId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(DefaultPolicy)) throw new InvalidOperationException("Default policy cannot be null or empty.");
        
        if (SystemAccountId == Guid.Empty)
            throw new InvalidOperationException("SystemAccountId cannot be empty GUID.");
    }

    public static AuthorizationOptions CreateDefault() { return new AuthorizationOptions(); }
}
