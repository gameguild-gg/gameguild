

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Users;
﻿namespace GameGuild.Programs;

/// <summary> EntityBase Framework configuration for ContentInteraction entity </summary>
public class ContentInteractionConfiguration : IEntityTypeConfiguration<ContentInteraction> {
  public void Configure(EntityTypeBuilder<ContentInteraction> builder) {
    // Configure relationship with ProgramUser (can't be done with annotations)
    builder.HasOne(ci => ci.ProgramUser).WithMany(pu => pu.ContentInteractions).HasForeignKey(ci => ci.ProgramUserId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Content (can't be done with annotations)
    builder.HasOne(ci => ci.Content).WithMany().HasForeignKey(ci => ci.ContentId).OnDelete(DeleteBehavior.Cascade);
  }
}
