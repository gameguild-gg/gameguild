using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Programs.DTOs;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.SharedKernel.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Users.Entities;
﻿namespace GameGuild.Modules.Programs;

/// <summary> EntityBase Framework configuration for ProgramUser entity </summary>
public class ProgramUserConfiguration : IEntityTypeConfiguration<ProgramUser> {
  public void Configure(EntityTypeBuilder<ProgramUser> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pu => pu.Program).WithMany(p => p.ProgramUsers).HasForeignKey(pu => pu.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(pu => pu.User).WithMany().HasForeignKey(pu => pu.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
