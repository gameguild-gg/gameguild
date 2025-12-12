using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

public record ResetUserPrivacyPreferencesCommand(Guid UserId) : ICommand;
