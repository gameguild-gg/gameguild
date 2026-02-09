using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ReplaceUserPrivacyPreferencesCommand(Guid UserId, ReplaceUserPrivacyPreferencesRequest Request) : ICommand;
