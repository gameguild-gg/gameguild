using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Products.Models;

/// <summary>
/// Entity Framework configuration for PromoCodeUse entity
/// </summary>
public class PromoCodeUseConfiguration : IEntityTypeConfiguration<PromoCodeUse> {
  public void Configure(EntityTypeBuilder<PromoCodeUse> builder) {
    // Configure relationship with PromoCode (can't be done with annotations)
    builder.HasOne(pcu => pcu.PromoCode)
           .WithMany(pc => pc.PromoCodeUses)
           .HasForeignKey(pcu => pcu.PromoCodeId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(pcu => pcu.User).WithMany().HasForeignKey(pcu => pcu.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with FinancialTransaction (can't be done with annotations)
    builder.HasOne(pcu => pcu.FinancialTransaction)
           .WithMany(ft => ft.PromoCodeUses)
           .HasForeignKey(pcu => pcu.FinancialTransactionId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
