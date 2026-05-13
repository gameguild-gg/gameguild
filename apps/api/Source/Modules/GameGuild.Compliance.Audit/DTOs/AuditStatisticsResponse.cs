namespace GameGuild.Compliance.Audit;

public class AuditStatisticsResponse
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalEvents { get; set; }

    public int AuthenticationEvents { get; set; }

    public int PermissionEvents { get; set; }

    public int SecurityEvents { get; set; }

    public int FailedEvents { get; set; }

    public int HighRiskEvents { get; set; }
}
