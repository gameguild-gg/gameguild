namespace GameGuild.Identity.Authentication;

/// <summary>
///     Revocation failure details
/// </summary>
public abstract class RevocationFailure
{
    /// <summary>
    ///     Item ID that failed revocation
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    ///     User ID whose permission failed to revoke
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Permission that failed to revoke
    /// </summary>
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    ///     Reason for failure
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
