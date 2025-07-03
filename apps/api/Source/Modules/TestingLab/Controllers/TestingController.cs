using Microsoft.AspNetCore.Mvc;
using GameGuild.Common.Entities;
using GameGuild.Common.Attributes;
using System.Security.Claims;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.TestingLab.Models;
using GameGuild.Modules.TestingLab.Services;
using GameGuild.Modules.TestingLab.Dtos;


namespace GameGuild.Modules.TestingLab.Controllers;

[ApiController]
[Route("[controller]")]
public class TestingController(ITestService testService) : ControllerBase {
  #region Testing Request Endpoints

  // GET: testing/requests
  [HttpGet("requests")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequests(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var requests = await testService.GetTestingRequestsAsync(skip, take);

    return Ok(requests);
  }

  // GET: testing/requests/{id}
  [HttpGet("requests/{id}")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<TestingRequest>> GetTestingRequest(Guid id) {
    var request = await testService.GetTestingRequestByIdAsync(id);

    if (request == null) return NotFound();

    return Ok(request);
  }

  // GET: testing/requests/{id}/details
  [HttpGet("requests/{id}/details")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<TestingRequest>> GetTestingRequestWithDetails(Guid id) {
    var request = await testService.GetTestingRequestByIdWithDetailsAsync(id);

    if (request == null) return NotFound();

    return Ok(request);
  } // POST: testing/requests

  [HttpPost("requests")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Create)]
  public async Task<ActionResult<TestingRequest>> CreateTestingRequest(CreateTestingRequestDto requestDto) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      var request = requestDto.ToTestingRequest(userId);
      var createdRequest = await testService.CreateTestingRequestAsync(request);

      return CreatedAtAction(nameof(GetTestingRequest), new { id = createdRequest.Id }, createdRequest);
    }
    catch (Exception ex) { return BadRequest($"Error creating testing request: {ex.Message}"); }
  }

