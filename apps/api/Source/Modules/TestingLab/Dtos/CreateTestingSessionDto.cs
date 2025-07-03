using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.TestingLab.Models;


namespace GameGuild.Modules.TestingLab.Dtos {
  public class CreateTestingSessionDto {
    [Required]
    public Guid TestingRequestId { get; set; }

    [Required]
    public Guid LocationId { get; set; }

    [Required][MaxLength(255)]
    public string SessionName { get; set; } = string.Empty;

    [Required]
    public DateTime SessionDate { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    public int MaxTesters { get; set; }

    [Required]
    public SessionStatus Status { get; set; }

    [Required]
    public Guid ManagerUserId { get; set; }

    public TestingSession ToTestingSession(Guid createdById) {
      return new TestingSession {
        TestingRequestId = TestingRequestId,
        LocationId = LocationId,
        SessionName = SessionName,
        SessionDate = SessionDate,
        StartTime = StartTime,
        EndTime = EndTime,
        MaxTesters = MaxTesters,
        Status = Status,
        ManagerUserId = ManagerUserId,
        CreatedById = createdById,
      };
    }
  }
}
