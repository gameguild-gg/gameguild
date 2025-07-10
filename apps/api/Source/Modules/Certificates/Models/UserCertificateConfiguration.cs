using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Certificates.Models;

public class UserCertificateConfiguration : IEntityTypeConfiguration<UserCertificate> {
  public void Configure(EntityTypeBuilder<UserCertificate> builder) {
    // Configure relationship with Certificate (can't be done with annotations)
    builder.HasOne(uc => uc.Certificate)
           .WithMany()
           .HasForeignKey(uc => uc.CertificateId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(uc => uc.User).WithMany().HasForeignKey(uc => uc.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure optional relationship with Program (can't be done with annotations)
    builder.HasOne(uc => uc.Program).WithMany().HasForeignKey(uc => uc.ProgramId).OnDelete(DeleteBehavior.SetNull);

    // Configure optional relationship with Product (can't be done with annotations)
    builder.HasOne(uc => uc.Product).WithMany().HasForeignKey(uc => uc.ProductId).OnDelete(DeleteBehavior.SetNull);

    // Configure optional relationship with ProgramUser (can't be done with annotations)
    builder.HasOne(uc => uc.ProgramUser)
           .WithMany(pu => pu.UserCertificates)
           .HasForeignKey(uc => uc.ProgramUserId)
           .OnDelete(DeleteBehavior.SetNull);
  }
}
