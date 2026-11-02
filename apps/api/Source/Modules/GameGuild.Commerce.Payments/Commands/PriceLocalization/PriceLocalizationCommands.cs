using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static GameGuild.Commerce.Payments.PriceLocalizationAudit;

namespace GameGuild.Commerce.Payments;

/// <summary>Business rule identifiers raised by the price localization commands.</summary>
public static class PriceLocalizationRules
{
    /// <summary>Unknown/unmapped ISO 4217 code supplied.</summary>
    public const string UnknownCurrency = "UnknownCurrency";

    /// <summary>The currency is not enabled for the tenant.</summary>
    public const string CurrencyNotEnabled = "CurrencyNotEnabled";

    /// <summary>The referenced plan does not exist.</summary>
    public const string PlanNotFound = "PlanNotFound";

    /// <summary>Expiry must be a future instant after the effective date.</summary>
    public const string InvalidExpiry = "InvalidLocalizedPriceExpiry";

    /// <summary>Only a finished run can be rolled back.</summary>
    public const string RunNotRollbackable = "RunNotRollbackable";
}

/// <summary>
///     Command (issue #412 spec C): set a manual localized price for a plan in one
///     tenant-enabled currency (psychological pricing, e.g. €8.99 instead of the FX-derived
///     €8.67). Manual rows are never touched by recalculation runs. Optional effective date
///     schedules a future change; optional expiry ends a temporary override, after which the
///     auto-calculated price applies again. Audit-logged.
/// </summary>
/// <param name="PlanId">Plan the localized price applies to.</param>
/// <param name="CurrencyCode">ISO 4217 code (any casing).</param>
/// <param name="LocalizedPrice">Manual monthly price in the target currency; strictly positive.</param>
/// <param name="Reason">Audit reason for the override.</param>
/// <param name="EffectiveDateUtc">Effective instant (UTC); null = immediately.</param>
/// <param name="ExpiryDateUtc">Optional expiry instant (UTC); null = until replaced.</param>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record SetPlanLocalizedPriceCommand(
    Guid PlanId,
    string CurrencyCode,
    decimal LocalizedPrice,
    string Reason,
    DateTime? EffectiveDateUtc = null,
    DateTime? ExpiryDateUtc = null) : ICommand<Guid>;

/// <summary>Validator for <see cref="SetPlanLocalizedPriceCommand"/>.</summary>
public sealed class SetPlanLocalizedPriceCommandValidator : AbstractValidator<SetPlanLocalizedPriceCommand>
{
    /// <summary>Initializes the validator.</summary>
    public SetPlanLocalizedPriceCommandValidator()
    {
        RuleFor(command => command.PlanId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.CurrencyCode)
            .NotEmpty().Length(3).Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must be a three-letter ISO 4217 code.");

        RuleFor(command => command.LocalizedPrice)
            .GreaterThan(0m)
            .WithMessage("Localized price must be strictly positive.");

        RuleFor(command => command.Reason)
            .NotEmpty().MaximumLength(500)
            .WithMessage("An audit reason is required for manual price overrides.");

        RuleFor(command => command.EffectiveDateUtc)
            .Must(instant => instant is null || instant.Value.Kind == DateTimeKind.Utc)
            .WithMessage("Effective instant must be a UTC instant.");

        RuleFor(command => command.ExpiryDateUtc)
            .Must(instant => instant is null || instant.Value.Kind == DateTimeKind.Utc)
            .WithMessage("Expiry instant must be a UTC instant.");
    }
}

