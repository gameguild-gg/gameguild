namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicyTemplateDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ConditionType { get; set; } = string.Empty;

    public string Template { get; set; } = string.Empty;

    public List<TemplateParameter> Parameters { get; set; } = new List<TemplateParameter>();

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
