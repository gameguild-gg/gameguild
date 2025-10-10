using GameGuild.Modules.Subscriptions.Models;
﻿using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by status
/// </summary>
public class SubscriptionsByStatusSpecification : Specification<Subscription>
{
    public SubscriptionsByStatusSpecification(SubscriptionStatus status) : base(s => s.Status == status) { ApplyOrderBy(s => s.NextBillingDate); }
}

