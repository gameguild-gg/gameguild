namespace GameGuild.Common;

/// <summary>
/// System date time provider implementation
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
