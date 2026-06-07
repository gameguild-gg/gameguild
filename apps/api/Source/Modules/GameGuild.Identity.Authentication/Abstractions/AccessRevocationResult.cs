namespace GameGuild.Identity.Authentication;

/// <summary>
///     Access revocation result
/// </summary>
public abstract class AccessRevocationResult
{
    /// <summary>
    ///     Number of permissions successfully revoked
    /// </summary>
    public int PermissionsRevoked { get; set; }

    /// <summary>
    ///     Number of revocation failures
    /// </summary>
    public int RevocationFailures { get; set; }

    /// <summary>
    ///     Details of failed revocations
    /// </summary>
    public List<RevocationFailure> Failures { get; set; } = new List<RevocationFailure>();

    /// <summary>
    ///     Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; } = SystemClock.UtcNow;
}
