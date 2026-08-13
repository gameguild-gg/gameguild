
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

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
      .HasPrecision(5, 2)
      .HasDefaultValue(60m);
  }
}
