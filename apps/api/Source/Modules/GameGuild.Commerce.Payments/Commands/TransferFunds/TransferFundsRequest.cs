namespace GameGuild.Commerce.Payments;

public abstract record TransferFundsRequest
{
    public required Guid FromUserId { get; init; }

    public required Guid ToUserId { get; init; }

    public required decimal Amount { get; init; }

    public required string Description { get; init; }

    public string? ReferenceId { get; init; }
}
