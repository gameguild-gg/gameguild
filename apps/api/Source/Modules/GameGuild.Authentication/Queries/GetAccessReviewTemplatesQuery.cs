using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetAccessReviewTemplatesQuery : IQuery<IEnumerable<AccessReviewTemplateDto>> { }
