

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

 namespace GameGuild.Learning.Courses;

/// <summary> EntityBase Framework configuration for ProgramContent entity </summary>
public class ProgramContentConfiguration : IEntityTypeConfiguration<ProgramContent> {
  public void Configure(EntityTypeBuilder<ProgramContent> builder) {
    builder.ToTable(table =>
    {
      table.HasCheckConstraint(
        "CK_program_contents_LessonFormat",
        "((\"Type\" IN (0, 1)) AND \"LessonFormat\" IN (0, 1, 2, 3, 4, 5)) OR ((\"Type\" NOT IN (0, 1)) AND \"LessonFormat\" IS NULL)");
    });

    builder.Property(content => content.ActivitySettingsData).HasColumnType("jsonb");
    builder.Property(content => content.JsonBody).HasColumnType("jsonb");

    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pc => pc.Program).WithMany(p => p.ProgramContents).HasForeignKey(pc => pc.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Parent (self-referencing, can't be done with annotations)
    builder.HasOne(pc => pc.Parent).WithMany(pc => pc.Children).HasForeignKey(pc => pc.ParentId).OnDelete(DeleteBehavior.Restrict);

    // Program detail pages load live content trees (ProgramId + DeletedAt == null,
    // ordered by SortOrder) — issue #1157. Distinct property list from the
    // annotation-declared single-column ProgramId index.
    builder.HasIndex(pc => new { pc.ProgramId, pc.SortOrder })
      .HasDatabaseName("ix_program_contents_program_sort_live")
      .HasFilter("\"DeletedAt\" IS NULL");
  }
}
