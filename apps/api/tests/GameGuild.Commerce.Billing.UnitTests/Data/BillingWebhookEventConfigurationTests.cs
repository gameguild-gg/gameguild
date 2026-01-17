using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Data;

public class BillingWebhookEventConfigurationTests
{
    [Fact]
    public void Configure_Should_Build_Model()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        var configuration = new BillingWebhookEventConfiguration();
        configuration.Configure(modelBuilder.Entity<BillingWebhookEvent>());

        var entity = modelBuilder.Model.FindEntityType(typeof(BillingWebhookEvent));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("BillingWebhookEvents");
    }
}