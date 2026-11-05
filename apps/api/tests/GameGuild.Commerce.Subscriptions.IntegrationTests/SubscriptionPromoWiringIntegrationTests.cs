using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Products;
using GameGuild.Commerce.Subscriptions.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests;

/// <summary>
///     Issue #1290 end-to-end wiring tests over the composed API host. Before the fix,
///     <c>AddProductsModule</c> was never invoked by the host: the subscription promo
///     coordinator resolved null (every subscription creation carrying promo codes 500ed),
///     the fraud-review ticket coordinator silently skipped, and the pricing scheduling
///     worker never ran. These tests pin the composed-host behavior after the module is
///     wired: promo checkout succeeds with the discount applied against the plan's
///     authoritative price, fully-rejected quotes answer 422 (not 500), and fraud review
///     cases raise support tickets.
/// </summary>
public sealed class SubscriptionPromoWiringIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string DatabaseName = $"SubscriptionPromoWiringDb_{Guid.NewGuid()}";

    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;

    public SubscriptionPromoWiringIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL existing DbContext and EF Core registrations (same pattern as
                // SubscriptionEndpointsIntegrationTests) and replace them with an in-memory
                // database shared across the host and the test assertions.
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(DatabaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                    .Options;

                services.AddSingleton(options);
                services.AddScoped<SubscriptionTestDbContext>();
                services.AddScoped<ApplicationDbContext>(p => p.GetRequiredService<SubscriptionTestDbContext>());
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

                services.AddHttpLogging(o => { });

                // The REAL PromoCodeUsageRecorder commits guarded ExecuteUpdate counter
                // increments inside a transaction, which the in-memory store cannot host;
                // its atomicity contract is covered by PromoCodeUsageRecorderTests (SQLite).
                // This substitute keeps the same interface contract with plain inserts so
                // the composed promo-apply path itself stays under test here.
                services.AddScoped<GameGuild.Commerce.Products.IPromoCodeUsageRecorder>(
                    p => new PlainInsertPromoUsageRecorder(p.GetRequiredService<IApplicationDbContext>()));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── P1: the composed host registers the module's seams and worker ──────────

    [Fact]
    public void ComposedHost_RegistersTheProductsModuleSeams()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<GameGuild.Commerce.ISubscriptionPromoCoordinator>()
                .Should().BeOfType<SubscriptionPromoCoordinator>(
                    "issue #1290 P2: before the module was wired this resolved null and promo-code checkouts 500ed");

            scope.ServiceProvider.GetRequiredService<GameGuild.Commerce.IFraudReviewTicketCoordinator>()
                .Should().BeOfType<FraudReviewSupportTicketCoordinator>(
                    "issue #1290 P3: fraud Review outcomes must raise support tickets");

            scope.ServiceProvider.GetRequiredService<IPromoRedemptionCaptchaGate>()
                .Should().BeOfType<PromoRedemptionCaptchaGate>();
            scope.ServiceProvider.GetRequiredService<IPromoUsageAnomalyEvaluator>()
                .Should().BeOfType<PromoUsageAnomalyEvaluator>();
            scope.ServiceProvider.GetRequiredService<IPromoCustomerStateResolver>()
                .Should().BeOfType<PromoCustomerStateResolver>();
        }

        _factory.Services.GetServices<IHostedService>()
            .Should().Contain(hosted => hosted is PricingSchedulingBackgroundService,
                "issue #1290 P1: the pricing scheduling worker is hosted by the composed API");
    }

    // ── P2: subscription creation WITH a promo code ─────────────────────────────

    [Fact]
    public async Task CreateSubscription_WithValidPromoCode_AppliesDiscountAgainstPlanPrice_AndRecordsUsage()
    {
        var planId = await SeedPlanAsync();
        var promoId = await SeedPromoCodeAsync(code: "WIRING5", discountAmount: 5.00m);

        // The client amount (999) is advisory only: the quote runs against the plan's
        // authoritative price (29.99), and the promo reduces what the customer pays.
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", new
        {
            TenantId,
            PlanId = planId,
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = 1,
            Amount = 999m,
            Currency = "USD",
            PromoCodes = new[] { "WIRING5" },
            PromoCodeIdempotencyKey = "wiring-happy-1"
        });

        // Before the fix this endpoint 500ed: the promo coordinator resolved null and the
        // handler refused to silently drop the supplied codes.
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var subscriptionId = await ReadCreatedIdAsync(response);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var subscription = await dbContext.Set<Subscription>().SingleAsync(s => s.Id == subscriptionId);
            subscription.Amount.Amount.Should().Be(24.99m,
                "the plan price (29.99) stays authoritative; the promo reduces what the customer pays");

            var usage = await dbContext.Set<PromoCodeUse>().SingleAsync(u => u.SubscriptionId == subscriptionId);
            usage.PromoCodeId.Should().Be(promoId);
            usage.DiscountApplied.Should().Be(5.00m);
            usage.Currency.Should().Be("USD");
            // The coordinator derives a per-code key from the caller's base key
            // (SubscriptionPromoCoordinator.RecordSubscriptionUsageAsync).
            usage.IdempotencyKey.Should().Be("wiring-happy-1:code:WIRING5");

            (await dbContext.Set<PromoCode>().SingleAsync(p => p.Id == promoId)).CurrentUses
                .Should().Be(1, "one usage row is recorded and counted for the applied code");
        }
    }

    [Fact]
    public async Task CreateSubscription_WithExpiredPromoCode_Returns422Not500()
    {
        var planId = await SeedPlanAsync();
        await SeedPromoCodeAsync(code: "EXPIREDW", discountAmount: 5.00m, validUntil: SystemClock.UtcNow.AddDays(-1));

        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", new
        {
            TenantId,
            PlanId = planId,
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = 1,
            Amount = 29.99m,
            Currency = "USD",
            PromoCodes = new[] { "EXPIREDW" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            $"a fully-rejected promo quote is a client-input outcome (422), never a 500 — got {(int)response.StatusCode}");

        // The composed host's ProblemDetails pipeline masks the detail text for domain
        // exceptions (the rejection reason itself — "Promo code has expired" — is pinned by
        // CreateSubscriptionPromoTests at the handler level), so only the status contract
        // is asserted here.
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\": 422", "the domain-violation ProblemDetails carries the 422 status");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Scope to this test's plan: the class shares one in-memory database across its
            // tests, so a tenant-wide count would see rows created by sibling tests.
            (await dbContext.Set<Subscription>().CountAsync(s => s.PlanId == planId)).Should().Be(0,
                "a rejected quote must fail closed before any subscription is created");
        }
    }

    [Fact]
    public async Task CreateSubscription_WithUnknownPromoCode_Returns422Not500()
    {
        var planId = await SeedPlanAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", new
        {
            TenantId,
            PlanId = planId,
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = 1,
            Amount = 29.99m,
            Currency = "USD",
            PromoCodes = new[] { "NOSUCHCODE" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            $"an unknown promo code is a client-input outcome (422), never a 500 — got {(int)response.StatusCode}");
    }

    // ── P3: fraud review cases raise support tickets on the products path ──────

    [Fact]
    public async Task ComposedHost_FraudReviewCoordinator_OpensASupportTicket()
    {
        var paymentId = Guid.NewGuid();

        Guid ticketId;
        using (var scope = _factory.Services.CreateScope())
        {
            var coordinator = scope.ServiceProvider
                .GetRequiredService<GameGuild.Commerce.IFraudReviewTicketCoordinator>();

            var opened = await coordinator.OpenReviewTicketAsync(new GameGuild.Commerce.FraudReviewTicketRequest(
                TenantId,
                paymentId,
                "Payment",
                "payment-gate",
                Score: 60,
                Reason: "disposable email domain",
                Amount: 29.99m,
                Currency: "USD",
                EvidenceJson: "{\"signals\":[\"disposable-email\"]}"));

            opened.Should().NotBeNull("the composed host must raise the ticket, not silently skip it (issue #1290 P3)");
            ticketId = opened!.Value;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ticket = await dbContext.Set<SupportTicket>().SingleAsync(t => t.Id == ticketId);

            ticket.TenantId.Should().Be(TenantId);
            ticket.Category.Should().Be("fraud-review");
            ticket.Priority.Should().Be(SupportTicketPriority.High,
                "scores at or below 85 are High; only Critical-band scores are Urgent");
            ticket.CustomerId.Should().Be(paymentId, "the ticket's customer slot carries the held payment id (issue #409 precedent)");
            ticket.Subject.Should().Contain("Fraud review required").And.Contain("score 60");
        }
    }

    // ── Seeding helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> SeedPlanAsync()
    {
        SubscriptionTestTenantSeeder.EnsureTenantExists(_factory.Services, TenantId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = new SubscriptionPlan(
            name: "Wiring Plan",
            slug: $"wiring-plan-{Guid.NewGuid():N}",
            monthlyPriceInCents: 2999,
            currency: "USD",
            description: "Issue #1290 wiring test plan");
        dbContext.Set<SubscriptionPlan>().Add(plan);
        await dbContext.SaveChangesAsync();

        return plan.Id;
    }

    private async Task<Guid> SeedPromoCodeAsync(
        string code,
        decimal discountAmount,
        DateTime? validUntil = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var promo = new PromoCode
        {
            Code = code,
            Name = $"Wiring promo {code}",
            Description = "Issue #1290 wiring test promo",
            Type = PromoCodeType.FixedAmountOff,
            DiscountAmount = discountAmount,
            Currency = "USD",
            IsActive = true,
            ValidFrom = null,
            ValidUntil = validUntil,
            EligibleCustomerType = PromoCustomerType.Any,
            TenantId = TenantId,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.Set<PromoCode>().Add(promo);
        await dbContext.SaveChangesAsync();

        return promo.Id;
    }

    private static async Task<Guid> ReadCreatedIdAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var root = System.Text.Json.JsonDocument.Parse(content).RootElement;
        if (root.TryGetProperty("id", out var id) && id.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return id.GetGuid();
        }

        if (root.TryGetProperty("Id", out var idCapitalized) && idCapitalized.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return idCapitalized.GetGuid();
        }

        throw new Xunit.Sdk.XunitException($"Could not read the created id from: {content}");
    }

    /// <summary>
    ///     In-memory-friendly stand-in for <see cref="PromoCodeUsageRecorder"/>: same
    ///     interface contract (replay by idempotency key, usage row linked to the
    ///     subscription, counter incremented) implemented with plain inserts because the
    ///     in-memory provider cannot host ExecuteUpdate/transactions. The real recorder's
    ///     atomicity is covered by PromoCodeUsageRecorderTests.
    /// </summary>
    private sealed class PlainInsertPromoUsageRecorder(IApplicationDbContext context) : IPromoCodeUsageRecorder
    {
        public async Task<(PromoUsageRecordingStatus Status, PromoCodeUse? Usage)> RecordAsync(
            PromoCode promoCode,
            Guid userId,
            decimal discountApplied,
            string currency,
            string? idempotencyKey,
            Guid? orderId = null,
            Guid? orderLineItemId = null,
            Guid? subscriptionId = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await context.Set<PromoCodeUse>()
                    .FirstOrDefaultAsync(u => u.IdempotencyKey == idempotencyKey, cancellationToken);
                if (existing is not null)
                {
                    return (PromoUsageRecordingStatus.Replay, existing);
                }
            }

            promoCode.CurrentUses += 1;
            var usage = new PromoCodeUse
            {
                Id = Guid.NewGuid(),
                PromoCodeId = promoCode.Id,
                UserId = userId,
                TenantId = promoCode.TenantId,
                UsedAt = SystemClock.UtcNow,
                DiscountApplied = decimal.Round(discountApplied, 2, MidpointRounding.AwayFromZero),
                Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant(),
                OrderId = orderId,
                OrderLineItemId = orderLineItemId,
                SubscriptionId = subscriptionId,
                IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim()
            };

            await context.Set<PromoCodeUse>().AddAsync(usage, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return (PromoUsageRecordingStatus.Recorded, usage);
        }
    }
}
