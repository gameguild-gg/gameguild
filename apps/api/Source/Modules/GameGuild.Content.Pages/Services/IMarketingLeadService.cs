namespace GameGuild.Content.Pages;

public interface IMarketingLeadService
{
    Task<MarketingLead> CreateAsync(CreateMarketingLeadDto dto, CancellationToken ct = default);
    Task<MarketingLead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MarketingLead>> ListAsync(
        string? source,
        string? status,
        string? topic,
        string? search,
        int skip,
        int take,
        CancellationToken ct = default);
}