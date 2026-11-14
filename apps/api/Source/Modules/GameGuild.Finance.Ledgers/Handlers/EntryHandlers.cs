using GameGuild.CQRS;
using GameGuild.Finance.Ledgers.Abstractions;
using GameGuild.Finance.Ledgers.Commands;
using GameGuild.Finance.Ledgers.Entities;
using GameGuild.Finance.Ledgers.Enums;
using GameGuild.Finance.Ledgers.Models;
using GameGuild.Finance.Ledgers.Queries;

namespace GameGuild.Finance.Ledgers.Handlers;

/// <summary>
/// Maps LedgerEntry entity to LedgerEntryDto with correct parameter order.
/// </summary>
internal static class EntryMapper
{
    public static LedgerEntryDto MapToDto(LedgerEntry entry) => new(
        Id: entry.Id,
        ReferenceNumber: entry.ReferenceNumber,
        LedgerId: entry.LedgerId,
        LedgerCode: entry.Ledger?.Code ?? string.Empty,
        LedgerPath: entry.LedgerPath,
        Type: entry.Type,
        Status: entry.Status,
        Category: entry.Category,
        Amount: entry.Amount,
        CurrencyCode: entry.CurrencyCode,
        OriginalAmount: entry.OriginalAmount,
        OriginalCurrencyCode: entry.OriginalCurrencyCode,
        TransactionDate: entry.TransactionDate,
        PostingDate: entry.PostingDate,
        Description: entry.Description,
        Notes: entry.Notes,
        CounterpartyName: entry.CounterpartyName,
        ExternalReferenceId: entry.ExternalReferenceId,
        TransferPairEntryId: entry.TransferPairEntryId,
        ReversesEntryId: entry.ReversesEntryId,
        ReversedByEntryId: entry.ReversedByEntryId,
        BudgetId: entry.BudgetId,
        BudgetCategoryCode: entry.BudgetCategoryCode,
        Tags: entry.Tags.AsReadOnly(),
        CreatedByUserId: entry.CreatedByUserId,
        CreatedAt: entry.CreatedAt,
        UpdatedAt: entry.UpdatedAt);
}

// ============================================================================
// Entry Command Handlers
// ============================================================================

public class PostEntryHandler(ILedgerEntryService entryService)
    : IRequestHandler<PostEntryCommand, LedgerEntryDto>
{
    public async Task<LedgerEntryDto> Handle(PostEntryCommand request, CancellationToken ct)
    {
        var entry = await entryService.PostEntryAsync(
            request.LedgerId,
            request.Type,
            request.Category,
            request.Amount,
            request.TransactionDate,
            request.Description,
            request.CreatedByUserId,
            request.ExternalReferenceId,
            ct);

        return EntryMapper.MapToDto(entry);
    }
}

public class PostTransferHandler(ILedgerEntryService entryService)
    : IRequestHandler<PostTransferCommand, TransferResult>
{
    public async Task<TransferResult> Handle(PostTransferCommand request, CancellationToken ct)
    {
        var (source, destination) = await entryService.PostTransferAsync(
            request.SourceLedgerId,
            request.DestinationLedgerId,
            request.Amount,
            request.TransactionDate,
            request.Description,
            request.CreatedByUserId,
            ct);

        return new TransferResult(EntryMapper.MapToDto(source), EntryMapper.MapToDto(destination));
    }
}

public class ReverseEntryHandler(ILedgerEntryService entryService)
    : IRequestHandler<ReverseEntryCommand, LedgerEntryDto>
{
    public async Task<LedgerEntryDto> Handle(ReverseEntryCommand request, CancellationToken ct)
    {
        var entry = await entryService.ReverseEntryAsync(
            request.EntryId,
            request.Reason,
            request.ReversedByUserId,
            ct);

        return EntryMapper.MapToDto(entry);
    }
}

public class UpdateEntryHandler(ILedgerEntryService entryService)
    : IRequestHandler<UpdateEntryCommand, LedgerEntryDto>
{
    public async Task<LedgerEntryDto> Handle(UpdateEntryCommand request, CancellationToken ct)
    {
        var entry = await entryService.UpdateEntryAsync(
            request.EntryId,
            request.Description,
            request.Notes,
            request.UpdatedByUserId,
            ct);

        return EntryMapper.MapToDto(entry);
    }
}

public class DeleteEntryHandler(ILedgerEntryService entryService)
    : IRequestHandler<DeleteEntryCommand, bool>
{
    public async Task<bool> Handle(DeleteEntryCommand request, CancellationToken ct)
    {
        await entryService.DeleteEntryAsync(
            request.EntryId,
            request.DeletedByUserId,
            ct);

        return true;
    }
}

