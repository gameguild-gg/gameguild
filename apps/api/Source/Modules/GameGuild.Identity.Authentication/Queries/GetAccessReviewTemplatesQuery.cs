using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAccessReviewTemplatesQuery : IQuery<IEnumerable<AccessReviewTemplateDto>> { }
