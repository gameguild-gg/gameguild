namespace GameGuild.Commerce.Payments;

public sealed record DeductFundsRequest
{
    public required Guid UserId { get; init; }

    public required decimal Amount { get; init; }

    public required string Description { get; init; }

    public string? ReferenceId { get; init; }
}
