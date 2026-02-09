using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetAccessReviewTemplatesQuery : IQuery<IEnumerable<AccessReviewTemplateDto>> { }
