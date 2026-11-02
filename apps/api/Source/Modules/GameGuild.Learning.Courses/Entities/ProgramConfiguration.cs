
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

    // Catalog queries filter DeletedAt == null with Status/Visibility equality
    // (GetPublishedProgramBySlugQueryHandler) — issue #1157. Distinct property
    // list from the annotation-declared single-column Status index.
    builder.HasIndex(p => new { p.Status, p.Visibility })
      .HasDatabaseName("ix_programs_status_visibility_live")
      .HasFilter("\"DeletedAt\" IS NULL");
  }
}
