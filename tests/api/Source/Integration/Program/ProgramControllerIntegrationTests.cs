using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Programs.DTOs;
using GameGuild.Modules.Programs.Services;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Tests.Integration.Program;

public class ProgramControllerIntegrationTests {
  private static ApplicationDbContext GetInMemoryContext() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(Guid.NewGuid().ToString())
                  .Options;

    return new ApplicationDbContext(options);
  }

  [Fact]
  public async Task ProgramService_CreateProgram_Should_Work() {
    // Arrange
    using var context = GetInMemoryContext();
    var programService = new ProgramService(context);

    var createDto = new CreateProgramDto("Test Program", "Test Description", "test-program");

    // Act
    var program = await programService.CreateProgramAsync(createDto);

    // Assert
    Assert.NotNull(program);
    Assert.Equal("Test Program", program.Title);
    Assert.Equal("Test Description", program.Description);
    Assert.Equal("test-program", program.Slug);
    Assert.NotEqual(Guid.Empty, program.Id);
  }

  [Fact]
  public async Task ProgramService_GetPrograms_Should_Return_Empty_Initially() {
    // Arrange
    using var context = GetInMemoryContext();
    var programService = new ProgramService(context);

    // Act
    var programs = await programService.GetProgramsAsync();

    // Assert
    Assert.NotNull(programs);
    Assert.Empty(programs);
  }

  [Fact]
  public async Task ProgramService_GetPublishedPrograms_Should_Return_Empty_Initially() {
    // Arrange
    using var context = GetInMemoryContext();
    var programService = new ProgramService(context);

    // Act
    var programs = await programService.GetPublishedProgramsAsync();

    // Assert
    Assert.NotNull(programs);
    Assert.Empty(programs);
  }

  [Fact]
  public async Task ProgramService_CreateAndRetrieveProgram_Should_Work() {
    // Arrange
    using var context = GetInMemoryContext();
    var programService = new ProgramService(context);

    var createDto = new CreateProgramDto(
      "Integration Test Program",
      "A program created for integration testing",
      "integration-test-program"
    );

    // Act
    var createdProgram = await programService.CreateProgramAsync(createDto);
    var retrievedProgram = await programService.GetProgramByIdAsync(createdProgram.Id);

    // Assert
    Assert.NotNull(retrievedProgram);
    Assert.Equal(createdProgram.Id, retrievedProgram.Id);
    Assert.Equal("Integration Test Program", retrievedProgram.Title);
    Assert.Equal("A program created for integration testing", retrievedProgram.Description);
    Assert.Equal("integration-test-program", retrievedProgram.Slug);
  }

  [Fact]
  public async Task ProgramService_UpdateProgram_Should_Work() {
    // Arrange
    using var context = GetInMemoryContext();
    var programService = new ProgramService(context);

    var createDto = new CreateProgramDto("Original Title", "Original Description", "original-slug");

    var createdProgram = await programService.CreateProgramAsync(createDto);

    var updateDto = new UpdateProgramDto("Updated Title", "Updated Description", "/path/to/new/thumbnail.jpg");

    // Act
    var updatedProgram = await programService.UpdateProgramAsync(createdProgram.Id, updateDto);

    // Assert
    Assert.NotNull(updatedProgram);
    Assert.Equal("Updated Title", updatedProgram.Title);
    Assert.Equal("Updated Description", updatedProgram.Description);
    Assert.Equal("/path/to/new/thumbnail.jpg", updatedProgram.Thumbnail);
  }

  [Fact]
  public async Task ProgramService_PublishProgram_Should_ChangeStatus() {
    // Arrange
    using var context = GetInMemoryContext();
    var programService = new ProgramService(context);

    var createDto = new CreateProgramDto("Program To Publish", "This program will be published", "program-to-publish");

    var createdProgram = await programService.CreateProgramAsync(createDto);

    // Act
    var publishedProgram = await programService.PublishProgramAsync(createdProgram.Id);

    // Assert
    Assert.NotNull(publishedProgram);
    Assert.Equal(ContentStatus.Published, publishedProgram.Status);

    // Verify it appears in published programs
    var publishedPrograms = await programService.GetPublishedProgramsAsync();
    Assert.Single(publishedPrograms);
    Assert.Equal(createdProgram.Id, publishedPrograms.First().Id);
  }
}
