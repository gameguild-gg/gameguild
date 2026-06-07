namespace GameGuild.Identity.Authentication;

public abstract class PermissionAuditTrailDto
{
    public List<PermissionAuditEntry> Entries { get; set; } = new List<PermissionAuditEntry>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
