using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ConfigureReminderSettingsCommand : ICommand<bool>
{
    public Guid TenantId { get; init; }

    public int ReminderFrequencyDays { get; init; }

    public bool EnableAutoReminders { get; init; }
}
