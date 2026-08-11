using GameGuild.CQRS;

namespace GameGuild.Economy.Payouts.Queries;

public sealed record EconomyPayoutOperationDto(
    Guid Id,
    long HardCoinUnits,
    PayoutOperationState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ListMyPayoutOperationsQuery(Guid PayeeId, int Take)
    : IQuery<IReadOnlyList<EconomyPayoutOperationDto>>;

public sealed record GetMyPayoutOperationQuery(Guid PayeeId, Guid OperationId)
    : IQuery<EconomyPayoutOperationDto?>;

public sealed class ListMyPayoutOperationsQueryHandler(IPayoutOperationStore operations)
    : IQueryHandler<ListMyPayoutOperationsQuery, IReadOnlyList<EconomyPayoutOperationDto>>
{
    public Task<IReadOnlyList<EconomyPayoutOperationDto>> Handle(
        ListMyPayoutOperationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<EconomyPayoutOperationDto> result = operations
            .ListForPayee(request.PayeeId, request.Take)
            .Select(ToDto)
            .ToArray();
        return Task.FromResult(result);
    }

    private static EconomyPayoutOperationDto ToDto(PayoutOperation operation) => new(
        operation.Id,
        operation.Amount.Units,
        operation.State,
        operation.CreatedAt,
        operation.UpdatedAt);
}

public sealed class GetMyPayoutOperationQueryHandler(IPayoutOperationStore operations)
    : IQueryHandler<GetMyPayoutOperationQuery, EconomyPayoutOperationDto?>
{
    public Task<EconomyPayoutOperationDto?> Handle(
        GetMyPayoutOperationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var operation = operations.Get(request.OperationId);
            if (operation.PayeeId != request.PayeeId)
                return Task.FromResult<EconomyPayoutOperationDto?>(null);

            return Task.FromResult<EconomyPayoutOperationDto?>(new EconomyPayoutOperationDto(
                operation.Id,
                operation.Amount.Units,
                operation.State,
                operation.CreatedAt,
                operation.UpdatedAt));
        }
        catch (KeyNotFoundException)
        {
            return Task.FromResult<EconomyPayoutOperationDto?>(null);
        }
    }
}