public class PostBatchEntriesHandler(ILedgerEntryService entryService)
    : IRequestHandler<PostBatchEntriesCommand, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<IReadOnlyList<LedgerEntryDto>> Handle(PostBatchEntriesCommand request, CancellationToken ct)
    {
        var entries = await entryService.PostBatchAsync(request.Entries, request.CreatedByUserId, ct);
        return entries.Select(EntryMapper.MapToDto).ToList();
    }
}

public class ReconcileEntryHandler(ILedgerEntryService entryService, ILedgerEntryRepository repository)
    : IRequestHandler<ReconcileEntryCommand, LedgerEntryDto>
{
    public async Task<LedgerEntryDto> Handle(ReconcileEntryCommand request, CancellationToken ct)
    {
        await entryService.ReconcileEntryAsync(
            request.EntryId,
            request.ReconciledByUserId,
            request.ExternalReferenceId,
            ct);

        var entry = await repository.GetByIdAsync(request.EntryId, ct)
            ?? throw new InvalidOperationException($"Entry '{request.EntryId}' not found.");

        return EntryMapper.MapToDto(entry);
    }
}

public class ChangeEntryStatusHandler(
    ILedgerEntryService entryService,
    ILedgerEntryRepository repository)
    : IRequestHandler<ChangeEntryStatusCommand, LedgerEntryDto>
{
    public async Task<LedgerEntryDto> Handle(ChangeEntryStatusCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.EntryId, ct)
            ?? throw new InvalidOperationException($"Entry '{request.EntryId}' not found.");

        switch (entry.Status, request.NewStatus)
        {
            case (EntryStatus.Pending, EntryStatus.Posted):
                entry.Post(request.UpdatedByUserId);
                await repository.UpdateAsync(entry, ct);
                break;

            case (EntryStatus.Posted, EntryStatus.Reconciled):
                entry.Reconcile(request.UpdatedByUserId);
                await repository.UpdateAsync(entry, ct);
                break;

            case (EntryStatus.Posted, EntryStatus.Voided) or (EntryStatus.Pending, EntryStatus.Voided):
                // Voiding goes through the reversal service so the paired reversal
                // entry and the posted integration event are both persisted.
                await entryService.ReverseEntryAsync(
                    request.EntryId,
                    request.Reason ?? "Voided via status change",
                    request.UpdatedByUserId,
                    ct);
                entry = await repository.GetByIdAsync(request.EntryId, ct) ?? entry;
                break;

            default:
                throw new InvalidOperationException(
                    $"Cannot change entry status from {entry.Status} to {request.NewStatus}.");
        }

        return EntryMapper.MapToDto(entry);
    }
}

public class RefreshLedgerStatsHandler(ILedgerRollupService rollupService)
    : IRequestHandler<RefreshLedgerStatsCommand, bool>
{
    public async Task<bool> Handle(RefreshLedgerStatsCommand request, CancellationToken ct)
    {
        await rollupService.RefreshLedgerStatsAsync(request.LedgerId, ct);
        return true;
    }
}

public class RefreshSubtreeStatsHandler(ILedgerRollupService rollupService)
    : IRequestHandler<RefreshSubtreeStatsCommand, bool>
{
    public async Task<bool> Handle(RefreshSubtreeStatsCommand request, CancellationToken ct)
    {
        await rollupService.RefreshSubtreeStatsAsync(request.RootLedgerId, ct);
        return true;
    }
}

// ============================================================================
// Entry Query Handlers
// ============================================================================

public class GetEntryByIdHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetEntryByIdQuery, LedgerEntryDto?>
{
    public async Task<LedgerEntryDto?> Handle(GetEntryByIdQuery request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.EntryId, ct);
        return entry is null ? null : EntryMapper.MapToDto(entry);
    }
}

public class GetEntriesByLedgerHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetEntriesByLedgerQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<IReadOnlyList<LedgerEntryDto>> Handle(GetEntriesByLedgerQuery request, CancellationToken ct)
    {
        var entries = await repository.GetByLedgerAsync(
            request.LedgerId,
            request.FromDate,
            request.ToDate,
            ct);

        return entries.Select(EntryMapper.MapToDto).ToList();
    }
}

public class GetEntriesIncludingDescendantsHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetEntriesIncludingDescendantsQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<IReadOnlyList<LedgerEntryDto>> Handle(GetEntriesIncludingDescendantsQuery request, CancellationToken ct)
    {
        var entries = await repository.GetByLedgerIncludingDescendantsAsync(
            request.LedgerId,
            request.FromDate,
            request.ToDate,
            ct);

        return entries.Select(EntryMapper.MapToDto).ToList();
    }
}

public class GetVirtualLedgerEntriesHandler(IVirtualLedgerService virtualLedgerService)
    : IRequestHandler<GetVirtualLedgerEntriesQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<IReadOnlyList<LedgerEntryDto>> Handle(GetVirtualLedgerEntriesQuery request, CancellationToken ct)
    {
        var entries = await virtualLedgerService.GetVirtualEntriesAsync(
            request.VirtualLedgerId,
            request.FromDate,
            request.ToDate,
            request.Skip,
            request.Take,
            ct);

        return entries.Select(EntryMapper.MapToDto).ToList();
    }
}

