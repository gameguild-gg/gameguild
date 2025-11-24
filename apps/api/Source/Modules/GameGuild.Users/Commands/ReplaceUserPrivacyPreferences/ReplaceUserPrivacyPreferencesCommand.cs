using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

public record ReplaceUserPrivacyPreferencesCommand(Guid UserId, ReplaceUserPrivacyPreferencesRequest Request) : ICommand;
