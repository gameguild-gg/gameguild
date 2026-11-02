
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using GameGuild.Learning.Grading.Contracts;

 namespace GameGuild.Learning.Courses;

/// <summary> EntityBase Framework configuration for Program entity </summary>
public class ProgramConfiguration : IEntityTypeConfiguration<Program> {
  public void Configure(EntityTypeBuilder<Program> builder) {
    // Ignore computed properties that shouldn't be mapped by EF Core
    builder.Ignore(p => p.SkillsRequired);
    builder.Ignore(p => p.SkillsProvided);
    builder.Ignore(p => p.AverageRating);
    builder.Ignore(p => p.TotalRatings);

    builder.Property(p => p.PassingScore)
      .IsRequired()
      .HasDefaultValue(PercentValue.FromUnits(6000));

    // Issue #1157: the public catalog lists programs by status on every browse; the
    // partial index keeps soft-deleted programs out of that hot path entirely.
    builder.HasIndex(p => p.Status)
      .HasFilter("\"DeletedAt\" IS NULL");
  }
}
