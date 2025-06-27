using GameGuild.Modules.TestingLab.Models;
using GameGuild.Modules.TestingLab.Services;
using GameGuild.Modules.TestingLab.Dtos;
using System.Security.Claims;


namespace GameGuild.Modules.TestingLab.GraphQL;

/// <summary>
/// GraphQL mutations for TestingLab module
/// </summary>
[ExtendObjectType("Mutation")]
public class TestingLabMutations {
  /// <summary>
  /// Create a new testing request
  /// </summary>
  public async Task<TestingRequest> CreateTestingRequest(
    [Service] ITestService testService,
    ClaimsPrincipal claimsPrincipal, CreateTestingRequestDto input
  ) {
    // Get the current authenticated user's ID
    string? userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId)) { throw new UnauthorizedAccessException("User ID not found in token"); }

    var request = input.ToTestingRequest(userId);

    return await testService.CreateTestingRequestAsync(request);
  }

  /// <summary>
  /// Update an existing testing request
  /// </summary>
  public async Task<TestingRequest> UpdateTestingRequest(
    [Service] ITestService testService, Guid id,
    UpdateTestingRequestDto input
  ) {
    var existingRequest = await testService.GetTestingRequestByIdAsync(id);

    if (existingRequest == null) { throw new ArgumentException($"Testing request with ID {id} not found"); }

    // Update properties using the DTO
    input.UpdateTestingRequest(existingRequest);

    return await testService.UpdateTestingRequestAsync(existingRequest);
  }

  /// <summary>
  /// Delete a testing request
  /// </summary>
  public async Task<bool> DeleteTestingRequest([Service] ITestService testService, Guid id) { return await testService.DeleteTestingRequestAsync(id); }

  /// <summary>
  /// Create a new testing session
  /// </summary>
  public async Task<TestingSession> CreateTestingSession(
    [Service] ITestService testService,
    ClaimsPrincipal claimsPrincipal, CreateTestingSessionDto input
  ) {
    // Get the current authenticated user's ID
    string? userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId)) { throw new UnauthorizedAccessException("User ID not found in token"); }

    var session = input.ToTestingSession(userId);

    return await testService.CreateTestingSessionAsync(session);
  }

  /// <summary>
  /// Update an existing testing session
  /// </summary>
  public async Task<TestingSession> UpdateTestingSession(
    [Service] ITestService testService, Guid id,
    CreateTestingSessionDto input
  ) {
    var existingSession = await testService.GetTestingSessionByIdAsync(id);

    if (existingSession == null) { throw new ArgumentException($"Testing session with ID {id} not found"); }

    // Update properties
    existingSession.TestingRequestId = input.TestingRequestId;
    existingSession.LocationId = input.LocationId;
    existingSession.SessionName = input.SessionName;
    existingSession.SessionDate = input.SessionDate;
    existingSession.StartTime = input.StartTime;
    existingSession.EndTime = input.EndTime;
    existingSession.MaxTesters = input.MaxTesters;
    existingSession.Status = input.Status;
    existingSession.ManagerUserId = input.ManagerUserId;

    return await testService.UpdateTestingSessionAsync(existingSession);
  }

  /// <summary>
  /// Delete a testing session
  /// </summary>
  public async Task<bool> DeleteTestingSession([Service] ITestService testService, Guid id) { return await testService.DeleteTestingSessionAsync(id); }
}
