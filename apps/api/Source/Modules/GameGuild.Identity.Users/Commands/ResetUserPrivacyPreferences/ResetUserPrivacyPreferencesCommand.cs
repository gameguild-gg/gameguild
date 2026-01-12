using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ResetUserPrivacyPreferencesCommand(Guid UserId) : ICommand;
