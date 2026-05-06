using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

/// <summary>
///     EF Core model configuration for the Learning.Courses module.
///     Discovered by the main API database context via assembly scanning.
/// </summary>
public sealed class CoursesModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Program).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Learning.Courses", StringComparison.Ordinal) == true);
    }
}
