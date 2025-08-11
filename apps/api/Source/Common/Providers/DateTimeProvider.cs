namespace GameGuild.Common;

/// <summary>
/// System date time provider implementation
/// </summary>
public class DateTimeProvider : IDateTimeProvider {
  public DateTime UtcNow {
    get => DateTime.UtcNow;
  }

  public DateTime Now {
    get => DateTime.Now;
  }

  public DateOnly Today {
    get => DateOnly.FromDateTime(DateTime.Today);
  }
}
