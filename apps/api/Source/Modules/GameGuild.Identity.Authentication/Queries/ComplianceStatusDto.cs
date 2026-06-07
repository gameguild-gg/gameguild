namespace GameGuild.Identity.Authentication;

public abstract class ComplianceStatusDto
{
    public Guid TenantId { get; set; }

    public double ComplianceScore { get; set; }

    public int TotalUsers { get; set; }

    public int UsersReviewed { get; set; }

    public int OverdueReviews { get; set; }

    public int UpcomingReviews { get; set; }

    public List<ComplianceIssue> Issues { get; set; } = new List<ComplianceIssue>();

    public DateTime LastAssessmentDate { get; set; }
}
