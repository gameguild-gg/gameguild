using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.Tests.Commerce.Products.Unit.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Services;

/// <summary>
/// Issue #418 customer-acquisition-cost attribution: the first recorded promo usage of a
/// customer under a campaign-tagged code writes one attribution row linking the customer
/// to the campaign; later usages never re-attribute; attribution failures never fail
/// the redemption.
/// </summary>
public class PromoCampaignAttributionTests : IDisposable
{
    private readonly List<System.Data.Common.DbConnection> _connections = new();

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.Dispose();
        }
    }

    private System.Data.Common.DbConnection CreateConnection()
    {
        var connection = PromoSqliteTestSupport.CreatePrivateConnection();
        _connections.Add(connection);
        return connection;
    }

    [Fact]
    public async Task FirstUsage_UnderCampaignTag_WritesAttribution()
    {
        var connection = CreateConnection();
        var promo = PromoSqliteTestSupport.SeedPromoCode(connection, p => p.CampaignId = "LAUNCH26");
        var userId = Guid.NewGuid();
        var recorder = new PromoCodeUsageRecorder(new PromoCodeRepository(PromoSqliteTestSupport.CreateContext(connection)));

        var (status, usage) = await recorder.RecordAsync(promo, userId, 10m, "USD", "cac-1");

        status.Should().Be(PromoUsageRecordingStatus.Recorded);
        await using var verify = PromoSqliteTestSupport.CreateContext(connection);
        var attribution = await verify.Set<PromoCampaignAttribution>().SingleOrDefaultAsync(a => a.UserId == userId);
        attribution.Should().NotBeNull();
        attribution!.CampaignKey.Should().Be("LAUNCH26");
        attribution.Code.Should().Be(promo.Code);
        attribution.FirstPurchaseUsageId.Should().Be(usage!.Id);
    }

    [Fact]
    public async Task SecondUsage_NeverRewritesAttribution()
    {
        var connection = CreateConnection();
        var firstPromo = PromoSqliteTestSupport.SeedPromoCode(connection, p => p.CampaignId = "LAUNCH26");
        var secondPromo = PromoSqliteTestSupport.SeedPromoCode(connection, p =>
        {
            p.Code = "OTHERCODE";
            p.CampaignId = "OTHER26";
        });
        var userId = Guid.NewGuid();
        var recorder = new PromoCodeUsageRecorder(new PromoCodeRepository(PromoSqliteTestSupport.CreateContext(connection)));

        await recorder.RecordAsync(firstPromo, userId, 10m, "USD", "cac-first");
        await recorder.RecordAsync(secondPromo, userId, 5m, "USD", "cac-second");

        await using var verify = PromoSqliteTestSupport.CreateContext(connection);
        var attributions = await verify.Set<PromoCampaignAttribution>().Where(a => a.UserId == userId).ToListAsync();
        attributions.Should().HaveCount(1);
        attributions[0].CampaignKey.Should().Be("LAUNCH26", "the first campaign keeps the attribution");
    }

    [Fact]
    public async Task Usage_WithoutCampaignTag_WritesNoAttribution()
    {
        var connection = CreateConnection();
        var promo = PromoSqliteTestSupport.SeedPromoCode(connection);
        var recorder = new PromoCodeUsageRecorder(new PromoCodeRepository(PromoSqliteTestSupport.CreateContext(connection)));

        await recorder.RecordAsync(promo, Guid.NewGuid(), 10m, "USD", "no-campaign");

        await using var verify = PromoSqliteTestSupport.CreateContext(connection);
        (await verify.Set<PromoCampaignAttribution>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CampaignPerformance_CarriesCacFields()
    {
        var connection = CreateConnection();
        var promo = PromoSqliteTestSupport.SeedPromoCode(connection, p => p.CampaignId = "LAUNCH26");

        // Managed campaign row with acquisition spend; three attributed customers.
        await using (var seed = PromoSqliteTestSupport.CreateContext(connection))
        {
            seed.Set<PromoCampaign>().Add(new PromoCampaign
            {
                Id = Guid.NewGuid(),
                Name = "Launch",
                Key = "LAUNCH26",
                AcquisitionSpend = 300m,
                Currency = "USD",
                CreatedBy = Guid.NewGuid()
            });
            for (var i = 0; i < 3; i++)
            {
                var userId = Guid.NewGuid();
                seed.Set<PromoCampaignAttribution>().Add(new PromoCampaignAttribution
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CampaignKey = "LAUNCH26"
                });
                seed.Set<PromoCodeUse>().Add(new PromoCodeUse
                {
                    Id = Guid.NewGuid(),
                    PromoCodeId = promo.Id,
                    UserId = userId,
                    DiscountApplied = 10m,
                    Currency = "USD"
                });
            }

            await seed.SaveChangesAsync();
        }

        var repository = new PromoCodeRepository(PromoSqliteTestSupport.CreateContext(connection));
        var performance = await repository.GetCampaignPerformanceAsync("LAUNCH26");

        performance.AcquiredCustomers.Should().Be(3);
        performance.CampaignAcquisitionSpend.Should().Be(300m);
        performance.CustomerAcquisitionCost.Should().Be(100m);
    }

    [Fact]
    public async Task CampaignPerformance_WithoutManagedCampaign_CacStaysNull()
    {
        var connection = CreateConnection();
        var promo = PromoSqliteTestSupport.SeedPromoCode(connection, p => p.CampaignId = "UNMANAGED");
        await using (var seed = PromoSqliteTestSupport.CreateContext(connection))
        {
            seed.Set<PromoCodeUse>().Add(new PromoCodeUse
            {
                Id = Guid.NewGuid(),
                PromoCodeId = promo.Id,
                UserId = Guid.NewGuid(),
                DiscountApplied = 5m,
                Currency = "USD"
            });
            await seed.SaveChangesAsync();
        }

        var repository = new PromoCodeRepository(PromoSqliteTestSupport.CreateContext(connection));
        var performance = await repository.GetCampaignPerformanceAsync("UNMANAGED");

        performance.AcquiredCustomers.Should().Be(0);
        performance.CampaignAcquisitionSpend.Should().BeNull();
        performance.CustomerAcquisitionCost.Should().BeNull();
    }
}
