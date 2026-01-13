namespace GameGuild.Compliance.Audit;

public class AuditLogResponse
{
    public List<AuditLogDto> Logs { get; set; } = [];

    public int TotalCount { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; }
}
