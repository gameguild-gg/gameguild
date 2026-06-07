namespace GameGuild.Identity.Authentication;

public abstract class TemplateParameter
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public object? DefaultValue { get; set; }

    public List<string> AllowedValues { get; set; } = new List<string>();
}
