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
        entity!.GetTableName().Should().Be("billing_webhook_events");
    }

    [Fact]
    public void Configure_Should_Keep_Provider_Security_Expansion_Nullable_And_Legacy_Index()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new BillingWebhookEventConfiguration().Configure(modelBuilder.Entity<BillingWebhookEvent>());

        var entity = modelBuilder.Model.FindEntityType(typeof(BillingWebhookEvent))!;
        foreach (var propertyName in new[]
                 {
                     nameof(BillingWebhookEvent.ProviderEnvironment),
                     nameof(BillingWebhookEvent.ProviderAccountId),
                     nameof(BillingWebhookEvent.WebhookEndpointId),
                     nameof(BillingWebhookEvent.ProviderObjectId),
                     nameof(BillingWebhookEvent.ProviderObjectType),
                     nameof(BillingWebhookEvent.ProviderMonetaryLeg),
                     nameof(BillingWebhookEvent.IsLiveMode),
                     nameof(BillingWebhookEvent.EventSchemaVersion)
                 })
        {
            entity.FindProperty(propertyName).Should().NotBeNull();
            entity.FindProperty(propertyName)!.IsNullable.Should().BeTrue();
        }

        entity.GetIndexes().Select(index => index.GetDatabaseName()).Should().Contain(
            "ix_billing_webhook_events_external_id_provider");
        entity.GetIndexes().Select(index => index.GetDatabaseName()).Should().Contain(
            "ix_billing_webhook_events_provider_scope_event");
        entity.GetIndexes().Select(index => index.GetDatabaseName()).Should().Contain(
            "ix_billing_webhook_events_provider_object_leg");
        entity.GetIndexes().Single(index =>
                index.GetDatabaseName() == "ix_billing_webhook_events_provider_scope_event")
            .IsUnique.Should().BeTrue();
        entity.GetIndexes().Single(index =>
                index.GetDatabaseName() == "ix_billing_webhook_events_external_id_provider")
            .GetFilter().Should().Contain("\"ProviderEnvironment\" IS NULL");
        entity.FindProperty(nameof(BillingWebhookEvent.ProcessingAttempts))!
            .IsConcurrencyToken.Should().BeTrue();

        entity.GetCheckConstraints().Select(constraint => constraint.Name).Should().Contain(
            "ck_billing_webhook_events_provider_scope_complete");
        entity.GetCheckConstraints().Select(constraint => constraint.Name).Should().Contain(
            "ck_billing_webhook_events_provider_object_complete");
        entity.GetCheckConstraints().Select(constraint => constraint.Name).Should().Contain(
            "ck_billing_webhook_events_provider_environment");
    }
}
