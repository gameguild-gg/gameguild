using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record UpdateUserPrivacyPreferencesCommand(Guid UserId, UpdateUserPrivacyPreferencesRequest Request) : ICommand;
