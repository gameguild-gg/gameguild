namespace GameGuild.Authentication.Commands;

public abstract class ReminderResult
{
    public int TotalReminders { get; set; }

    public int SentCount { get; set; }

    public int FailedCount { get; set; }
}