  // PUT: testing/requests/{id}
  [HttpPut("requests/{id}")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Edit)]
  public async Task<ActionResult<TestingRequest>> UpdateTestingRequest(Guid id, TestingRequest request) {
    if (id != request.Id) return BadRequest("ID mismatch");

    try {
      var updatedRequest = await testService.UpdateTestingRequestAsync(request);

      return Ok(updatedRequest);
    }
    catch (InvalidOperationException ex) { return NotFound(ex.Message); }
    catch (Exception ex) { return BadRequest($"Error updating testing request: {ex.Message}"); }
  }

  // DELETE: testing/requests/{id}
  [HttpDelete("requests/{id}")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteTestingRequest(Guid id) {
    var result = await testService.DeleteTestingRequestAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  // POST: testing/requests/{id}/restore
  [HttpPost("requests/{id}/restore")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Edit)]
  public async Task<ActionResult> RestoreTestingRequest(Guid id) {
    var result = await testService.RestoreTestingRequestAsync(id);

    if (!result) return NotFound();

    return Ok();
  }

  #endregion

  #region Testing Session Endpoints

  // GET: testing/sessions
  [HttpGet("sessions")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessions(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var sessions = await testService.GetTestingSessionsAsync(skip, take);

    return Ok(sessions);
  }

  // GET: testing/sessions/{id}
  [HttpGet("sessions/{id}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<TestingSession>> GetTestingSession(Guid id) {
    var session = await testService.GetTestingSessionByIdAsync(id);

    if (session == null) return NotFound();

    return Ok(session);
  }

  // GET: testing/sessions/{id}/details
  [HttpGet("sessions/{id}/details")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<TestingSession>> GetTestingSessionWithDetails(Guid id) {
    var session = await testService.GetTestingSessionByIdWithDetailsAsync(id);

    if (session == null) return NotFound();

    return Ok(session);
  }

  // POST: testing/sessions
  [HttpPost("sessions")]
  [RequireResourcePermission<TestingSession>(PermissionType.Create)]
  public async Task<ActionResult<TestingSession>> CreateTestingSession(TestingSession session) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      session.CreatedById = userId;
      var createdSession = await testService.CreateTestingSessionAsync(session);

      return CreatedAtAction(nameof(GetTestingSession), new { id = createdSession.Id }, createdSession);
    }
    catch (Exception ex) { return BadRequest($"Error creating testing session: {ex.Message}"); }
  }

  // PUT: testing/sessions/{id}
  [HttpPut("sessions/{id}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Edit)]
  public async Task<ActionResult<TestingSession>> UpdateTestingSession(Guid id, TestingSession session) {
    if (id != session.Id) return BadRequest("ID mismatch");

    try {
      var updatedSession = await testService.UpdateTestingSessionAsync(session);

      return Ok(updatedSession);
    }
    catch (InvalidOperationException ex) { return NotFound(ex.Message); }
    catch (Exception ex) { return BadRequest($"Error updating testing session: {ex.Message}"); }
  }

  // DELETE: testing/sessions/{id}
  [HttpDelete("sessions/{id}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteTestingSession(Guid id) {
    var result = await testService.DeleteTestingSessionAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  // POST: testing/sessions/{id}/restore
  [HttpPost("sessions/{id}/restore")]
  [RequireResourcePermission<TestingSession>(PermissionType.Edit)]
  public async Task<ActionResult> RestoreTestingSession(Guid id) {
    var result = await testService.RestoreTestingSessionAsync(id);

    if (!result) return NotFound();

    return Ok();
  }

  #endregion

  #region Filtered Query Endpoints

  // GET: testing/requests/by-project-version/{projectVersionId}
  [HttpGet("requests/by-project-version/{projectVersionId}")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequestsByProjectVersion(Guid projectVersionId) {
    var requests = await testService.GetTestingRequestsByProjectVersionAsync(projectVersionId);

    return Ok(requests);
  }

  // GET: testing/requests/by-creator/{creatorId}
  [HttpGet("requests/by-creator/{creatorId}")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequestsByCreator(Guid creatorId) {
    var requests = await testService.GetTestingRequestsByCreatorAsync(creatorId);

    return Ok(requests);
  }

  // GET: testing/requests/by-status/{status}
  [HttpGet("requests/by-status/{status}")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequestsByStatus(TestingRequestStatus status) {
    var requests = await testService.GetTestingRequestsByStatusAsync(status);

    return Ok(requests);
  }

  // GET: testing/sessions/by-request/{testingRequestId}
  [HttpGet("sessions/by-request/{testingRequestId}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByRequest(Guid testingRequestId) {
    var sessions = await testService.GetTestingSessionsByRequestAsync(testingRequestId);

    return Ok(sessions);
  }

  // GET: testing/sessions/by-location/{locationId}
  [HttpGet("sessions/by-location/{locationId}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByLocation(Guid locationId) {
    var sessions = await testService.GetTestingSessionsByLocationAsync(locationId);

    return Ok(sessions);
  }

  // GET: testing/sessions/by-status/{status}
  [HttpGet("sessions/by-status/{status}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByStatus(SessionStatus status) {
    var sessions = await testService.GetTestingSessionsByStatusAsync(status);

    return Ok(sessions);
  }

  // GET: testing/sessions/by-manager/{managerId}
  [HttpGet("sessions/by-manager/{managerId}")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByManager(Guid managerId) {
    var sessions = await testService.GetTestingSessionsByManagerAsync(managerId);

    return Ok(sessions);
  }

  // GET: testing/requests/search
  [HttpGet("requests/search")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> SearchTestingRequests([FromQuery] string searchTerm) {
    if (string.IsNullOrWhiteSpace(searchTerm)) return BadRequest("Search term is required");

    var requests = await testService.SearchTestingRequestsAsync(searchTerm);

    return Ok(requests);
  }

  // GET: testing/sessions/search
  [HttpGet("sessions/search")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingSession>>> SearchTestingSessions([FromQuery] string searchTerm) {
    if (string.IsNullOrWhiteSpace(searchTerm)) return BadRequest("Search term is required");

    var sessions = await testService.SearchTestingSessionsAsync(searchTerm);

    return Ok(sessions);
  }

  #endregion

  #region Participant Management Endpoints

  // POST: testing/requests/{requestId}/participants/{userId}
  [HttpPost("requests/{requestId}/participants/{userId}")]
  [RequireResourcePermission<TestingParticipant>(PermissionType.Create)]
  public async Task<ActionResult<TestingParticipant>> AddParticipant(Guid requestId, Guid userId) {
    try {
      var participant = await testService.AddParticipantAsync(requestId, userId);

      return Ok(participant);
    }
    catch (Exception ex) { return BadRequest($"Error adding participant: {ex.Message}"); }
  }

  // DELETE: testing/requests/{requestId}/participants/{userId}
  [HttpDelete("requests/{requestId}/participants/{userId}")]
  [RequireResourcePermission<TestingParticipant>(PermissionType.Delete)]
  public async Task<ActionResult> RemoveParticipant(Guid requestId, Guid userId) {
    var result = await testService.RemoveParticipantAsync(requestId, userId);

    if (!result) return NotFound();

    return NoContent();
  }

  // GET: testing/requests/{requestId}/participants
  [HttpGet("requests/{requestId}/participants")]
  [RequireResourcePermission<TestingParticipant>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingParticipant>>> GetTestingRequestParticipants(Guid requestId) {
    var participants = await testService.GetTestingRequestParticipantsAsync(requestId);

    return Ok(participants);
  }

  // GET: testing/requests/{requestId}/participants/{userId}/check
  [HttpGet("requests/{requestId}/participants/{userId}/check")]
  [RequireResourcePermission<TestingParticipant>(PermissionType.Read)]
  public async Task<ActionResult<bool>> CheckUserParticipation(Guid requestId, Guid userId) {
    var isParticipant = await testService.IsUserParticipantAsync(requestId, userId);

    return Ok(isParticipant);
  }

  #endregion

  #region Session Registration Endpoints

  // POST: testing/sessions/{sessionId}/register
  [HttpPost("sessions/{sessionId}/register")]
  [RequireResourcePermission<SessionRegistration>(PermissionType.Create)]
  public async Task<ActionResult<SessionRegistration>> RegisterForSession(
    Guid sessionId,
    [FromBody] SessionRegistrationRequest request
  ) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      var registration =
        await testService.RegisterForSessionAsync(sessionId, userId, request.RegistrationType, request.Notes);

      return Ok(registration);
    }
    catch (Exception ex) { return BadRequest($"Error registering for session: {ex.Message}"); }
  }

  // DELETE: testing/sessions/{sessionId}/register
  [HttpDelete("sessions/{sessionId}/register")]
  [RequireResourcePermission<SessionRegistration>(PermissionType.Delete)]
  public async Task<ActionResult> UnregisterFromSession(Guid sessionId) {
    // Get the current authenticated user's ID
    var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

    var result = await testService.UnregisterFromSessionAsync(sessionId, userId);

    if (!result) return NotFound();

    return NoContent();
  }

  // GET: testing/sessions/{sessionId}/registrations
  [HttpGet("sessions/{sessionId}/registrations")]
  [RequireResourcePermission<SessionRegistration>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<SessionRegistration>>> GetSessionRegistrations(Guid sessionId) {
    var registrations = await testService.GetSessionRegistrationsAsync(sessionId);

    return Ok(registrations);
  }

  // POST: testing/sessions/{sessionId}/waitlist
  [HttpPost("sessions/{sessionId}/waitlist")]
  [RequireResourcePermission<SessionWaitlist>(PermissionType.Create)]
  public async Task<ActionResult<SessionWaitlist>> AddToWaitlist(
    Guid sessionId,
    [FromBody] SessionRegistrationRequest request
  ) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      var waitlistEntry =
        await testService.AddToWaitlistAsync(sessionId, userId, request.RegistrationType, request.Notes);

      return Ok(waitlistEntry);
    }
    catch (Exception ex) { return BadRequest($"Error adding to waitlist: {ex.Message}"); }
  }

  // DELETE: testing/sessions/{sessionId}/waitlist
  [HttpDelete("sessions/{sessionId}/waitlist")]
  [RequireResourcePermission<SessionWaitlist>(PermissionType.Delete)]
  public async Task<ActionResult> RemoveFromWaitlist(Guid sessionId) {
    // Get the current authenticated user's ID
    var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

    var result = await testService.RemoveFromWaitlistAsync(sessionId, userId);

    if (!result) return NotFound();

    return NoContent();
  }

  // GET: testing/sessions/{sessionId}/waitlist
  [HttpGet("sessions/{sessionId}/waitlist")]
  [RequireResourcePermission<SessionWaitlist>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<SessionWaitlist>>> GetSessionWaitlist(Guid sessionId) {
    var waitlist = await testService.GetSessionWaitlistAsync(sessionId);

    return Ok(waitlist);
  }

  #endregion

  #region Feedback Endpoints

  // POST: testing/requests/{requestId}/feedback
  [HttpPost("requests/{requestId}/feedback")]
  [RequireResourcePermission<TestingFeedback>(PermissionType.Create)]
  public async Task<ActionResult<TestingFeedback>> AddFeedback(Guid requestId, [FromBody] FeedbackRequest request) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      var feedback = await testService.AddFeedbackAsync(
                       requestId,
                       userId,
                       request.FeedbackFormId,
                       request.FeedbackData,
                       request.TestingContext,
                       request.SessionId,
                       request.AdditionalNotes
                     );

      return Ok(feedback);
    }
    catch (Exception ex) { return BadRequest($"Error adding feedback: {ex.Message}"); }
  }

  // GET: testing/requests/{requestId}/feedback
  [HttpGet("requests/{requestId}/feedback")]
  [RequireResourcePermission<TestingFeedback>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingFeedback>>> GetTestingRequestFeedback(Guid requestId) {
    var feedback = await testService.GetTestingRequestFeedbackAsync(requestId);

    return Ok(feedback);
  }

  // GET: testing/feedback/by-user/{userId}
  [HttpGet("feedback/by-user/{userId}")]
  [RequireResourcePermission<TestingFeedback>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingFeedback>>> GetFeedbackByUser(Guid userId) {
    var feedback = await testService.GetFeedbackByUserAsync(userId);

    return Ok(feedback);
  }

  #endregion

  #region Statistics Endpoints

  // GET: testing/requests/{requestId}/statistics
  [HttpGet("requests/{requestId}/statistics")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<object>> GetTestingRequestStatistics(Guid requestId) {
    var statistics = await testService.GetTestingRequestStatisticsAsync(requestId);

    return Ok(statistics);
  }

  // GET: testing/sessions/{sessionId}/statistics
  [HttpGet("sessions/{sessionId}/statistics")]
  [RequireResourcePermission<TestingSession>(PermissionType.Read)]
  public async Task<ActionResult<object>> GetTestingSessionStatistics(Guid sessionId) {
    var statistics = await testService.GetTestingSessionStatisticsAsync(sessionId);

    return Ok(statistics);
  }

  // GET: testing/users/{userId}/activity
  [HttpGet("users/{userId}/activity")]
  [RequireResourcePermission<TestingParticipant>(PermissionType.Read)]
  public async Task<ActionResult<object>> GetUserTestingActivity(Guid userId) {
    var activity = await testService.GetUserTestingActivityAsync(userId);

    return Ok(activity);
  }

  #endregion
}

// DTOs for request bodies
public class SessionRegistrationRequest {
  public RegistrationType RegistrationType { get; set; }

  public string? Notes { get; set; }
}

public class FeedbackRequest {
  public Guid FeedbackFormId { get; set; }

  public string FeedbackData { get; set; } = string.Empty;

  public TestingContext TestingContext { get; set; }

  public Guid? SessionId { get; set; }

  public string? AdditionalNotes { get; set; }
}
