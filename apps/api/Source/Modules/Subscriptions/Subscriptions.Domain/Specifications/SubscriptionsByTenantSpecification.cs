using GameGuild.Modules.Subscriptions.Models;
﻿using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by tenant
/// </summary>
public class SubscriptionsByTenantSpecification : SpecificationBase<Subscription>
{
    public SubscriptionsByTenantSpecification(Guid tenantId) : base(s => s.TenantId == tenantId) { ApplyOrderByDescending(s => s.CreatedAt); }
}

