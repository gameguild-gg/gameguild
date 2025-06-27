using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.TestingLab.Models {
  public class TestingFeedback : BaseEntity {
    private Guid _testingRequestId;

    private TestingRequest _testingRequest = null!;

    private Guid _feedbackFormId;

    private TestingFeedbackForm _feedbackForm = null!;

    private Guid _userId;

    private User.Models.User _user = null!;

    private Guid? _sessionId;

    private TestingSession? _session;

    private TestingContext _testingContext;

    private string _feedbackData = string.Empty;

    private string? _additionalNotes;

    /// <summary>
    /// Foreign key to the testing request
    /// </summary>
    public Guid TestingRequestId {
      get => _testingRequestId;
      set => _testingRequestId = value;
    }

    /// <summary>
    /// Navigation property to the testing request
    /// </summary>
    public virtual TestingRequest TestingRequest {
      get => _testingRequest;
      set => _testingRequest = value;
    }

    /// <summary>
    /// Foreign key to the feedback form
    /// </summary>
    public Guid FeedbackFormId {
      get => _feedbackFormId;
      set => _feedbackFormId = value;
    }

    /// <summary>
    /// Navigation property to the feedback form
    /// </summary>
    public virtual TestingFeedbackForm FeedbackForm {
      get => _feedbackForm;
      set => _feedbackForm = value;
    }

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    public Guid UserId {
      get => _userId;
      set => _userId = value;
    }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual Modules.User.Models.User User {
      get => _user;
      set => _user = value;
    }

    /// <summary>
    /// Optional foreign key to the session
    /// </summary>
    public Guid? SessionId {
      get => _sessionId;
      set => _sessionId = value;
    }

    /// <summary>
    /// Navigation property to the session (optional)
    /// </summary>
    public virtual TestingSession? Session {
      get => _session;
      set => _session = value;
    }

    [Required]
    public TestingContext TestingContext {
      get => _testingContext;
      set => _testingContext = value;
    }

    [Required]
    public string FeedbackData {
      get => _feedbackData;
      set => _feedbackData = value;
    } // JSON

    public string? AdditionalNotes {
      get => _additionalNotes;
      set => _additionalNotes = value;
    }
  }
}
