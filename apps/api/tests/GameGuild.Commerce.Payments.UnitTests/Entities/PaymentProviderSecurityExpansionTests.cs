using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Entities;

public sealed class PaymentProviderSecurityExpansionTests
{
    [Fact]
    public void Payment_Model_Exposes_Nullable_Provider_Mapping_Fields_And_Scoped_Index()
    {
        var modelBuilder = new ModelBuilder();
        new PaymentsModelConfiguration().Configure(modelBuilder);

        var entity = modelBuilder.Model.FindEntityType(typeof(Payment))!;
        foreach (var propertyName in new[]
                 {
                     nameof(Payment.ProviderEnvironment),
                     nameof(Payment.ProviderAccountId),
                     nameof(Payment.ProviderObjectId),
                     nameof(Payment.ProviderObjectType),
                     nameof(Payment.ProviderMonetaryLeg)
                 })
        {
            entity.FindProperty(propertyName).Should().NotBeNull();
            entity.FindProperty(propertyName)!.IsNullable.Should().BeTrue();
        }

        entity.GetIndexes().Single(index => index.GetDatabaseName() == "ix_payments_provider_object_leg")
            .IsUnique.Should().BeFalse();
    }

    [Fact]
    public void ResolveUnverifiedLegacyProviderObjectId_Returns_Existing_External_Payment_Id()
    {
        var payment = Payment.Create(Guid.NewGuid(), 10m, "USD", "provider-expand");
        payment.MarkAsProcessing("txn_legacy");
        payment.MarkAsSucceeded("pi_legacy");

        payment.ResolveUnverifiedLegacyProviderObjectId().Should().Be("pi_legacy");
    }
}
