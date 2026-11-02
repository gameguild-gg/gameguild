using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Micro-deposit verification challenge for a saved bank-account payment method
///     (issue #414). Two small deposits (each $0.01–$0.99) are generated when the challenge
///     is initiated; only their salted SHA-256 hashes are stored, so the expected amounts are
///     never recoverable from the database. The owner must echo both amounts within
///     <see cref="BankAccountVerificationRules.VerificationBusinessDays"/> business days and
///     at most <see cref="BankAccountVerificationRules.MaxFailedAttempts"/> failed attempts;
///     afterwards a new challenge must be initiated. A bank account becomes chargeable only
///     once its latest challenge is verified (enforced by
///     <see cref="PaymentMethodChargeResolver"/>).
/// </summary>
[Table("bank_account_verifications")]
public class BankAccountVerification : EntityBase
{
    /// <summary>Parameterless constructor for EF Core.</summary>
    private BankAccountVerification()
    {
    }

    /// <summary>Creates an open (unverified) challenge with hashed deposit amounts.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="paymentMethodId">Bank-account payment method being verified.</param>
    /// <param name="deposit1Hash">Salted SHA-256 hash of the first deposit amount.</param>
    /// <param name="deposit2Hash">Salted SHA-256 hash of the second deposit amount.</param>
    /// <param name="salt">Per-challenge random salt (hex) used by the hashes.</param>
    /// <param name="initiatedAtUtc">UTC instant the two deposits were generated.</param>
    public BankAccountVerification(
        Guid? tenantId,
        Guid paymentMethodId,
        string deposit1Hash,
        string deposit2Hash,
        string salt,
        DateTime initiatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deposit1Hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(deposit2Hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        TenantId = tenantId;
        PaymentMethodId = paymentMethodId;
        Deposit1Hash = deposit1Hash;
        Deposit2Hash = deposit2Hash;
        Salt = salt;
        InitiatedAtUtc = initiatedAtUtc;
        ExpiresAtUtc = BankAccountVerificationRules.DeadlineFromInitiation(initiatedAtUtc);
    }

    /// <summary>Tenant the verified payment method belongs to.</summary>
    public override Guid? TenantId { get; set; }

    /// <summary>Bank-account payment method this challenge verifies.</summary>
    public Guid PaymentMethodId { get; private set; }

    /// <summary>Salted SHA-256 hash (hex) of the first deposit amount.</summary>
    [Required]
    [MaxLength(64)]
    public string Deposit1Hash { get; private set; } = string.Empty;

    /// <summary>Salted SHA-256 hash (hex) of the second deposit amount.</summary>
    [Required]
    [MaxLength(64)]
    public string Deposit2Hash { get; private set; } = string.Empty;

    /// <summary>Per-challenge random salt (hex) used when hashing the amounts.</summary>
    [Required]
    [MaxLength(64)]
    public string Salt { get; private set; } = string.Empty;

    /// <summary>Failed echo attempts against this challenge so far.</summary>
    public int FailedAttempts { get; private set; }

    /// <summary>UTC instant the two deposits were generated (challenge opened).</summary>
    public DateTime InitiatedAtUtc { get; private set; }

    /// <summary>UTC deadline (3 business days) after which the challenge can no longer be answered.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>UTC instant the owner echoed both amounts correctly; null until verified.</summary>
    public DateTime? VerifiedAtUtc { get; private set; }

    /// <summary>Whether the challenge has been answered correctly.</summary>
    public bool IsVerified => VerifiedAtUtc.HasValue;

    /// <summary>Whether the challenge can still be answered at <paramref name="utcNow"/>.</summary>
    public bool IsOpen(DateTime utcNow) => !IsVerified && utcNow <= ExpiresAtUtc;

    /// <summary>Whether the challenge is exhausted (locked) by failed attempts.</summary>
    public bool IsLocked => FailedAttempts >= BankAccountVerificationRules.MaxFailedAttempts;

    /// <summary>Records one failed echo attempt.</summary>
    public void RecordFailedAttempt() => FailedAttempts++;

    /// <summary>Marks the challenge verified; idempotent.</summary>
    /// <param name="utcNow">UTC instant of the successful echo.</param>
    public void MarkVerified(DateTime utcNow)
    {
        if (!IsVerified)
        {
            VerifiedAtUtc = utcNow;
        }
    }
}

/// <summary>
///     Micro-deposit verification math and limits (issue #414): amount bounds, challenge
///     generation with a cryptographically random salt, salted hashing, attempt cap and the
///     3-business-day deadline (weekends skipped).
/// </summary>
public static class BankAccountVerificationRules
{
    /// <summary>Smallest micro-deposit amount (USD).</summary>
    public const decimal MinimumAmount = 0.01m;

    /// <summary>Largest micro-deposit amount (USD).</summary>
    public const decimal MaximumAmount = 0.99m;

    /// <summary>Smallest micro-deposit amount in integer cents.</summary>
    private const int MinimumCents = 1;

    /// <summary>Largest micro-deposit amount in integer cents.</summary>
    private const int MaximumCents = 99;

    /// <summary>Failed echo attempts allowed before the challenge locks.</summary>
    public const int MaxFailedAttempts = 3;

    /// <summary>Business days the owner has to echo the two amounts.</summary>
    public const int VerificationBusinessDays = 3;

    /// <summary>
    ///     Generates a fresh challenge: two distinct random deposit amounts within
    ///     [<see cref="MinimumAmount"/>, <see cref="MaximumAmount"/>] and a random salt.
    /// </summary>
    /// <returns>(first amount, second amount, salt hex).</returns>
    public static (decimal Deposit1, decimal Deposit2, string Salt) GenerateChallenge()
    {
        var first = RandomNumberGenerator.GetInt32(MinimumCents, MaximumCents + 1);
        var second = RandomNumberGenerator.GetInt32(MinimumCents, MaximumCents + 1);
        while (second == first)
        {
            second = RandomNumberGenerator.GetInt32(MinimumCents, MaximumCents + 1);
        }

        var saltBytes = RandomNumberGenerator.GetBytes(16);
        return (first / 100m, second / 100m, Convert.ToHexString(saltBytes));
    }

    /// <summary>Salted SHA-256 hash (lowercase hex) of a normalized deposit amount.</summary>
    /// <param name="salt">Challenge salt (hex).</param>
    /// <param name="amount">Deposit amount; normalized to two decimals before hashing.</param>
    public static string HashDeposit(string salt, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);
        var normalized = NormalizeAmount(amount).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var payload = $"{salt}:{normalized}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Whether an echoed amount is inside the representable deposit range.</summary>
    public static bool IsPlausibleAmount(decimal amount) =>
        amount >= MinimumAmount && amount <= MaximumAmount;

    /// <summary>Two-decimal normalization of an echoed deposit amount.</summary>
    public static decimal NormalizeAmount(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    ///     Deadline for a challenge initiated at <paramref name="initiatedAtUtc"/>: three
    ///     business days later, skipping Saturdays and Sundays.
    /// </summary>
    public static DateTime DeadlineFromInitiation(DateTime initiatedAtUtc)
    {
        var deadline = initiatedAtUtc;
        var added = 0;
        while (added < VerificationBusinessDays)
        {
            deadline = deadline.AddDays(1);
            if (deadline.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                added++;
            }
        }

        return deadline;
    }
}
