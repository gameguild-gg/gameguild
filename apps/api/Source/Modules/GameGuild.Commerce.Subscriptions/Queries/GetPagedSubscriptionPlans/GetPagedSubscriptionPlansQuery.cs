using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetPagedSubscriptionPlansQuery(int Page = 1, int PageSize = 10, bool? IsActive = null, bool? IsFeatured = null, string? SearchTerm = null) : IQuery<IEnumerable<SubscriptionPlan>>;
