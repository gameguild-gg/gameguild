namespace GameGuild.Commerce.Products;

/// <summary>
/// Promo campaign DTO (issue #418 campaign management).
/// </summary>
/// <param name="Id">Campaign id</param>
/// <param name="Name">Display name</param>
/// <param name="Key">The grouping key promo codes reference as <c>CampaignId</c></param>
/// <param name="Description">What the campaign is about</param>
/// <param name="Status">Lifecycle status</param>
/// <param name="StartsAtUtc">Window start (UTC instant; null = unbounded)</param>
/// <param name="EndsAtUtc">Window end (UTC instant; null = unbounded)</param>
/// <param name="StartsAtLocal">Window start rendered in the campaign timezone (null when unbounded)</param>
/// <param name="EndsAtLocal">Window end rendered in the campaign timezone (null when unbounded)</param>
/// <param name="TimeZoneId">IANA timezone the window is expressed in (default UTC)</param>
/// <param name="Budget">Planned spend</param>
/// <param name="AcquisitionSpend">Acquisition spend; numerator of CAC</param>
/// <param name="Currency">Currency of the spend fields</param>
/// <param name="CodeCount">Codes grouped under the campaign key</param>
/// <param name="AttributedCustomers">Customers whose first purchase the campaign tagged</param>
/// <param name="CustomerAcquisitionCost">AcquisitionSpend ÷ attributed customers (null when not computable)</param>
/// <param name="WindowOpen">Whether the campaign window is currently open</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
public sealed record PromoCampaignDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    PromoCampaignStatus Status,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    DateTime? StartsAtLocal,
    DateTime? EndsAtLocal,
    string TimeZoneId,
    decimal? Budget,
    decimal? AcquisitionSpend,
    string Currency,
    int CodeCount,
    int AttributedCustomers,
    decimal? CustomerAcquisitionCost,
    bool WindowOpen,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Admin campaign report row: campaign header plus the rolled-up performance
/// aggregates (issue #418 minimal admin reporting).
/// </summary>
/// <param name="Campaign">The campaign</param>
/// <param name="Performance">Aggregated performance within the requested window</param>
public sealed record PromoCampaignReportEntry(
    PromoCampaignDto Campaign,
    CampaignPerformanceDto Performance);
