using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record UpdateUserPrivacyPreferencesCommand(Guid UserId, UpdateUserPrivacyPreferencesRequest Request) : ICommand;
