using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;
using GameGuild.Modules.User.Models;
using GameGuild.Modules.Project.Models;


namespace GameGuild.Modules.TestingLab.Models {
  public class TestingRequest : BaseEntity {
    private Guid _projectVersionId;

    private ProjectVersion _projectVersion = null!;

    private string _title = string.Empty;

    private string? _description;

    private InstructionType _instructionsType;

    private string? _instructionsContent;

    private string? _instructionsUrl;

    private Guid? _instructionsFileId;

    private int? _maxTesters;

    private int _currentTesterCount = 0;

    private DateTime _startDate;

    private DateTime _endDate;

    private TestingRequestStatus _status = TestingRequestStatus.Draft;

    private Guid _createdById;

    private User.Models.User _createdBy = null!;

    /// <summary>
    /// Foreign key to the project version
    /// </summary>
    public Guid ProjectVersionId {
      get => _projectVersionId;
      set => _projectVersionId = value;
    }

    /// <summary>
    /// Navigation property to the project version
    /// </summary>
    public virtual ProjectVersion ProjectVersion {
      get => _projectVersion;
      set => _projectVersion = value;
    }

    [Required, MaxLength(255)]
    public string Title {
      get => _title;
      set => _title = value;
    }

    public string? Description {
      get => _description;
      set => _description = value;
    }

    [Required]
    public InstructionType InstructionsType {
      get => _instructionsType;
      set => _instructionsType = value;
    }

    public string? InstructionsContent {
      get => _instructionsContent;
      set => _instructionsContent = value;
    }

    [MaxLength(500)]
    public string? InstructionsUrl {
      get => _instructionsUrl;
      set => _instructionsUrl = value;
    }

    public Guid? InstructionsFileId {
      get => _instructionsFileId;
      set => _instructionsFileId = value;
    }

    public int? MaxTesters {
      get => _maxTesters;
      set => _maxTesters = value;
    }

    public int CurrentTesterCount {
      get => _currentTesterCount;
      set => _currentTesterCount = value;
    }

    [Required]
    public DateTime StartDate {
      get => _startDate;
      set => _startDate = value;
    }

    [Required]
    public DateTime EndDate {
      get => _endDate;
      set => _endDate = value;
    }

    [Required]
    public TestingRequestStatus Status {
      get => _status;
      set => _status = value;
    }

    /// <summary>
    /// Foreign key to the user who created this request
    /// </summary>
    public Guid CreatedById {
      get => _createdById;
      set => _createdById = value;
    }

    /// <summary>
    /// Navigation property to the user who created this request
    /// </summary>
    public virtual Modules.User.Models.User CreatedBy {
      get => _createdBy;
      set => _createdBy = value;
    }
  }
}
