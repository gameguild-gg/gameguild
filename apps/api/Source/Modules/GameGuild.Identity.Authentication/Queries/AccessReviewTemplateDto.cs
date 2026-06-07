namespace GameGuild.Identity.Authentication;

public abstract class AccessReviewTemplateDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ReviewType { get; set; } = string.Empty;

    public int DefaultDurationDays { get; set; }

    public bool IsActive { get; set; }
}
