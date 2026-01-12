using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ReplaceUserPrivacyPreferencesCommand(Guid UserId, ReplaceUserPrivacyPreferencesRequest Request) : ICommand;
