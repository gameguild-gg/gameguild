namespace GameGuild.Modules.Payments;

public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction> {
  public void Configure(EntityTypeBuilder<FinancialTransaction> builder) {
    // Configure relationship with FromUser (can't be done with annotations)
    builder.HasOne(ft => ft.FromUser).WithMany().HasForeignKey(ft => ft.FromUserId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with ToUser (can't be done with annotations)
    builder.HasOne(ft => ft.ToUser).WithMany().HasForeignKey(ft => ft.ToUserId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with PaymentMethod (can't be done with annotations)
    builder.HasOne(ft => ft.PaymentMethod)
           .WithMany()
           .HasForeignKey(ft => ft.PaymentMethodId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with PromoCode (can't be done with annotations)
    // This explicitly maps to the FinancialTransactions collection on PromoCode
    builder.HasOne(ft => ft.PromoCode)
           .WithMany(pc => pc.FinancialTransactions)
           .HasForeignKey(ft => ft.PromoCodeId)
           .OnDelete(DeleteBehavior.SetNull);
  }
}
