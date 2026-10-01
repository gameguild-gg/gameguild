using System.Buffers.Binary;
using System.Data;
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
        var options = services.GetRequiredService<AuthenticationSecurityOptions>();
        var database = services.GetRequiredService<IApplicationDbContext>();
        var normalizedEmail = email.ToLowerInvariant();
        context.HttpContext.Response.Headers.CacheControl = "no-store";

        var lockoutLock = await TryAcquireLockAsync(
                services.GetRequiredService<ApplicationDbContext>(),
                normalizedEmail,
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (lockoutLock is null)
        {
            context.Result = CreateUnauthorizedResult();
            return;
        }

        await using var heldLock = lockoutLock;
        var now = DateTime.UtcNow;

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

            context.Result = CreateUnauthorizedResult();
            return;
        }

        await next().ConfigureAwait(false);
    }

    private static UnauthorizedObjectResult CreateUnauthorizedResult() => new(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Authentication failed",
        Detail = "The email or password is incorrect."
    });

    private static async Task<IAsyncDisposable?> TryAcquireLockAsync(
        ApplicationDbContext database,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var lockKey = CreateLockKey(normalizedEmail);

        if (!database.Database.IsRelational())
        {
            var stripe = InProcessLockStripes[(int)((ulong)lockKey % (uint)InProcessLockStripes.Length)];
            await stripe.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new InProcessLockLease(stripe);
        }

        if (!database.Database.IsNpgsql())
        {
            throw new InvalidOperationException(
                "Account lockout requires PostgreSQL advisory locks for relational database providers.");
        }

        await database.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var connection = database.Database.GetDbConnection();

        try
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
                await database.Database.CloseConnectionAsync().ConfigureAwait(false);
                return null;
            }

            return new PostgreSqlLockLease(database, lockKey);
        }
        catch
        {
            if (connection is NpgsqlConnection npgsqlConnection)
            {
                NpgsqlConnection.ClearPool(npgsqlConnection);
            }

            await database.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static long CreateLockKey(string normalizedEmail)
    {
        var scopedIdentifier = $"gameguild:auth:local-sign-in-lockout:v1:{normalizedEmail}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(scopedIdentifier));
        return BinaryPrimitives.ReadInt64BigEndian(digest);
    }

    private sealed class InProcessLockLease(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PostgreSqlLockLease(ApplicationDbContext database, long lockKey) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            var connection = database.Database.GetDbConnection();
            var discardPool = false;

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock(@lock_key)";
                command.Transaction = database.Database.CurrentTransaction?.GetDbTransaction();

                var parameter = command.CreateParameter();
                parameter.ParameterName = "lock_key";
                parameter.DbType = DbType.Int64;
                parameter.Value = lockKey;
                command.Parameters.Add(parameter);

                var unlocked = await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
                if (unlocked is not true)
                {
                    throw new InvalidOperationException("The PostgreSQL account lockout advisory lock was not held at release time.");
                }
            }
            catch
            {
                discardPool = true;
                throw;
            }
            finally
            {
                if (discardPool && connection is NpgsqlConnection npgsqlConnection)
                {
                    NpgsqlConnection.ClearPool(npgsqlConnection);
                }

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
