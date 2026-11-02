using System.Buffers.Binary;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GameGuild.API.Database;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GameGuild.API.Core.Filters;

/// <summary>Shared account and source-IP admission for all password sign-in entry points.</summary>
public sealed class PasswordSignInAdmissionService(
    ApplicationDbContext database,
    AuthenticationSecurityOptions options,
    IUserEnumerationProtectionService protection,
    ILogger<PasswordSignInAdmissionService> logger,
    IAccountLockoutAlertPublisher? lockoutAlertPublisher = null,
    GameGuild.Identity.Users.IUserRepository? userRepository = null,
    ITurnstileVerificationService? turnstileVerification = null) : IPasswordSignInAdmissionService
{
    private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromHours(1);

    /// <summary>
    ///     A lockout alert (issue #1083) is emitted only while the lockout is fresh: an
    ///     attack that keeps retrying during the lockout window must not spam the owner
    ///     on every denied attempt. Fresh means the lockout end is (nearly) the full
    ///     duration away, i.e. the threshold-crossing failure just happened.
    /// </summary>
    private static readonly TimeSpan LockoutAlertFreshnessTolerance = TimeSpan.FromMinutes(1);

    private static readonly SemaphoreSlim[] InProcessLockStripes = Enumerable.Range(0, 128)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    public async Task<IAsyncDisposable> AdmitAsync(
        string identifier, HttpContext? context, AuthenticationTimingOrigin timingOrigin,
        CancellationToken cancellationToken, string? turnstileToken = null)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = identifier.ToLowerInvariant();
        var remoteIpAddress = context?.Connection.RemoteIpAddress;
        var normalizedIpAddress = remoteIpAddress is null ? null
            : remoteIpAddress.IsIPv4MappedToIPv6
                ? remoteIpAddress.MapToIPv4().ToString()
                : remoteIpAddress.ToString();
        if (context is not null)
        {
            context.Response.Headers.CacheControl = "no-store";
        }

        var lease = await TryAcquireLockAsync(database, normalizedEmail,
            options.EnableIpThrottling ? normalizedIpAddress : null, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            throw await CreateDenialAsync(timingOrigin, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var now = DateTime.UtcNow;
            var turnstileThreshold = options.TurnstileRequiredAfterFailedAttempts;
            if ((options.EnableIpThrottling || turnstileThreshold > 0) && IPAddress.TryParse(normalizedIpAddress, out _))
            {
                // One query serves both the hourly throttle ceiling and the (lower)
                // Turnstile threshold (issue #1082); each check reads the same rows.
                var failureProbeLimit = options.EnableIpThrottling
                    ? Math.Max(options.MaxAttemptsPerIpPerHour, turnstileThreshold)
                    : turnstileThreshold;
                var recentIpFailures = await database.Set<AuthenticationAttempt>()
                    .Where(attempt => attempt.IpAddress == normalizedIpAddress &&
                        !attempt.IsSuccessful && attempt.AttemptedAt >= now.Subtract(FailedAttemptWindow))
                    .OrderByDescending(attempt => attempt.AttemptedAt)
                    .Select(attempt => attempt.AttemptedAt)
                    .Take(failureProbeLimit)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (options.EnableIpThrottling && recentIpFailures.Count >= options.MaxAttemptsPerIpPerHour)
                {
                    logger.LogWarning("Password sign-in throttled after reaching the source-IP hourly failure limit");
                    throw await CreateDenialAsync(timingOrigin, cancellationToken).ConfigureAwait(false);
                }
                if (turnstileThreshold > 0 && recentIpFailures.Count >= turnstileThreshold)
                {
                    await RequireTurnstileAsync(turnstileToken, normalizedIpAddress!, timingOrigin, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            var recentFailures = await database.Set<AuthenticationAttempt>()
                .Where(attempt => attempt.Email == normalizedEmail &&
                    !attempt.IsSuccessful && attempt.AttemptedAt >= now.Subtract(FailedAttemptWindow))
                .OrderByDescending(attempt => attempt.AttemptedAt)
                .Select(attempt => attempt.AttemptedAt)
                .Take(options.MaxFailedAttemptsPerHour)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var lockoutEndsAt = recentFailures.Count >= options.MaxFailedAttemptsPerHour
                ? recentFailures[0].AddMinutes(options.AccountLockoutDurationMinutes)
                : DateTime.MinValue;
            if (lockoutEndsAt > now)
            {
                var identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
                logger.LogWarning("Password sign-in lockout enforced for identifier hash {IdentifierHash} until {LockoutEndsAtUtc}",
                    identifierHash, lockoutEndsAt);
                await PublishLockoutAlertAsync(normalizedEmail, lockoutEndsAt, now, cancellationToken).ConfigureAwait(false);
                throw await CreateDenialAsync(timingOrigin, cancellationToken).ConfigureAwait(false);
            }

            // The caller owns the lease through credential work and durable attempt/session recording.
            return lease;
        }
        catch
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    ///     Enforces the Turnstile challenge (issue #1082) once the source IP crossed the
    ///     configured failed-attempt threshold: the request must present a token that
    ///     verifies server-side. A missing or rejected token denies the request with a
    ///     captcha-specific marker; a missing verification service skips the challenge
    ///     (hosts that do not register the verifier keep the pre-existing behavior).
    /// </summary>
    private async Task RequireTurnstileAsync(
        string? turnstileToken, string normalizedIpAddress,
        AuthenticationTimingOrigin timingOrigin, CancellationToken cancellationToken)
    {
        if (turnstileVerification is null)
        {
            return;
        }

        TurnstileVerificationResult verification;
        try
        {
            verification = await turnstileVerification.VerifyAsync(turnstileToken, normalizedIpAddress, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Verification infrastructure failure must not become a 500 on the sign-in path.
            logger.LogError(exception, "Turnstile verification threw for source IP {IpAddress}; denying conservatively",
                normalizedIpAddress);
            verification = TurnstileVerificationResult.VerifyFailed;
        }

        if (verification is not { Success: true })
        {
            logger.LogWarning(
                "Password sign-in CAPTCHA challenge enforced for source IP {IpAddress} ({Reason})",
                normalizedIpAddress, verification.Reason ?? "unknown");
            // Timing compensation keeps the captcha denial consistent with the generic denial.
            if (protection is IAuthenticationTimingProtection timingProtection)
            {
                await timingProtection.CompleteAuthenticationTimingAsync(timingOrigin, false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await protection.AddTimingProtectionDelayAsync(false, timingOrigin.StartedAtUtc).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            throw new PasswordSignInCaptchaRequiredException();
        }
    }

    /// <summary>
    ///     Notifies the account owner that the lockout policy engaged (issue #1083), but
    ///     only when the lockout just began (see <see cref="LockoutAlertFreshnessTolerance"/>)
    ///     so a sustained attack does not email the owner on every denied attempt. The
    ///     notification is recorded durably by the module publisher and delivered by the
    ///     host-side notification bridge; every failure here is swallowed because the
    ///     sign-in denial itself must not depend on the alert path.
    /// </summary>
    private async Task PublishLockoutAlertAsync(
        string normalizedEmail, DateTime lockoutEndsAt, DateTime now, CancellationToken cancellationToken)
    {
        if (lockoutAlertPublisher is null || userRepository is null)
        {
            return;
        }

        var lockoutDuration = TimeSpan.FromMinutes(options.AccountLockoutDurationMinutes);
        if (lockoutEndsAt - now < lockoutDuration.Subtract(LockoutAlertFreshnessTolerance))
        {
            // The lockout is not fresh: the threshold-crossing failure happened more than
            // the tolerance ago, so the owner was (or will be) alerted by the request that
            // first observed the fresh lockout.
            return;
        }

        try
        {
            var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
            // Unknown and deleted accounts have no owner to notify; their lockouts still deny.
            if (user is null || user.IsDeleted)
            {
                return;
            }

            await lockoutAlertPublisher.RecordAsync(
                user.Id, lockoutEndsAt.ToUniversalTime(), options.MaxFailedAttemptsPerHour, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Could not queue the account-lockout owner notification for identifier hash {IdentifierHash}",
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail))));
        }
    }

    private async Task<UnauthorizedAccessException> CreateDenialAsync(
        AuthenticationTimingOrigin origin, CancellationToken cancellationToken)
    {
        if (protection is IAuthenticationTimingProtection timingProtection)
        {
            await timingProtection.CompleteAuthenticationTimingAsync(origin, false, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await protection.AddTimingProtectionDelayAsync(false, origin.StartedAtUtc).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new UnauthorizedAccessException(protection.GetGenericErrorMessage("login"));
    }

    private static async Task<IAsyncDisposable?> TryAcquireLockAsync(
        ApplicationDbContext database,
        string normalizedEmail,
        string? normalizedIpAddress,
        CancellationToken cancellationToken)
    {
        var lockKeys = new List<long> { CreateEmailLockKey(normalizedEmail) };
        if (normalizedIpAddress is not null)
        {
            lockKeys.Add(CreateIpLockKey(normalizedIpAddress));
        }

        lockKeys.Sort();

        if (!database.Database.IsRelational())
        {
            var stripes = lockKeys
                .Select(GetLockStripeIndex)
                .Distinct()
                .OrderBy(index => index)
                .Select(index => InProcessLockStripes[index])
                .ToArray();
            var acquiredStripes = new List<SemaphoreSlim>(stripes.Length);

            try
            {
                foreach (var stripe in stripes)
                {
                    await stripe.WaitAsync(cancellationToken).ConfigureAwait(false);
                    acquiredStripes.Add(stripe);
                }

                return new InProcessLockLease(acquiredStripes.ToArray());
            }
            catch
            {
                for (var index = acquiredStripes.Count - 1; index >= 0; index--)
                {
                    acquiredStripes[index].Release();
                }

                throw;
            }
        }

        if (!database.Database.IsNpgsql())
        {
            throw new InvalidOperationException(
                "Account lockout requires PostgreSQL advisory locks for relational database providers.");
        }

        await database.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var connection = database.Database.GetDbConnection();
        var acquiredKeys = new List<long>(lockKeys.Count);

        try
        {
            foreach (var lockKey in lockKeys)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT pg_try_advisory_lock(@lock_key)";
                command.Transaction = database.Database.CurrentTransaction?.GetDbTransaction();

                var parameter = command.CreateParameter();
                parameter.ParameterName = "lock_key";
                parameter.DbType = DbType.Int64;
                parameter.Value = lockKey;
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result is not true)
                {
                    await ReleasePostgreSqlLocksAsync(database, acquiredKeys).ConfigureAwait(false);
                    await database.Database.CloseConnectionAsync().ConfigureAwait(false);
                    return null;
                }

                acquiredKeys.Add(lockKey);
            }

            return new PostgreSqlLockLease(database, acquiredKeys.ToArray());
        }
        catch
        {
            try
            {
                await ReleasePostgreSqlLocksAsync(database, acquiredKeys).ConfigureAwait(false);
            }
            catch
            {
                // Clearing the connection pool below releases any session advisory locks.
            }

            if (connection is NpgsqlConnection npgsqlConnection)
            {
                NpgsqlConnection.ClearPool(npgsqlConnection);
            }

            await database.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static int GetLockStripeIndex(long lockKey) =>
        (int)((ulong)lockKey % (uint)InProcessLockStripes.Length);

    private static long CreateEmailLockKey(string normalizedEmail)
    {
        var scopedIdentifier = $"gameguild:auth:local-sign-in-lockout:v1:{normalizedEmail}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(scopedIdentifier));
        return BinaryPrimitives.ReadInt64BigEndian(digest);
    }

    private static long CreateIpLockKey(string normalizedIpAddress)
    {
        var scopedIdentifier = $"gameguild:auth:local-sign-in-lockout:ip:v1:{normalizedIpAddress}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(scopedIdentifier));
        return BinaryPrimitives.ReadInt64BigEndian(digest);
    }

    private static async Task ReleasePostgreSqlLocksAsync(ApplicationDbContext database, IReadOnlyList<long> lockKeys)
    {
        var connection = database.Database.GetDbConnection();

        for (var index = lockKeys.Count - 1; index >= 0; index--)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_advisory_unlock(@lock_key)";
            command.Transaction = database.Database.CurrentTransaction?.GetDbTransaction();

            var parameter = command.CreateParameter();
            parameter.ParameterName = "lock_key";
            parameter.DbType = DbType.Int64;
            parameter.Value = lockKeys[index];
            command.Parameters.Add(parameter);

            var unlocked = await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
            if (unlocked is not true)
            {
                throw new InvalidOperationException("The PostgreSQL sign-in lock was not held at release time.");
            }
        }
    }

    private sealed class InProcessLockLease(SemaphoreSlim[] semaphores) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            for (var index = semaphores.Length - 1; index >= 0; index--)
            {
                semaphores[index].Release();
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class PostgreSqlLockLease(ApplicationDbContext database, long[] lockKeys) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            var connection = database.Database.GetDbConnection();

            try
            {
                await ReleasePostgreSqlLocksAsync(database, lockKeys).ConfigureAwait(false);
            }
            catch
            {
                if (connection is NpgsqlConnection npgsqlConnection)
                {
                    NpgsqlConnection.ClearPool(npgsqlConnection);
                }

                throw;
            }
            finally
            {
                await database.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
    }

}
