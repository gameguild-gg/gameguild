using Fido2NetLib.Objects;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Configuration options for WebAuthn/FIDO2 ceremony parameters (issue #1143).
///     Binds to the existing <c>WebAuthn</c> configuration section next to
///     <c>ServerDomain</c>/<c>Origins</c>; the defaults reproduce the previous
///     hard-coded ceremony values, so behavior is unchanged unless configured.
/// </summary>
public sealed class WebAuthnCeremonyOptions
{
    public const string SectionName = "WebAuthn";

    /// <summary>
    ///     Attestation conveyance preference sent in registration options.
    ///     Accepted values: <c>none</c> (default), <c>indirect</c>, <c>direct</c>,
    ///     <c>enterprise</c>. Unrecognized values fall back to <c>none</c>.
    /// </summary>
    public string AttestationConveyance { get; set; } = "none";

    /// <summary>
    ///     User-verification requirement sent in registration and assertion options.
    ///     Accepted values: <c>preferred</c> (default), <c>required</c>, <c>discouraged</c>.
    ///     Unrecognized values fall back to <c>preferred</c>.
    /// </summary>
    public string UserVerification { get; set; } = "preferred";

    /// <summary>
    ///     Resolves <see cref="AttestationConveyance"/> to a FIDO2 value, falling back to
    ///     the previous hard-coded default (<c>None</c>) for null, empty, or unknown input.
    /// </summary>
    public AttestationConveyancePreference ResolvedAttestationConveyance =>
        Normalize(AttestationConveyance) switch
        {
            "indirect" => AttestationConveyancePreference.Indirect,
            "direct" => AttestationConveyancePreference.Direct,
            "enterprise" => AttestationConveyancePreference.Enterprise,
            "none" => AttestationConveyancePreference.None,
            _ => AttestationConveyancePreference.None
        };

    /// <summary>
    ///     Resolves <see cref="UserVerification"/> to a FIDO2 value, falling back to
    ///     the previous hard-coded default (<c>Preferred</c>) for null, empty, or unknown input.
    /// </summary>
    public UserVerificationRequirement ResolvedUserVerification =>
        Normalize(UserVerification) switch
        {
            "required" => UserVerificationRequirement.Required,
            "discouraged" => UserVerificationRequirement.Discouraged,
            "preferred" => UserVerificationRequirement.Preferred,
            _ => UserVerificationRequirement.Preferred
        };

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
