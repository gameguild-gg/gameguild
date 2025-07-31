using System.Security.Claims;
using GameGuild.Common;
using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.TestingLab;

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
      // Validate model state
      if (!ModelState.IsValid) return BadRequest(ModelState);

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
  [RequireResourcePermission<SessionRegistration>(PermissionType.Create, "sessionId")]
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
  [RequireResourcePermission<SessionRegistration>(PermissionType.Delete, "sessionId")]
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
  [RequireResourcePermission<SessionRegistration>(PermissionType.Read, "sessionId")]
  public async Task<ActionResult<IEnumerable<SessionRegistration>>> GetSessionRegistrations(Guid sessionId) {
    var registrations = await testService.GetSessionRegistrationsAsync(sessionId);

    return Ok(registrations);
  }

  // POST: testing/sessions/{sessionId}/waitlist
  [HttpPost("sessions/{sessionId}/waitlist")]
  [RequireResourcePermission<SessionWaitlist>(PermissionType.Create, "sessionId")]
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
  [RequireResourcePermission<SessionWaitlist>(PermissionType.Delete, "sessionId")]
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
  [RequireResourcePermission<SessionWaitlist>(PermissionType.Read, "sessionId")]
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
  [RequireResourcePermission<TestingSession>(PermissionType.Read, "sessionId")]
  public async Task<ActionResult<object>> GetTestingSessionStatistics(Guid sessionId) {
    var statistics = await testService.GetTestingSessionStatisticsAsync(sessionId);

    return Ok(statistics);
  }

  // GET: testing/users/{userId}/activity
  [HttpGet("users/{userId}/activity")]
  [RequireResourcePermission<TestingParticipant>(PermissionType.Read, "userId")]
  public async Task<ActionResult<object>> GetUserTestingActivity(Guid userId) {
    var activity = await testService.GetUserTestingActivityAsync(userId);

    return Ok(activity);
  }

  #endregion

  #region Simplified Testing Workflow Endpoints

  // POST: testing/submit-simple
  [HttpPost("submit-simple")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Create)]
  public async Task<ActionResult<TestingRequest>> SubmitSimpleTestingRequest(CreateSimpleTestingRequestDto requestDto) {
    try {
      // Validate model state
      if (!ModelState.IsValid) return BadRequest(ModelState);

      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      var request = await testService.CreateSimpleTestingRequestAsync(requestDto, userId);

      return CreatedAtAction(nameof(GetTestingRequest), new { id = request.Id }, request);
    }
    catch (Exception ex) { return BadRequest($"Error creating testing request: {ex.Message}"); }
  }

  // POST: testing/feedback
  [HttpPost("feedback")]
  [RequireResourcePermission<TestingFeedback>(PermissionType.Create)]
  public async Task<ActionResult> SubmitFeedback(SubmitFeedbackDto feedbackDto) {
    try {
      // Validate model state
      if (!ModelState.IsValid) return BadRequest(ModelState);

      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

      await testService.SubmitFeedbackAsync(feedbackDto, userId);

      return Ok(new { message = "Feedback submitted successfully" });
    }
    catch (Exception ex) { return BadRequest($"Error submitting feedback: {ex.Message}"); }
  }

  // GET: testing/my-requests
  [HttpGet("my-requests")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> GetMyTestingRequests() {
    // Get the current authenticated user's ID
    var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

    var requests = await testService.GetTestingRequestsByCreatorAsync(userId);

    return Ok(requests);
  }

  // GET: testing/available-for-testing
  [HttpGet("available-for-testing")]
  [RequireResourcePermission<TestingRequest>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingRequest>>> GetAvailableTestingRequests() {
    var requests = await testService.GetActiveTestingRequestsAsync();

    return Ok(requests);
  }

  // GET: testing/attendance/students
  [HttpGet("attendance/students")]
  [RequireResourcePermission<SessionRegistration>(PermissionType.Read)]
  public async Task<ActionResult<object>> GetStudentAttendanceReport() {
    try {
      var report = await testService.GetStudentAttendanceReportAsync();
      return Ok(report);
    }
    catch (Exception ex) {
      return BadRequest($"Error generating student attendance report: {ex.Message}");
    }
  }

  // GET: testing/attendance/sessions
  [HttpGet("attendance/sessions")]
  [RequireResourcePermission<SessionRegistration>(PermissionType.Read)]
  public async Task<ActionResult<object>> GetSessionAttendanceReport() {
    try {
      var report = await testService.GetSessionAttendanceReportAsync();
      return Ok(report);
    }
    catch (Exception ex) {
      return BadRequest($"Error generating session attendance report: {ex.Message}");
    }
  }

  // POST: testing/sessions/{id}/attendance
  [HttpPost("sessions/{sessionId}/attendance")]
  [RequireResourcePermission<SessionRegistration>(PermissionType.Edit, "sessionId")]
  public async Task<ActionResult> UpdateAttendance(Guid sessionId, UpdateAttendanceDto attendanceDto) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId)) 
        return Unauthorized("User ID not found in token");

      await testService.UpdateSessionAttendanceAsync(sessionId, attendanceDto.UserId, attendanceDto.AttendanceStatus, currentUserId);

      return Ok(new { message = "Attendance updated successfully" });
    }
    catch (Exception ex) {
      return BadRequest($"Error updating attendance: {ex.Message}");
    }
  }

  // POST: testing/feedback/{id}/report
  [HttpPost("feedback/{feedbackId}/report")]
  [RequireResourcePermission<TestingFeedback>(PermissionType.Report, "feedbackId")]
  public async Task<ActionResult> ReportFeedback(Guid feedbackId, ReportFeedbackDto reportDto) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId)) 
        return Unauthorized("User ID not found in token");

      await testService.ReportFeedbackAsync(feedbackId, reportDto.Reason, currentUserId);

      return Ok(new { message = "Feedback reported successfully" });
    }
    catch (Exception ex) {
      return BadRequest($"Error reporting feedback: {ex.Message}");
    }
  }

  // POST: testing/feedback/{id}/quality
  [HttpPost("feedback/{feedbackId}/quality")]
  [RequireResourcePermission<TestingFeedback>(PermissionType.Edit, "feedbackId")]
  public async Task<ActionResult> RateFeedbackQuality(Guid feedbackId, RateFeedbackQualityDto qualityDto) {
    try {
      // Get the current authenticated user's ID
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId)) 
        return Unauthorized("User ID not found in token");

      await testService.RateFeedbackQualityAsync(feedbackId, qualityDto.Quality, currentUserId);

      return Ok(new { message = "Feedback quality rated successfully" });
    }
    catch (Exception ex) {
      return BadRequest($"Error rating feedback quality: {ex.Message}");
    }
  }

  #endregion

  #region Testing Location Endpoints

  // GET: testing/locations
  [HttpGet("locations")]
  [RequireResourcePermission<TestingLocation>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TestingLocation>>> GetTestingLocations(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var locations = await testService.GetTestingLocationsAsync(skip, take);
    return Ok(locations);
  }

  // GET: testing/locations/{id}
  [HttpGet("locations/{id}")]
  [RequireResourcePermission<TestingLocation>(PermissionType.Read)]
  public async Task<ActionResult<TestingLocation>> GetTestingLocation(Guid id) {
    var location = await testService.GetTestingLocationByIdAsync(id);
    if (location == null) return NotFound();
    return Ok(location);
  }

  // POST: testing/locations
  [HttpPost("locations")]
  [RequireResourcePermission<TestingLocation>(PermissionType.Create)]
  public async Task<ActionResult<TestingLocation>> CreateTestingLocation(CreateTestingLocationDto locationDto) {
    try {
      if (!ModelState.IsValid) return BadRequest(ModelState);

      var location = locationDto.ToTestingLocation();
      var createdLocation = await testService.CreateTestingLocationAsync(location);

      return CreatedAtAction(nameof(GetTestingLocation), new { id = createdLocation.Id }, createdLocation);
    }
    catch (Exception ex) {
      return BadRequest($"Error creating testing location: {ex.Message}");
    }
  }

  // PUT: testing/locations/{id}
  [HttpPut("locations/{id}")]
  [RequireResourcePermission<TestingLocation>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLocation>> UpdateTestingLocation(Guid id, UpdateTestingLocationDto locationDto) {
    try {
      var existingLocation = await testService.GetTestingLocationByIdAsync(id);
      if (existingLocation == null) return NotFound();

      locationDto.UpdateTestingLocation(existingLocation);
      var updatedLocation = await testService.UpdateTestingLocationAsync(existingLocation);

      return Ok(updatedLocation);
    }
    catch (Exception ex) {
      return BadRequest($"Error updating testing location: {ex.Message}");
    }
  }

  // DELETE: testing/locations/{id}
  [HttpDelete("locations/{id}")]
  [RequireResourcePermission<TestingLocation>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteTestingLocation(Guid id) {
    var result = await testService.DeleteTestingLocationAsync(id);
    if (!result) return NotFound();
    return NoContent();
  }

  // POST: testing/locations/{id}/restore
  [HttpPost("locations/{id}/restore")]
  [RequireResourcePermission<TestingLocation>(PermissionType.Edit)]
  public async Task<ActionResult> RestoreTestingLocation(Guid id) {
    var result = await testService.RestoreTestingLocationAsync(id);
    if (!result) return NotFound();
    return Ok(new { message = "Testing location restored successfully" });
  }

  #endregion

  #region Module Permission Integration Endpoints

  /// <summary>
  /// Check if current user can perform specific Testing Lab actions
  /// </summary>
  [HttpGet("permissions/check")]
  public async Task<ActionResult<TestingLabActionPermissions>> CheckTestingLabPermissions([FromServices] IModulePermissionService modulePermissionService, [FromQuery] Guid? tenantId = null) {
    var userId = GetCurrentUserId();
    
    var permissions = new TestingLabActionPermissions {
      CanCreateSessions = await modulePermissionService.CanCreateTestingSessionsAsync(userId, tenantId),
      CanDeleteSessions = await modulePermissionService.CanDeleteTestingSessionsAsync(userId, tenantId),
      CanManageTesters = await modulePermissionService.CanManageTestersAsync(userId, tenantId),
      CanViewReports = await modulePermissionService.CanViewTestingReportsAsync(userId, tenantId),
      CanExportData = await modulePermissionService.CanExportTestingDataAsync(userId, tenantId)
    };
    
    return Ok(permissions);
  }

  /// <summary>
  /// Get comprehensive Testing Lab permissions for current user
  /// </summary>
  [HttpGet("permissions/my-permissions")]
  public async Task<ActionResult<TestingLabPermissions>> GetMyTestingLabPermissions([FromServices] IModulePermissionService modulePermissionService, [FromQuery] Guid? tenantId = null) {
    var userId = GetCurrentUserId();
    var permissions = await modulePermissionService.GetUserTestingLabPermissionsAsync(userId, tenantId);
    return Ok(permissions);
  }

  /// <summary>
  /// Assign Testing Lab role to a user (admin only)
  /// </summary>
  [HttpPost("permissions/assign-role")]
  public async Task<ActionResult> AssignTestingLabRole([FromServices] IModulePermissionService modulePermissionService, [FromBody] AssignTestingLabRoleRequest request) {
    // TODO: Add admin permission check
    try {
      await modulePermissionService.AssignRoleAsync(
        request.UserId,
        request.TenantId,
        ModuleType.TestingLab,
        request.RoleName,
        request.Constraints,
        request.ExpiresAt);
      
      return Ok(new { message = $"Successfully assigned {request.RoleName} role to user {request.UserId}" });
    }
    catch (Exception ex) {
      return BadRequest($"Error assigning role: {ex.Message}");
    }
  }

  /// <summary>
  /// Create a new testing session with module permission checking
  /// </summary>
  [HttpPost("sessions/create-with-permissions")]
  public async Task<ActionResult<TestingSession>> CreateTestingSessionWithPermissions(
    [FromServices] IModulePermissionService modulePermissionService,
    [FromBody] CreateTestingSessionDto sessionDto,
    [FromQuery] Guid? tenantId = null) {
    
    var userId = GetCurrentUserId();
    
    // Check if user has permission to create sessions
    var canCreate = await modulePermissionService.CanCreateTestingSessionsAsync(userId, tenantId);
    if (!canCreate) {
      return Forbid("You do not have permission to create testing sessions");
    }
    
    try {
      // Convert DTO to entity and create session
      var session = sessionDto.ToTestingSession(userId);
      var createdSession = await testService.CreateTestingSessionAsync(session);
      
      return CreatedAtAction(nameof(GetTestingSession), new { id = createdSession.Id }, createdSession);
    }
    catch (Exception ex) {
      return BadRequest($"Error creating testing session: {ex.Message}");
    }
  }

  /// <summary>
  /// Delete a testing session with module permission checking
  /// </summary>
  [HttpDelete("sessions/{id}/delete-with-permissions")]
  public async Task<ActionResult> DeleteTestingSessionWithPermissions(
    [FromServices] IModulePermissionService modulePermissionService,
    Guid id,
    [FromQuery] Guid? tenantId = null) {
    
    var userId = GetCurrentUserId();
    
    // Check if user has permission to delete sessions
    var canDelete = await modulePermissionService.CanDeleteTestingSessionsAsync(userId, tenantId);
    if (!canDelete) {
      return Forbid("You do not have permission to delete testing sessions");
    }
    
    try {
      var result = await testService.DeleteTestingSessionAsync(id);
      if (!result) return NotFound();
      
      return NoContent();
    }
    catch (Exception ex) {
      return BadRequest($"Error deleting testing session: {ex.Message}");
    }
  }

  /// <summary>
  /// Get users with specific Testing Lab roles
  /// </summary>
  [HttpGet("permissions/users-with-role/{roleName}")]
  public async Task<ActionResult<List<UserRoleAssignment>>> GetUsersWithTestingLabRole(
    [FromServices] IModulePermissionService modulePermissionService,
    string roleName,
    [FromQuery] Guid? tenantId = null) {
    
    // TODO: Add admin permission check
    var users = await modulePermissionService.GetUsersWithRoleAsync(tenantId, ModuleType.TestingLab, roleName);
    return Ok(users);
  }

  // Helper method to get current user ID
  private Guid GetCurrentUserId() {
    var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      throw new UnauthorizedAccessException("User ID not found in token");
    }
    return userId;
  }

  #endregion
}

// DTOs for request bodies
public class UpdateAttendanceDto {
  public Guid UserId { get; set; }
  public AttendanceStatus AttendanceStatus { get; set; }
}

public class ReportFeedbackDto {
  public string Reason { get; set; } = string.Empty;
}

public class RateFeedbackQualityDto {
  public FeedbackQuality Quality { get; set; }
}

public class CreateTestingLocationDto {
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public string? Address { get; set; }
  public int MaxTestersCapacity { get; set; }
  public int MaxProjectsCapacity { get; set; }
  public string? EquipmentAvailable { get; set; }
  public LocationStatus Status { get; set; } = LocationStatus.Active;

  public TestingLocation ToTestingLocation() {
    return new TestingLocation {
      Name = Name,
      Description = Description,
      Address = Address,
      MaxTestersCapacity = MaxTestersCapacity,
      MaxProjectsCapacity = MaxProjectsCapacity,
      EquipmentAvailable = EquipmentAvailable,
      Status = Status
    };
  }
}

public class UpdateTestingLocationDto {
  public string? Name { get; set; }
  public string? Description { get; set; }
  public string? Address { get; set; }
  public int? MaxTestersCapacity { get; set; }
  public int? MaxProjectsCapacity { get; set; }
  public string? EquipmentAvailable { get; set; }
  public LocationStatus? Status { get; set; }

  public void UpdateTestingLocation(TestingLocation location) {
    if (!string.IsNullOrEmpty(Name)) location.Name = Name;
    if (Description != null) location.Description = Description;
    if (Address != null) location.Address = Address;
    if (MaxTestersCapacity.HasValue) location.MaxTestersCapacity = MaxTestersCapacity.Value;
    if (MaxProjectsCapacity.HasValue) location.MaxProjectsCapacity = MaxProjectsCapacity.Value;
    if (EquipmentAvailable != null) location.EquipmentAvailable = EquipmentAvailable;
    if (Status.HasValue) location.Status = Status.Value;
  }
}

// Module Permission DTOs
public class TestingLabActionPermissions {
  public bool CanCreateSessions { get; set; }
  public bool CanDeleteSessions { get; set; }
  public bool CanManageTesters { get; set; }
  public bool CanViewReports { get; set; }
  public bool CanExportData { get; set; }
}

public class AssignTestingLabRoleRequest {
  public Guid UserId { get; set; }
  public Guid? TenantId { get; set; }
  public string RoleName { get; set; } = string.Empty; // "TestingLabAdmin", "TestingLabManager", etc.
  public List<PermissionConstraint>? Constraints { get; set; }
  public DateTime? ExpiresAt { get; set; }
}
