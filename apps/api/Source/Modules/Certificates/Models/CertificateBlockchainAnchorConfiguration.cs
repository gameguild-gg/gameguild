using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Certificates.Models;

public class CertificateBlockchainAnchorConfiguration : IEntityTypeConfiguration<CertificateBlockchainAnchor> {
  public void Configure(EntityTypeBuilder<CertificateBlockchainAnchor> builder) {
    // Configure relationship with Certificate (can't be done with annotations)
    builder.HasOne(cba => cba.Certificate)
           .WithMany(uc => uc.BlockchainAnchors)
           .HasForeignKey(cba => cba.CertificateId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
