namespace GameGuild.Modules.Certificates;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate> {
  public void Configure(EntityTypeBuilder<Certificate> builder) {
    // Configure relationship with Program (specify the navigation property)
    builder.HasOne(c => c.Program).WithMany(p => p.Certificates).HasForeignKey(c => c.ProgramId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(c => c.Product).WithMany().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.SetNull);
  }
}
