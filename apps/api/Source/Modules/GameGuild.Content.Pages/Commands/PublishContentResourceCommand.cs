using GameGuild.CQRS;

namespace GameGuild.Content.Pages;

public sealed record PublishContentResourceCommand(Guid ResourceId, Guid PublishedByUserId) : ICommand<ContentResourceDto?>;
