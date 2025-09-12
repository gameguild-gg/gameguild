namespace GameGuild;

public class ProblemDetailsOptions
{
    public bool IncludeExceptionDetails { get; set; }

    public string DefaultTitle { get; set; } = "An error occurred";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultTitle))
            throw new InvalidOperationException("Default title cannot be null or empty.");
    }
}
