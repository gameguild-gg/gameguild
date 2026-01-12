

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Users;
﻿namespace GameGuild.Programs;

/// <summary> EntityBase Framework configuration for ProgramWishlist entity </summary>
public class ProgramWishlistConfiguration : IEntityTypeConfiguration<ProgramWishlist> {
  public void Configure(EntityTypeBuilder<ProgramWishlist> builder) {
    // Additional configuration if needed
  }
}
