namespace GameGuild.Common.Entities;

internal sealed class DateTimeProvider : Abstractions.IDateTimeProvider {
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
