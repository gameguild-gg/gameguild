using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ResetUserPrivacyPreferencesCommand(Guid UserId) : ICommand;
