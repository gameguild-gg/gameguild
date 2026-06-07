namespace GameGuild.Commerce.Payments;

/// <summary>Stack behavior types</summary>
public enum StackBehavior
{
    /// <summary>Allow stacking with any promo</summary>
    Allow = 0,

    /// <summary>Deny stacking with any promo</summary>
    Deny = 1,

    /// <summary>Allow stacking only if this promo is first</summary>
    AllowIfFirst = 2,

    /// <summary>Allow stacking only if this promo is last</summary>
    AllowIfLast = 3,

    /// <summary>Only stack with specific promos</summary>
    OnlyWithSpecific = 4,

    /// <summary>Maximum one per type</summary>
    MaxOnePerType = 5
}
