

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

 namespace GameGuild.Learning.Courses;

/// <summary> EntityBase Framework configuration for ProgramContent entity </summary>
public class ProgramContentConfiguration : IEntityTypeConfiguration<ProgramContent> {
  public void Configure(EntityTypeBuilder<ProgramContent> builder) {
    builder.ToTable(table =>
    {
      table.HasCheckConstraint(
        "CK_program_contents_Lesson_NotGraded",
        "\"Type\" NOT IN (0, 1) OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");
      table.HasCheckConstraint(
        "CK_program_contents_LessonFormat",
        "((\"Type\" IN (0, 1)) AND \"LessonFormat\" IN (0, 1, 2, 3, 4, 5)) OR ((\"Type\" NOT IN (0, 1)) AND \"LessonFormat\" IS NULL)");
      table.HasCheckConstraint(
        "CK_program_contents_Survey_NotGraded",
        "\"Type\" <> 8 OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");
    });

    builder.Property(content => content.ActivitySettingsData).HasColumnType("jsonb");

    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pc => pc.Program).WithMany(p => p.ProgramContents).HasForeignKey(pc => pc.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Parent (self-referencing, can't be done with annotations)
    builder.HasOne(pc => pc.Parent).WithMany(pc => pc.Children).HasForeignKey(pc => pc.ParentId).OnDelete(DeleteBehavior.Restrict);
  }
}
