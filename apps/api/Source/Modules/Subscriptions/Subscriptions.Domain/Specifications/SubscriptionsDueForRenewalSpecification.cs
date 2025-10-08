using GameGuild.Shared.Abstractions;
using GameGuild.Modules.Subscriptions.Models;
﻿using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions due for renewal
/// </summary>
public class SubscriptionsDueForRenewalSpecification : Specification<Subscription>
{
    public SubscriptionsDueForRenewalSpecification(DateTime beforeDate) : base(s =>
        s.Status == SubscriptionStatus.Active && s.NextBillingDate <= beforeDate
    )
    {
        ApplyOrderBy(s => s.NextBillingDate);
    }
}

