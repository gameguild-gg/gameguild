using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.TestingLab.Models;


namespace GameGuild.Modules.TestingLab.Dtos {
  public class CreateTestingRequestDto {
    [Required] public Guid ProjectVersionId { get; set; }

    [Required] [MaxLength(255)] public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required] public InstructionType InstructionsType { get; set; }

    public string? InstructionsContent { get; set; }

    [MaxLength(500)] public string? InstructionsUrl { get; set; }

    public Guid? InstructionsFileId { get; set; }

    public int? MaxTesters { get; set; }

    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    [Required] public TestingRequestStatus Status { get; set; }

    public TestingRequest ToTestingRequest(Guid createdById) {
      return new TestingRequest {
        ProjectVersionId = ProjectVersionId,
        Title = Title,
        Description = Description,
        InstructionsType = InstructionsType,
        InstructionsContent = InstructionsContent,
        InstructionsUrl = InstructionsUrl,
        InstructionsFileId = InstructionsFileId,
        MaxTesters = MaxTesters,
        StartDate = StartDate,
        EndDate = EndDate,
        Status = Status,
        CreatedById = createdById,
      };
    }
  }
}