/// <summary>Handler for <see cref="SetPlanLocalizedPriceCommand"/>.</summary>
public sealed class SetPlanLocalizedPriceCommandHandler(
    IApplicationDbContext context,
    IPlanPricingResolver planPricingResolver,
    ILocalizedPlanPriceResolver localizedPriceResolver,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<SetPlanLocalizedPriceCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(SetPlanLocalizedPriceCommand request, CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);
        var code = CurrencyConfigurationCommandHelpers.NormalizeOrThrow(request.CurrencyCode);

        if (!await PriceLocalizationCommandHelpers.IsCurrencyEnabledAsync(context, tenantId, code, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleViolationException(
                PriceLocalizationRules.CurrencyNotEnabled,
                $"Currency {code} is not enabled for this tenant; localized prices require an enabled currency.",
                new { tenantId, currencyCode = code });
        }

        if (!await planPricingResolver.PlanExistsAsync(request.PlanId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleViolationException(
                PriceLocalizationRules.PlanNotFound,
                $"Plan {request.PlanId} does not exist; localized prices require an existing plan.",
                new { tenantId, request.PlanId });
        }

        var now = SystemClock.UtcNow;
        var effectiveDate = request.EffectiveDateUtc ?? now;
        if (request.ExpiryDateUtc is { } expiry
            && (expiry.Kind != DateTimeKind.Utc || expiry <= effectiveDate))
        {
            throw new BusinessRuleViolationException(
                PriceLocalizationRules.InvalidExpiry,
                "A localized price expiry must be a UTC instant strictly after the effective instant.");
        }

        // One row per (tenant, plan, currency, effective date): re-setting the same slot
        // replaces the prior row of that date (manual or auto alike).
        var existing = await context.Set<PriceLocalization>()
            .FirstOrDefaultAsync(localization => localization.TenantId == tenantId
                && localization.PlanId == request.PlanId
                && localization.CurrencyCode == code
                && localization.EffectiveDateUtc == effectiveDate, cancellationToken)
            .ConfigureAwait(false);

        string? before = null;
        Guid rowId;
        if (existing is null)
        {
            var entity = new PriceLocalization
            {
                TenantId = tenantId,
                PlanId = request.PlanId,
                CurrencyCode = code,
                LocalizedPrice = request.LocalizedPrice,
                IsAutoCalculated = false,
                EffectiveDateUtc = effectiveDate,
                ExpiryDateUtc = request.ExpiryDateUtc,
            };
            await context.Set<PriceLocalization>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
            rowId = entity.Id;
        }
        else
        {
            before = Describe(existing);
            existing.LocalizedPrice = request.LocalizedPrice;
            existing.IsAutoCalculated = false;
            existing.LastCalculatedAt = null;
            existing.ExchangeRateUsed = null;
            existing.ExpiryDateUtc = request.ExpiryDateUtc;
            existing.Touch();
            rowId = existing.Id;
        }

        context.Set<AuditTrail>().Add(new AuditTrail
        {
            TenantId = tenantId,
            EntityType = nameof(PriceLocalization),
            EntityId = rowId,
            Action = AuditAction.ConfigurationChanged,
            OldValue = before,
            NewValue = JsonSerializer.Serialize(new
            {
                request.PlanId,
                currencyCode = code,
                localizedPrice = request.LocalizedPrice,
                isAutoCalculated = false,
                effectiveDateUtc = effectiveDate,
                expiryDateUtc = request.ExpiryDateUtc,
            }),
            ChangedBy = actorId,
            ChangedAt = now,
            Metadata = JsonSerializer.Serialize(new { tenantId, action = "SetPlanLocalizedPrice", reason = request.Reason }),
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        localizedPriceResolver.Invalidate(request.PlanId, code);
        return rowId;
    }
}

/// <summary>
///     Command (issue #412 spec C): remove the manual overrides of a (plan, currency) pair so
///     pricing falls back to the auto-calculated/FX-converted price. Future-dated manual rows
///     are removed as well; auto rows are untouched. Audit-logged.
/// </summary>
/// <param name="PlanId">Plan whose overrides are cleared.</param>
/// <param name="CurrencyCode">ISO 4217 code (any casing).</param>
/// <param name="Reason">Audit reason.</param>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record ClearPlanLocalizedPriceCommand(
    Guid PlanId,
    string CurrencyCode,
    string Reason) : ICommand<int>;

/// <summary>Validator for <see cref="ClearPlanLocalizedPriceCommand"/>.</summary>
public sealed class ClearPlanLocalizedPriceCommandValidator : AbstractValidator<ClearPlanLocalizedPriceCommand>
{
    /// <summary>Initializes the validator.</summary>
    public ClearPlanLocalizedPriceCommandValidator()
    {
        RuleFor(command => command.PlanId).NotEqual(Guid.Empty);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty().Length(3).Matches("^[A-Za-z]{3}$");
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler for <see cref="ClearPlanLocalizedPriceCommand"/>.</summary>
public sealed class ClearPlanLocalizedPriceCommandHandler(
    IApplicationDbContext context,
    ILocalizedPlanPriceResolver localizedPriceResolver,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<ClearPlanLocalizedPriceCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(ClearPlanLocalizedPriceCommand request, CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);
        var code = CurrencyConfigurationCommandHelpers.NormalizeOrThrow(request.CurrencyCode);

        var rows = await context.Set<PriceLocalization>()
            .Where(localization => localization.TenantId == tenantId
                && localization.PlanId == request.PlanId
                && localization.CurrencyCode == code
                && !localization.IsAutoCalculated)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var row in rows)
        {
            context.Set<AuditTrail>().Add(new AuditTrail
            {
                TenantId = tenantId,
                EntityType = nameof(PriceLocalization),
                EntityId = row.Id,
                Action = AuditAction.Deleted,
                OldValue = Describe(row),
                NewValue = null,
                ChangedBy = actorId,
                ChangedAt = SystemClock.UtcNow,
                Metadata = JsonSerializer.Serialize(new { tenantId, action = "ClearPlanLocalizedPrice", reason = request.Reason }),
            });
            context.Set<PriceLocalization>().Remove(row);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        localizedPriceResolver.Invalidate(request.PlanId, code);
        return rows.Count;
    }
}

/// <summary>
///     Command (issue #412 spec C): bulk-recalculate auto-calculated localized prices from the
///     current FX quotations — every active plan × every enabled currency in scope, or a
///     single plan. Manual overrides are preserved; a persisted run row tracks progress and
///     snapshots before-values for rollback. Audit-logged.
/// </summary>
/// <param name="PlanId">Optional plan scope; null = every active plan of the tenant.</param>
/// <param name="Reason">Audit reason.</param>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record RecalculateLocalizedPricesCommand(
    Guid? PlanId = null,
    string Reason = "manual") : ICommand<PriceRecalculationOutcome>;

/// <summary>Validator for <see cref="RecalculateLocalizedPricesCommand"/>.</summary>
public sealed class RecalculateLocalizedPricesCommandValidator : AbstractValidator<RecalculateLocalizedPricesCommand>
{
    /// <summary>Initializes the validator.</summary>
    public RecalculateLocalizedPricesCommandValidator()
    {
        RuleFor(command => command.Reason).MaximumLength(200);
    }
}

/// <summary>Handler for <see cref="RecalculateLocalizedPricesCommand"/>.</summary>
public sealed class RecalculateLocalizedPricesCommandHandler(
    IApplicationDbContext context,
    IPriceLocalizationRecalculationService recalculationService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<RecalculateLocalizedPricesCommand, PriceRecalculationOutcome>
{
    /// <inheritdoc />
    public async Task<PriceRecalculationOutcome> Handle(RecalculateLocalizedPricesCommand request, CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);
        return await recalculationService
            .RecalculateAsync(context, tenantId, request.PlanId, actorId, request.Reason, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
///     Command (issue #412 spec C): roll back a finished recalculation run, restoring every
///     auto row it rewrote to the exact before-values snapshotted by the run. Manual overrides
///     were never touched and stay as they are. Audit-logged.
/// </summary>
/// <param name="RunId">Run to roll back.</param>
/// <param name="Reason">Audit reason.</param>
[AuthorizeRequest("", RequireTenantAdmin = true)]
public sealed record RollbackPriceRecalculationCommand(Guid RunId, string Reason) : ICommand<int>;

/// <summary>Validator for <see cref="RollbackPriceRecalculationCommand"/>.</summary>
public sealed class RollbackPriceRecalculationCommandValidator : AbstractValidator<RollbackPriceRecalculationCommand>
{
    /// <summary>Initializes the validator.</summary>
    public RollbackPriceRecalculationCommandValidator()
    {
        RuleFor(command => command.RunId).NotEqual(Guid.Empty);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler for <see cref="RollbackPriceRecalculationCommand"/>.</summary>
public sealed class RollbackPriceRecalculationCommandHandler(
    IApplicationDbContext context,
    ILocalizedPlanPriceResolver localizedPriceResolver,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<RollbackPriceRecalculationCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(RollbackPriceRecalculationCommand request, CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = CurrencyConfigurationCommandHelpers.ResolveActor(actorContextAccessor);

        var run = await context.Set<PriceRecalculationRun>()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RunId && candidate.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (run is null)
        {
            throw new BusinessRuleViolationException(
                PriceLocalizationRules.RunNotRollbackable,
                $"Recalculation run {request.RunId} does not exist for this tenant.",
                new { tenantId, request.RunId });
        }

        if (run.Status is not (PriceRecalculationRunStatus.Succeeded or PriceRecalculationRunStatus.Failed))
        {
            throw new BusinessRuleViolationException(
                PriceLocalizationRules.RunNotRollbackable,
                $"Recalculation run {request.RunId} is {run.Status}; only finished runs can be rolled back.",
                new { tenantId, request.RunId, run.Status });
        }

        var affectedPlanIds = new HashSet<Guid>();
        var restored = 0;
        if (!string.IsNullOrWhiteSpace(run.BeforeSnapshot))
        {
            var entries = JsonSerializer.Deserialize<List<PriceLocalizationRollbackEntry>>(run.BeforeSnapshot) ?? [];
            var rowIds = entries.Select(entry => entry.LocalizationId).ToList();
            var rows = await context.Set<PriceLocalization>()
                .Where(localization => rowIds.Contains(localization.Id))
                .ToDictionaryAsync(localization => localization.Id, cancellationToken)
                .ConfigureAwait(false);

            foreach (var entry in entries)
            {
                if (!rows.TryGetValue(entry.LocalizationId, out var row))
                {
                    continue; // the row was deleted afterwards; restoring it is not this run's business
                }

                context.Set<AuditTrail>().Add(new AuditTrail
                {
                    TenantId = tenantId,
                    EntityType = nameof(PriceLocalization),
                    EntityId = row.Id,
                    Action = AuditAction.Restored,
                    OldValue = Describe(row),
                    NewValue = JsonSerializer.Serialize(new
                    {
                        entry.LocalizedPrice,
                        entry.ExchangeRateUsed,
                        entry.LastCalculatedAt,
                    }),
                    ChangedBy = actorId,
                    ChangedAt = SystemClock.UtcNow,
                    Metadata = JsonSerializer.Serialize(new { tenantId, action = "RollbackPriceRecalculation", runId = run.Id, reason = request.Reason }),
                });

                row.LocalizedPrice = entry.LocalizedPrice;
                row.ExchangeRateUsed = entry.ExchangeRateUsed;
                row.LastCalculatedAt = entry.LastCalculatedAt;
                row.Touch();
                affectedPlanIds.Add(row.PlanId);
                restored++;
            }
        }

        run.Status = PriceRecalculationRunStatus.RolledBack;
        run.RolledBackAtUtc = SystemClock.UtcNow;
        run.RolledBackByUserId = actorId;
        run.Touch();

        context.Set<AuditTrail>().Add(new AuditTrail
        {
            TenantId = tenantId,
            EntityType = nameof(PriceRecalculationRun),
            EntityId = run.Id,
            Action = AuditAction.Updated,
            OldValue = JsonSerializer.Serialize(new { status = nameof(PriceRecalculationRunStatus.Succeeded) }),
            NewValue = JsonSerializer.Serialize(new { status = nameof(PriceRecalculationRunStatus.RolledBack), restored, reason = request.Reason }),
            ChangedBy = actorId,
            ChangedAt = run.RolledBackAtUtc.Value,
            Metadata = JsonSerializer.Serialize(new { tenantId, action = "RollbackPriceRecalculation" }),
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var planId in affectedPlanIds)
        {
            localizedPriceResolver.Invalidate(planId);
        }

        return restored;
    }
}

/// <summary>Shared helpers for the price localization commands.</summary>
internal static class PriceLocalizationCommandHelpers
{
    /// <summary>Whether the tenant has the currency enabled (fail closed: no row = not enabled).</summary>
    public static async Task<bool> IsCurrencyEnabledAsync(
        IApplicationDbContext context,
        Guid tenantId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        return await context.Set<CurrencyConfiguration>()
            .AsNoTracking()
            .AnyAsync(configuration => configuration.TenantId == tenantId
                && configuration.CurrencyCode == currencyCode
                && configuration.IsEnabled)
            .ConfigureAwait(false);
    }
}

/// <summary>JSON shape stored in <see cref="PriceRecalculationRun.BeforeSnapshot"/>.</summary>
public sealed record PriceLocalizationRollbackEntry(
    Guid LocalizationId,
    Guid PlanId,
    string CurrencyCode,
    decimal LocalizedPrice,
    decimal? ExchangeRateUsed,
    DateTime? LastCalculatedAt);

/// <summary>Shared serialization helper for localization audit payloads.</summary>
internal static class PriceLocalizationAudit
{
    /// <summary>Serializes the audit-relevant fields of a localization row.</summary>
    public static string Describe(PriceLocalization localization)
    {
        return JsonSerializer.Serialize(new
        {
            localization.TenantId,
            localization.PlanId,
            localization.CurrencyCode,
            localization.LocalizedPrice,
            localization.IsAutoCalculated,
            localization.ExchangeRateUsed,
            localization.LastCalculatedAt,
            localization.EffectiveDateUtc,
            localization.ExpiryDateUtc,
        });
    }
}
