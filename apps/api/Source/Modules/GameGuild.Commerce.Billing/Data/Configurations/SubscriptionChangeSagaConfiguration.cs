using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     EF Core configuration for the orchestrated subscription plan change saga state
///     (issue #396 workflow orchestration).
/// </summary>
public class SubscriptionChangeSagaConfiguration : IEntityTypeConfiguration<SubscriptionChangeSaga>
{
    public void Configure(EntityTypeBuilder<SubscriptionChangeSaga> builder)
    {
        builder.ToTable("subscription_change_sagas");

        builder.HasKey(saga => saga.Id);

        builder.Property(saga => saga.SubscriptionId).IsRequired();
        builder.Property(saga => saga.FromPlanId).IsRequired();
        builder.Property(saga => saga.ToPlanId).IsRequired();
        builder.Property(saga => saga.InitiatedByActorId).IsRequired();
        builder.Property(saga => saga.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(saga => saga.CurrentStep).HasMaxLength(50).IsRequired();
        builder.Property(saga => saga.Currency).HasMaxLength(3).IsRequired();
        builder.Property(saga => saga.PaymentTransactionId).HasMaxLength(255);
        builder.Property(saga => saga.FailedStep).HasMaxLength(50);
        builder.Property(saga => saga.FailureReason).HasMaxLength(1000);
        builder.Property(saga => saga.CompensationsApplied).HasMaxLength(1000);

        builder.Property(saga => saga.CreditForUnused).HasPrecision(18, 2);
        builder.Property(saga => saga.ChargeForNew).HasPrecision(18, 2);
        builder.Property(saga => saga.NetAdjustment).HasPrecision(18, 2);

        // One durable saga per change intent: replayed requests resolve to the original
        // saga instead of double-charging.
        builder.HasIndex(saga => saga.IdempotencyKey).IsUnique();
        builder.HasIndex(saga => saga.SubscriptionId);
        builder.HasIndex(saga => saga.Status);

        builder.HasMany(saga => saga.Steps)
            .WithOne()
            .HasForeignKey(step => step.SagaId)
            .OnDelete(DeleteBehavior.Cascade);

        var navigation = builder.Metadata.FindNavigation(nameof(SubscriptionChangeSaga.Steps))!;
        navigation.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
///     EF Core configuration for the saga step journal (issue #396 workflow orchestration).
/// </summary>
public class SubscriptionChangeSagaStepConfiguration : IEntityTypeConfiguration<SubscriptionChangeSagaStep>
{
    public void Configure(EntityTypeBuilder<SubscriptionChangeSagaStep> builder)
    {
        builder.ToTable("subscription_change_saga_steps");

        builder.HasKey(step => step.Id);

        builder.Property(step => step.SagaId).IsRequired();
        builder.Property(step => step.Step).HasMaxLength(50).IsRequired();
        builder.Property(step => step.Outcome).HasMaxLength(20).IsRequired();
        builder.Property(step => step.Detail).HasMaxLength(500).IsRequired();

        builder.HasIndex(step => step.SagaId);
    }
}
