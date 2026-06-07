using GameGuild.CQRS;

namespace GameGuild.Content.Pages;

public sealed record PublishPageCommand(Guid PageId, Guid PublishedByUserId) : ICommand<PageDto?>;
