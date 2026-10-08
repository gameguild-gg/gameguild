using System.Buffers.Binary;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GameGuild.API.Database;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GameGuild.API.Core.Filters;

/// <summary>
///     Applies the configured account lockout policy before local password sign-in.
/// </summary>
public sealed class AuthenticationLockoutActionFilter : IAsyncActionFilter
{
    private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromHours(1);
    private static readonly SemaphoreSlim[] InProcessLockStripes = Enumerable.Range(0, 128)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!TryGetLocalSignInEmail(context, out var email))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;
        cancellationToken.ThrowIfCancellationRequested();
        var timingOrigin = AuthenticationTimingOrigin.GetOrStartForRequest(
            context.HttpContext, services.GetService<TimeProvider>());
        var options = services.GetRequiredService<AuthenticationSecurityOptions>();
        var database = services.GetRequiredService<IApplicationDbContext>();
        var normalizedEmail = email.ToLowerInvariant();
        var remoteIpAddress = context.HttpContext.Connection.RemoteIpAddress;
        var normalizedIpAddress = remoteIpAddress is null
            ? null
            : remoteIpAddress.IsIPv4MappedToIPv6
                ? remoteIpAddress.MapToIPv4().ToString()
                : remoteIpAddress.ToString();
        context.HttpContext.Response.Headers.CacheControl = "no-store";

        var lockoutLock = await TryAcquireLockAsync(
                services.GetRequiredService<ApplicationDbContext>(),
                normalizedEmail,
                options.EnableIpThrottling ? normalizedIpAddress : null,
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (lockoutLock is null)
        {
            context.Result = await CreateUnauthorizedResultAsync(services, timingOrigin, cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var heldLock = lockoutLock;
        var now = DateTime.UtcNow;

        if (options.EnableIpThrottling && IPAddress.TryParse(normalizedIpAddress, out _))
        {
            // A source-IP budget is shared across account identifiers. Forwarded addresses are
            // already normalized by ForwardedHeadersMiddleware and only accepted from trusted proxies.
            var recentIpFailures = await database.Set<AuthenticationAttempt>()
                .Where(attempt => attempt.IpAddress == normalizedIpAddress &&
                                  !attempt.IsSuccessful &&
                                  attempt.AttemptedAt >= now.Subtract(FailedAttemptWindow))
                .OrderByDescending(attempt => attempt.AttemptedAt)
                .Select(attempt => attempt.AttemptedAt)
                .Take(options.MaxAttemptsPerIpPerHour)
                .ToListAsync(context.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (recentIpFailures.Count >= options.MaxAttemptsPerIpPerHour)
            {
                services.GetService<ILogger<AuthenticationLockoutActionFilter>>()?.LogWarning(
                    "Local sign-in throttled for source IP after reaching the configured hourly failure limit");

                context.Result = await CreateUnauthorizedResultAsync(services, timingOrigin, cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        // Bound the rows read to the threshold. The stored attempt history is shared across API
        // instances, so failures are not reset by load balancing or process restart.
        var recentFailures = await database.Set<AuthenticationAttempt>()
            .Where(attempt => attempt.Email == normalizedEmail &&
                              !attempt.IsSuccessful &&
                              attempt.AttemptedAt >= now.Subtract(FailedAttemptWindow))
            .OrderByDescending(attempt => attempt.AttemptedAt)
            .Select(attempt => attempt.AttemptedAt)
            .Take(options.MaxFailedAttemptsPerHour)
            .ToListAsync(context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var lockoutEndsAt = recentFailures.Count >= options.MaxFailedAttemptsPerHour
            ? recentFailures[0].AddMinutes(options.AccountLockoutDurationMinutes)
            : DateTime.MinValue;

        if (lockoutEndsAt > now)
        {
            // Keep the response indistinguishable from invalid credentials so this check does
            // not expose whether an account exists or is currently locked.
            var identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
            services.GetService<ILogger<AuthenticationLockoutActionFilter>>()?.LogWarning(
                "Local sign-in lockout enforced for identifier hash {IdentifierHash} until {LockoutEndsAtUtc}",
                identifierHash,
                lockoutEndsAt);

            context.Result = await CreateUnauthorizedResultAsync(services, timingOrigin, cancellationToken).ConfigureAwait(false);
            return;
        }

        await next().ConfigureAwait(false);
    }

    private static async Task<UnauthorizedObjectResult> CreateUnauthorizedResultAsync(
        IServiceProvider services, AuthenticationTimingOrigin origin, CancellationToken cancellationToken)
    {
        // Admission denial has performed no password verification. Preserve the denial while
        // completing the same real credential work and total request floor as invalid credentials.
        var protection = services.GetRequiredService<IUserEnumerationProtectionService>();
        if (protection is IAuthenticationTimingProtection timingProtection)
        {
            await timingProtection.CompleteAuthenticationTimingAsync(origin, false, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await protection.AddTimingProtectionDelayAsync(false, origin.StartedAtUtc).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new UnauthorizedObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = protection.GetGenericErrorMessage("login")
        });
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

    private static bool TryGetLocalSignInEmail(ActionExecutingContext context, out string email)
    {
        email = string.Empty;

        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor ||
            descriptor.ControllerTypeInfo.AsType() != typeof(AuthController) ||
            !string.Equals(descriptor.ActionName, nameof(AuthController.LocalSignIn), StringComparison.Ordinal))
        {
            return false;
        }

        var request = context.ActionArguments.Values.OfType<LocalSignInRequest>().FirstOrDefault();
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            return false;
        }

        email = request.Email;
        return true;
    }
}
