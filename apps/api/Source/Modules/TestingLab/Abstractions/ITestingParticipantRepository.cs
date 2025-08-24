namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary> Repository abstraction for Testing Participant operations </summary>
public interface ITestingParticipantRepository : IRepository<TestingParticipant> {
  /// <summary> Get participants with pagination </summary>
  Task<IEnumerable<TestingParticipant>> GetWithPaginationAsync(int skip = 0, int take = 50);

  /// <summary> Get participants by testing session </summary>
  Task<IEnumerable<TestingParticipant>> GetByTestingSessionAsync(Guid testingSessionId);

  /// <summary> Get participants by user </summary>
  Task<IEnumerable<TestingParticipant>> GetByUserAsync(Guid userId);

  /// <summary> Get participants by attendance status </summary>
  Task<IEnumerable<TestingParticipant>> GetByAttendanceStatusAsync(AttendanceStatus status);

  /// <summary> Check if user is already a participant in a testing session </summary>
  Task<bool> IsUserParticipantAsync(Guid userId, Guid testingSessionId);

  /// <summary> Get participant count for a testing session </summary>
  Task<int> GetParticipantCountAsync(Guid testingSessionId);

  /// <summary> Get active participants for a testing session </summary>
  Task<IEnumerable<TestingParticipant>> GetActiveParticipantsAsync(Guid testingSessionId);
}
