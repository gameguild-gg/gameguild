using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record GetPagedSubscriptionPlansQuery(int Page = 1, int PageSize = 10, bool? IsActive = null, bool? IsFeatured = null, string? SearchTerm = null) : IQuery<IEnumerable<SubscriptionPlan>>;
