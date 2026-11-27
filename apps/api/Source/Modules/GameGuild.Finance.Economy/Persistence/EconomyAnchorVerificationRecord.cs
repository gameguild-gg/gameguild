namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     One external anchor verification, read back from the unified event
///     ledger (<see cref="EconomyEventStreamKind.AnchorVerification"/>, #1092
///     Tier 3). The ledger's columns are nullable because every other stream
///     leaves them NULL; for this kind the kind-shape check constraint
///     guarantees they are present, so the record hands them out non-nullable
///     under the names the verification readers have always used.
/// </summary>
internal sealed record EconomyAnchorVerificationRecord(
    Guid Id,
    string KeyId,
    string ObjectVersion,
    string ETag,
    DateTimeOffset RetainUntil,
    string ObjectHash,
    bool SignatureValid,
    bool ObjectMatches,
    DateTimeOffset VerifiedAt)
{
    public static EconomyAnchorVerificationRecord From(EconomyEventRow row) =>
        new(row.Id, row.KeyId!, row.ObjectVersion!, row.ETag!, row.ExpiresAt!.Value, row.EvidenceHash!,
            row.SignatureValid!.Value, row.ObjectMatches!.Value, row.OccurredAt);

    /// <summary>The record of a ledger row, or null when there is none —
    ///     an anchor that has not been verified yet has no record.</summary>
    public static EconomyAnchorVerificationRecord? FromOrNull(EconomyEventRow? row) =>
        row is null ? null : From(row);
}