public class GetEntryByReferenceHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetEntryByReferenceQuery, LedgerEntryDto?>
{
    public async Task<LedgerEntryDto?> Handle(GetEntryByReferenceQuery request, CancellationToken ct)
    {
        var entry = await repository.GetByReferenceAsync(request.TenantId, request.ReferenceNumber, ct);
        return entry is null ? null : EntryMapper.MapToDto(entry);
    }
}

public class SearchEntriesHandler(
    ILedgerEntryRepository repository,
    ILedgerClosureRepository closureRepository)
    : IRequestHandler<SearchEntriesQuery, PagedResult<LedgerEntryDto>>
{
    public async Task<PagedResult<LedgerEntryDto>> Handle(SearchEntriesQuery request, CancellationToken ct)
    {
        IReadOnlyList<Guid> ledgerIds;
        if (request.LedgerId.HasValue)
        {
            ledgerIds = request.IncludeDescendants
                ? await closureRepository.GetDescendantIdsAsync(request.LedgerId.Value, ct: ct)
                : [request.LedgerId.Value];
        }
        else
        {
            ledgerIds = [];
        }

        var matched = await repository.SearchAsync(
            request.TenantId,
            ledgerIds,
            request.SearchTerm,
            request.Type,
            request.Category,
            request.Status,
            request.MinAmount,
            request.MaxAmount,
            request.FromDate,
            request.ToDate,
            ct);

        if (request.Tags is not null)
        {
            var tags = request.Tags.ToList();
            matched = matched.Where(e => tags.Any(tag => e.Tags.Contains(tag))).ToList();
        }

        var totalCount = matched.Count;
        var items = matched
            .OrderByDescending(e => e.TransactionDate)
            .ThenBy(e => e.ReferenceNumber)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(EntryMapper.MapToDto)
            .ToList();

        return new PagedResult<LedgerEntryDto>(items, totalCount, request.Skip, request.Take);
    }
}

// ============================================================================
// Balance Query Handlers
// ============================================================================

public class GetLedgerBalanceHandler(ILedgerRollupService rollupService)
    : IRequestHandler<GetLedgerBalanceQuery, LedgerBalance>
{
    public async Task<LedgerBalance> Handle(GetLedgerBalanceQuery request, CancellationToken ct)
    {
        return await rollupService.GetBalanceAsync(request.LedgerId, request.AsOfDate, ct);
    }
}

public class GetConsolidatedBalanceHandler(ILedgerRollupService rollupService)
    : IRequestHandler<GetConsolidatedBalanceQuery, LedgerBalance>
{
    public async Task<LedgerBalance> Handle(GetConsolidatedBalanceQuery request, CancellationToken ct)
    {
        return await rollupService.GetConsolidatedBalanceAsync(request.LedgerId, request.AsOfDate, ct);
    }
}

public class GetVirtualLedgerBalanceHandler(IVirtualLedgerService virtualLedgerService)
    : IRequestHandler<GetVirtualLedgerBalanceQuery, LedgerBalance>
{
    public async Task<LedgerBalance> Handle(GetVirtualLedgerBalanceQuery request, CancellationToken ct)
    {
        return await virtualLedgerService.GetVirtualBalanceAsync(request.VirtualLedgerId, request.AsOfDate, ct);
    }
}

public class GetHierarchicalRollupHandler(ILedgerRollupService rollupService)
    : IRequestHandler<GetHierarchicalRollupQuery, LedgerRollupNode>
{
    public async Task<LedgerRollupNode> Handle(GetHierarchicalRollupQuery request, CancellationToken ct)
    {
        return await rollupService.GetHierarchicalRollupAsync(
            request.RootLedgerId,
            request.FromDate,
            request.ToDate,
            ct);
    }
}

public class GetCategoryRollupHandler(ILedgerRollupService rollupService)
    : IRequestHandler<GetCategoryRollupQuery, IReadOnlyList<CategoryRollup>>
{
    public async Task<IReadOnlyList<CategoryRollup>> Handle(GetCategoryRollupQuery request, CancellationToken ct)
    {
        return await rollupService.GetCategoryRollupAsync(
            request.LedgerId,
            request.IncludeDescendants,
            request.FromDate,
            request.ToDate,
            ct);
    }
}

public class GetPeriodRollupHandler(ILedgerRollupService rollupService)
    : IRequestHandler<GetPeriodRollupQuery, IReadOnlyList<PeriodRollup>>
{
    public async Task<IReadOnlyList<PeriodRollup>> Handle(GetPeriodRollupQuery request, CancellationToken ct)
    {
        return await rollupService.GetPeriodRollupAsync(
            request.LedgerId,
            request.IncludeDescendants,
            request.FromDate,
            request.ToDate,
            request.Granularity,
            ct);
    }
}
