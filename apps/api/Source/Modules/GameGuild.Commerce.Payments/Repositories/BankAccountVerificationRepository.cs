using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Micro-deposit challenge lookup for bank-account verification (issue #414).
/// </summary>
public interface IBankAccountVerificationRepository
{
    /// <summary>
    ///     Loads the latest challenge of a payment method (verified or not), if any challenge
    ///     was ever initiated.
    /// </summary>
    Task<BankAccountVerification?> GetLatestAsync(Guid paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>Loads a specific challenge by id.</summary>
    Task<BankAccountVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new challenge.</summary>
    Task AddAsync(BankAccountVerification verification, CancellationToken cancellationToken = default);

    /// <summary>Persists challenge state changes (attempt counter, verification).</summary>
    Task UpdateAsync(BankAccountVerification verification, CancellationToken cancellationToken = default);
}

/// <summary>
///     EF Core implementation of <see cref="IBankAccountVerificationRepository"/>. Verification
///     rows are immutable history except for the attempt counter and the verification stamp;
///     re-initiation after expiry or lock always writes a new row.
/// </summary>
public class BankAccountVerificationRepository(
    IApplicationDbContext context,
    ILogger<BankAccountVerificationRepository> logger)
    : CommerceRepositoryBase<BankAccountVerification>(context), IBankAccountVerificationRepository
{
    /// <inheritdoc />
    public async Task<BankAccountVerification?> GetLatestAsync(Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting latest bank-account verification for payment method {PaymentMethodId}", paymentMethodId);
        return await Query
            .Where(verification => verification.PaymentMethodId == paymentMethodId)
            .OrderByDescending(verification => verification.InitiatedAtUtc)
            .ThenByDescending(verification => verification.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public new async Task<BankAccountVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query.FirstOrDefaultAsync(verification => verification.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddAsync(BankAccountVerification verification, CancellationToken cancellationToken = default)
    {
        await Context.Set<BankAccountVerification>().AddAsync(verification, cancellationToken).ConfigureAwait(false);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(BankAccountVerification verification, CancellationToken cancellationToken = default)
    {
        Context.Set<BankAccountVerification>().Update(verification);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
