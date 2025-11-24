namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Example template options class showing the recommended pattern
/// </summary>
public class TemplateOptions : BaseOptions
{
    public string Property1 { get; set; } = string.Empty;

    public bool Property2 { get; set; }

    public int Property3 { get; set; }

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(Property1)) throw new ArgumentException("Property1 cannot be empty", nameof(Property1));

        if (Property3 < 0) throw new ArgumentException("Property3 must be non-negative", nameof(Property3));
    }
}
