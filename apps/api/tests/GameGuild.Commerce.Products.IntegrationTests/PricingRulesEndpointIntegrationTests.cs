using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Products.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.Commerce.Products.IntegrationTests;

/// <summary>
/// Issue #395: end-to-end coverage for the pricing rules management API
/// (<c>/api/v{version}/billing/pricing-engine</c>) and the calculate endpoint that
/// applies volume tiers and customer segments at checkout calculation.
/// </summary>
public class PricingRulesEndpointIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private const string BasePath = "/api/v1/billing/pricing-engine";
    private const string ManagePermissions = "products:read,products:pricing:manage,monetization:monetize";
    private const string ReadPermissions = "products:read";

    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"PricingRulesTestDb_{Guid.NewGuid()}";

    public PricingRulesEndpointIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove all EF Core and Npgsql service registrations
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

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
                services.AddDefaultTenantMembership();

                // Authorization checks read permissions from the X-Test-Permissions header.
                services.RemoveAll<IAuthorizationPermissionService>();
                services.AddScoped<IAuthorizationPermissionService, PricingRulesTestAuthorizationPermissionService>();
                services.RemoveAll<IAuthorizationTenantResolver>();
                services.AddScoped<IAuthorizationTenantResolver, PricingRulesTestAuthorizationTenantResolver>();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = PricingRulesTestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = PricingRulesTestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, PricingRulesTestAuthHandler>(PricingRulesTestAuthHandler.SchemeName, _ => { });
            });
        });

        PricingRulesTestTenantServices.SeedDefaultTenant(_factory.Services);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CrudRoundTrip_CreateReadListUpdateActivateDelete()
    {
        var productId = Guid.NewGuid();

        // CREATE
        using (var create = AuthenticatedRequest(HttpMethod.Post, BasePath, ManagePermissions))
        {
            create.Content = JsonContent.Create(new
            {
                productId,
                name = "Integration volume tiers",
                ruleType = 13, // TieredPricing
                priority = 5,
                isActive = true,
                tiers = new object[]
                {
                    new { minQuantity = 10, discountPercentage = 10m },
                    new { minQuantity = 50, price = 75m }
                }
            });

            var response = await _client.SendAsync(create);
            response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

            var created = await response.Content.ReadFromJsonAsync<PricingRuleContract>();
            created!.Name.Should().Be("Integration volume tiers");
            created.Tiers.Should().HaveCount(2);
            created.Tiers![0].DiscountPercentage.Should().Be(10m);

            // READ (by id)
            var fetched = await GetRuleAsync(created.Id);
            fetched!.Name.Should().Be("Integration volume tiers");
            fetched.Tiers.Should().HaveCount(2);

            // LIST (by product, includes global rules)
            using var list = AuthenticatedRequest(HttpMethod.Get, $"{BasePath}?productId={productId}", ReadPermissions);
            var listResponse = await _client.SendAsync(list);
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await listResponse.Content.ReadFromJsonAsync<PagedContract<PricingRuleContract>>();
            page!.Items!.Select(r => r.Id).Should().Contain(created.Id);

            // UPDATE (replace tiers)
            using (var update = AuthenticatedRequest(HttpMethod.Put, $"{BasePath}/{created.Id}", ManagePermissions))
            {
                update.Content = JsonContent.Create(new
                {
                    productId,
                    name = "Integration volume tiers v2",
                    ruleType = 13,
                    priority = 9,
                    isActive = true,
                    tiers = new object[] { new { minQuantity = 20, discountPercentage = 15m } }
                });

                var updateResponse = await _client.SendAsync(update);
                updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());
                var updated = await updateResponse.Content.ReadFromJsonAsync<PricingRuleContract>();
                updated!.Name.Should().Be("Integration volume tiers v2");
                updated.Tiers.Should().ContainSingle().Which.DiscountPercentage.Should().Be(15m);
            }

            // DEACTIVATE + ACTIVATE
            using (var deactivate = AuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}:deactivate", ManagePermissions))
            {
                (await _client.SendAsync(deactivate)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var activate = AuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}:activate", ManagePermissions))
            {
                (await _client.SendAsync(activate)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // DELETE (soft)
            using (var delete = AuthenticatedRequest(HttpMethod.Delete, $"{BasePath}/{created.Id}", ManagePermissions))
            {
                (await _client.SendAsync(delete)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            // READ after delete -> 404
            using var gone = AuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{created.Id}", ReadPermissions);
            (await _client.SendAsync(gone)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task Calculate_AppliesVolumeTier_AtQualifiedQuantity()
    {
        var productId = await SeedProductWithPricingAsync(basePrice: 100m);

        using (var create = AuthenticatedRequest(HttpMethod.Post, BasePath, ManagePermissions))
        {
            create.Content = JsonContent.Create(new
            {
                productId,
                name = "Checkout volume discount",
                ruleType = 13, // TieredPricing
                tiers = new object[] { new { minQuantity = 10, discountPercentage = 10m } }
            });

            var response = await _client.SendAsync(create);
            response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        }

        var lowVolume = await CalculateAsync(productId, quantity: 9);
        lowVolume!.FinalPrice.Should().Be(100m);
        lowVolume.RuleDiscount.Should().Be(0m);

        var highVolume = await CalculateAsync(productId, quantity: 10);
        highVolume!.FinalPrice.Should().Be(90m);
        highVolume.RuleDiscount.Should().Be(10m);
        highVolume.AppliedRuleName.Should().Be("Checkout volume discount");
    }

    [Fact]
    public async Task Calculate_AppliesSegmentRule_OnlyForMatchingSegment()
    {
        var productId = await SeedProductWithPricingAsync(basePrice: 200m);

        using (var create = AuthenticatedRequest(HttpMethod.Post, BasePath, ManagePermissions))
        {
            create.Content = JsonContent.Create(new
            {
                productId,
                name = "VIP segment price",
                ruleType = 4, // SegmentBased
                discountPercentage = 25m,
                customerSegment = "vip"
            });

            var response = await _client.SendAsync(create);
            response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        }

        var anonymous = await CalculateAsync(productId, quantity: 1, customerSegment: null);
        anonymous!.FinalPrice.Should().Be(200m);

        var basic = await CalculateAsync(productId, quantity: 1, customerSegment: "basic");
        basic!.FinalPrice.Should().Be(200m);

        var vip = await CalculateAsync(productId, quantity: 1, customerSegment: "VIP");
        vip!.FinalPrice.Should().Be(150m);
        vip.AppliedRuleName.Should().Be("VIP segment price");
    }

    [Fact]
    public async Task Calculate_WithoutRules_FallsBackToBasePrice()
    {
        var productId = await SeedProductWithPricingAsync(basePrice: 123m);

        var result = await CalculateAsync(productId, quantity: 3);

        result!.FinalPrice.Should().Be(123m);
        result.RuleDiscount.Should().Be(0m);
        result.AppliedRuleId.Should().BeNull();
    }

    [Fact]
    public async Task Mutations_RequirePricingManagePermission()
    {
        using var request = AuthenticatedRequest(HttpMethod.Post, BasePath, ReadPermissions);
        request.Content = JsonContent.Create(new { name = "nope", ruleType = 10 });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Mutations_RequireAuthentication()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BasePath);
        request.Content = JsonContent.Create(new { name = "nope", ruleType = 10 });
        request.Headers.Add("X-Test-Unauthenticated", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<PricingRuleContract?> GetRuleAsync(Guid ruleId)
    {
        using var request = AuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{ruleId}", ReadPermissions);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<PricingRuleContract>();
    }

    private async Task<PricingCalculationContract?> CalculateAsync(Guid productId, int quantity, string? customerSegment = null)
    {
        using var request = AuthenticatedRequest(HttpMethod.Post, $"{BasePath}:calculate", ReadPermissions);
        request.Content = JsonContent.Create(new { productId, quantity, customerSegment });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<PricingCalculationContract>();
    }

    private async Task<Guid> SeedProductWithPricingAsync(decimal basePrice)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = Product.Create($"Pricing Engine Product {Guid.NewGuid()}", ProductType.Program);
        var (pricing, _) = ProductPricing.CreateWithVersion(product.Id, "Standard", basePrice, isDefault: true);
        product.Pricing.Add(pricing);

        context.Set<Product>().Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string uri, string permissions)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Test-Permissions", permissions);
        return request;
    }

    // Lightweight JSON contracts to assert on response payloads.

    private sealed record PricingRuleContract(
        Guid Id,
        Guid? ProductId,
        string Name,
        int? RuleType,
        int Priority,
        bool IsActive,
        IReadOnlyList<PricingRuleTierContract>? Tiers,
        decimal? DiscountPercentage,
        string? AppliedRuleName = null);

    private sealed record PricingRuleTierContract(
        Guid Id,
        int? MinQuantity,
        int? MaxQuantity,
        decimal? Price,
        decimal? DiscountPercentage);

    private sealed record PagedContract<T>(
        IReadOnlyList<T>? Items,
        int TotalCount,
        int Skip,
        int Take);

    private sealed record PricingCalculationContract(
        decimal BasePrice,
        decimal? SalePrice,
        bool IsSaleActive,
        decimal PromoDiscount,
        decimal FinalPrice,
        string Currency,
        IReadOnlyList<string>? AppliedPromoCodes,
        decimal RuleDiscount,
        Guid? AppliedRuleId,
        string? AppliedRuleName);
}
