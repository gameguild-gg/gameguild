namespace GameGuild;

/// <summary>
///     Validation error with message and optional property name
/// </summary>
public record ValidationError(string Message, string? PropertyName = null)
{
    public string FullMessage
    {
        get => string.IsNullOrEmpty(PropertyName) ? Message : $"{PropertyName}: {Message}";
    }
}
