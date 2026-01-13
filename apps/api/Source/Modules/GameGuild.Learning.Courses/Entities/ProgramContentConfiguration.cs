

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Users;
﻿namespace GameGuild.Programs;

/// <summary> EntityBase Framework configuration for ProgramContent entity </summary>
public class ProgramContentConfiguration : IEntityTypeConfiguration<ProgramContent> {
  public void Configure(EntityTypeBuilder<ProgramContent> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pc => pc.Program).WithMany(p => p.ProgramContents).HasForeignKey(pc => pc.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Parent (self-referencing, can't be done with annotations)
    builder.HasOne(pc => pc.Parent).WithMany(pc => pc.Children).HasForeignKey(pc => pc.ParentId).OnDelete(DeleteBehavior.Restrict);
  }
}
