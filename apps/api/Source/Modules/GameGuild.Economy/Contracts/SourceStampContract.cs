namespace GameGuild.Economy.Contracts;

public sealed record SourceStampContract
{
    public SourceStampContract(
        SourceStampId id,
        string evidenceHash,
        SourceConfirmationState state,
        DateTimeOffset observedAt,
        DateTimeOffset? confirmedAt,
        string? providerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceHash);
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (state == SourceConfirmationState.Confirmed && confirmedAt is null)
            throw new ArgumentException("Confirmed source stamps require a confirmation timestamp.", nameof(confirmedAt));
        if (state != SourceConfirmationState.Confirmed && confirmedAt is not null)
            throw new ArgumentException("Only confirmed source stamps may carry a confirmation timestamp.", nameof(confirmedAt));
        if (confirmedAt < observedAt)
            throw new ArgumentException("Confirmation cannot precede observation.", nameof(confirmedAt));

        Id = id;
        EvidenceHash = evidenceHash.Trim();
        State = state;
        ObservedAt = observedAt;
        ConfirmedAt = confirmedAt;
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim();
    }

    public SourceStampId Id { get; }
    public string EvidenceHash { get; }
    public SourceConfirmationState State { get; }
    public DateTimeOffset ObservedAt { get; }
    public DateTimeOffset? ConfirmedAt { get; }
    public string? ProviderReference { get; }
}
