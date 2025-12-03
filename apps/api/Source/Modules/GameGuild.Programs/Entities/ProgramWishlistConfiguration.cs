using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Programs.DTOs;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.SharedKernel.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Users.Entities;
﻿namespace GameGuild.Modules.Programs;

/// <summary> EntityBase Framework configuration for ProgramWishlist entity </summary>
public class ProgramWishlistConfiguration : IEntityTypeConfiguration<ProgramWishlist> {
  public void Configure(EntityTypeBuilder<ProgramWishlist> builder) {
    // Additional configuration if needed
  }
}
