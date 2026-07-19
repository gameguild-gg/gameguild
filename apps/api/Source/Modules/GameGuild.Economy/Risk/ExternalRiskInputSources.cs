namespace GameGuild.Economy.Risk;

public interface IFinancialCrimeRiskInputSource
{
    ValueTask<FinancialCrimeRiskInput> ReadAsync(
        string opaqueSubjectReference,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}

public interface ITrustSafetyRiskInputSource
{
    ValueTask<TrustSafetyRiskInput> ReadAsync(
        string opaqueSubjectReference,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
