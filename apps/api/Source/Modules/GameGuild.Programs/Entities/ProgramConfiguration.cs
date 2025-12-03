using GameGuild.Modules.Programs.DTOs;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Users.Entities;
﻿namespace GameGuild.Modules.Programs;

/// <summary> EntityBase Framework configuration for Program entity </summary>
public class ProgramConfiguration : IEntityTypeConfiguration<Program> {
  public void Configure(EntityTypeBuilder<Program> builder) {
    // Ignore computed properties that shouldn't be mapped by EF Core
    builder.Ignore(p => p.SkillsRequired);
    builder.Ignore(p => p.SkillsProvided);
    builder.Ignore(p => p.AverageRating);
    builder.Ignore(p => p.TotalRatings);
  }
}
