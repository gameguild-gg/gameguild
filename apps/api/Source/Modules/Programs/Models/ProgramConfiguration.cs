using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Entity Framework configuration for Program entity
/// </summary>
public class ProgramConfiguration : IEntityTypeConfiguration<Program> {
  public void Configure(EntityTypeBuilder<Program> builder) {
    // Additional configuration can be added here if needed
  }
}
