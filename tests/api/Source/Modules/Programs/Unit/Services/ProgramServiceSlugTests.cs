using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Programs;
using Microsoft.EntityFrameworkCore;
using ProgramEntity = GameGuild.Modules.Programs.Program;


namespace GameGuild.Tests.Modules.Programs.Unit.Services;

/// <summary>
/// Unit tests for ProgramService slug-based operations
/// Tests business logic with in-memory database
/// </summary>
public class ProgramServiceSlugTests : IDisposable {
  private readonly ApplicationDbContext _context;
  private readonly ProgramService _programService;

  public ProgramServiceSlugTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    _context = new ApplicationDbContext(options);
    _programService = new ProgramService(_context);
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithValidSlug_ReturnsProgram() {
    // Arrange
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = "Unity Game Development",
      Slug = "unity-game-development",
      Description = "Learn Unity game development from scratch",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Category = ProgramCategory.Programming,
      Difficulty = ProgramDifficulty.Beginner,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();

    // Act
    var result = await _programService.GetProgramBySlugAsync("unity-game-development");

    // Assert
    Assert.NotNull(result);
    Assert.Equal(program.Id, result.Id);
    Assert.Equal(program.Slug, result.Slug);
    Assert.Equal(program.Title, result.Title);
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithInvalidSlug_ReturnsNull() {
    // Arrange
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = "Unity Game Development",
      Slug = "unity-game-development",
      Description = "Learn Unity game development from scratch",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Category = ProgramCategory.Programming,
      Difficulty = ProgramDifficulty.Beginner,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();

    // Act
    var result = await _programService.GetProgramBySlugAsync("non-existent-slug");

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithDeletedProgram_ReturnsNull() {
    // Arrange
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = "Unity Game Development",
      Slug = "unity-game-development",
      Description = "Learn Unity game development from scratch",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Category = ProgramCategory.Programming,
      Difficulty = ProgramDifficulty.Beginner,
      DeletedAt = DateTime.UtcNow, // Mark as deleted
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();

    // Act
    var result = await _programService.GetProgramBySlugAsync("unity-game-development");

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithCaseSensitiveSlug_IsExact() {
    // Arrange
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = "Unity Game Development",
      Slug = "Unity-Game-Development", // Mixed case
      Description = "Learn Unity game development from scratch",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Category = ProgramCategory.Programming,
      Difficulty = ProgramDifficulty.Beginner,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();

    // Act
    var exactMatch = await _programService.GetProgramBySlugAsync("Unity-Game-Development");
    var lowerCaseMatch = await _programService.GetProgramBySlugAsync("unity-game-development");

    // Assert
    Assert.NotNull(exactMatch);
    Assert.Null(lowerCaseMatch); // Should be case-sensitive
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithSpecialCharacters_HandlesCorrectly() {
    // Arrange
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = "C# Programming & .NET Development",
      Slug = "c-sharp-programming-and-dotnet-development",
      Description = "Learn C# and .NET development",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Category = ProgramCategory.Programming,
      Difficulty = ProgramDifficulty.Intermediate,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();

    // Act
    var result = await _programService.GetProgramBySlugAsync("c-sharp-programming-and-dotnet-development");

    // Assert
    Assert.NotNull(result);
    Assert.Equal(program.Slug, result.Slug);
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithMultiplePrograms_ReturnsCorrectOne() {
    // Arrange
    var programs = new[] {
      new ProgramEntity {
        Id = Guid.NewGuid(),
        Title = "Unity Game Development",
        Slug = "unity-game-development",
        Description = "Learn Unity",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Beginner,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new ProgramEntity {
        Id = Guid.NewGuid(),
        Title = "Unreal Engine Development",
        Slug = "unreal-engine-development",
        Description = "Learn Unreal Engine",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Advanced,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new ProgramEntity {
        Id = Guid.NewGuid(),
        Title = "Blender 3D Modeling",
        Slug = "blender-3d-modeling",
        Description = "Learn 3D modeling with Blender",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Beginner,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      }
    };

    _context.Programs.AddRange(programs);
    await _context.SaveChangesAsync();

    // Act
    var unityProgram = await _programService.GetProgramBySlugAsync("unity-game-development");
    var unrealProgram = await _programService.GetProgramBySlugAsync("unreal-engine-development");
    var blenderProgram = await _programService.GetProgramBySlugAsync("blender-3d-modeling");

    // Assert
    Assert.NotNull(unityProgram);
    Assert.NotNull(unrealProgram);
    Assert.NotNull(blenderProgram);
    
    Assert.Equal(programs[0].Id, unityProgram.Id);
    Assert.Equal(programs[1].Id, unrealProgram.Id);
    Assert.Equal(programs[2].Id, blenderProgram.Id);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public async Task GetProgramBySlugAsync_WithInvalidSlugValues_ReturnsNull(string invalidSlug) {
    // Act
    var result = await _programService.GetProgramBySlugAsync(invalidSlug);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task GetProgramBySlugAsync_WithVeryLongSlug_HandlesCorrectly() {
    // Arrange
    var longSlug = string.Join("-", Enumerable.Repeat("very-long-slug-segment", 10));
    
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = "Test Program with Very Long Slug",
      Slug = longSlug,
      Description = "Test program description",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Category = ProgramCategory.Programming,
      Difficulty = ProgramDifficulty.Beginner,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();

    // Act
    var result = await _programService.GetProgramBySlugAsync(longSlug);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(program.Slug, result.Slug);
  }

  public void Dispose() {
    _context.Dispose();
  }
}
