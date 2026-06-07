namespace GameGuild.Identity.Authentication;

public abstract class PolicyAuditEntry
{
    public Guid Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string Action { get; set; } = string.Empty; // "Created", "Updated", "Evaluated", "Activated", "Deactivated"

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Details { get; set; }

    public bool? EvaluationResult { get; set; }

    public double? EvaluationTime { get; set; }
}
