namespace GameGuild.Resources;

/// <summary>
///     Request model for checking resource quota enforcement
/// </summary>
public class CheckResourceQuotaRequest
{
    public long Amount { get; set; } = 1;
}
