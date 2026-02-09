using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record ConfigureReminderSettingsCommand : ICommand<bool>
{
    public Guid TenantId { get; init; }

    public int ReminderFrequencyDays { get; init; }

    public bool EnableAutoReminders { get; init; }
}
