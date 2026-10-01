namespace GameGuild.Compliance.Audit;

/// <summary>
/// Signing keys used to make tamper-evident audit entries verifiable after rotation.
/// Private keys must be supplied by a secret store or deployment configuration.
/// </summary>
public sealed class AuditSigningOptions
{
    public static string SectionName { get; } = "AuditSigning";

    public string? ActiveKeyId { get; set; }

    public Dictionary<string, AuditSigningKeyOptions> Keys { get; set; } = new(StringComparer.Ordinal);
}

public sealed class AuditSigningKeyOptions
{
    public string? PrivateKeyPem { get; set; }

    public string? PublicKeyPem { get; set; }
}
