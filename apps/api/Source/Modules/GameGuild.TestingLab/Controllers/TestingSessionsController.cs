using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.TestingLab;

/// <summary>
/// Controller for testing session CRUD operations, queries, search, and attendance.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing")]
[Authorize]
public class TestingSessionsController(
    ITestingSessionOperations sessionService,
    IActorContextAccessor actorContextAccessor,
    ILogger<TestingSessionsController> _logger) : BaseApiController
{
    // GET: testing/sessions
    [HttpGet("sessions")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessions([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var sessions = await sessionService.GetTestingSessionsAsync(skip, take);
        return Ok(sessions);
    }

    // GET: testing/sessions/{id}
    [HttpGet("sessions/{id}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<TestingSession>> GetTestingSession(Guid id)
    {
        var session = await sessionService.GetTestingSessionByIdAsync(id);
        if (session == null) return NotFound();
        return Ok(session);
    }

    // GET: testing/sessions/{id}/details
    [HttpGet("sessions/{id}/details")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<TestingSession>> GetTestingSessionWithDetails(Guid id)
    {
        var session = await sessionService.GetTestingSessionByIdWithDetailsAsync(id);
        if (session == null) return NotFound();
        return Ok(session);
    }

    // POST: testing/sessions
    [HttpPost("sessions")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Create)]
    public async Task<ActionResult<TestingSession>> CreateTestingSession(TestingSession session)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        session.CreatedById = userId.Value;
        var createdSession = await sessionService.CreateTestingSessionAsync(session);

        return CreatedAtAction(nameof(GetTestingSession), new { id = createdSession.Id }, createdSession);
    }

    // PUT: testing/sessions/{id}
    [HttpPut("sessions/{id}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Edit)]
    public async Task<ActionResult<TestingSession>> UpdateTestingSession(Guid id, TestingSession session)
    {
        if (id != session.Id) return BadRequest("ID mismatch");

        try
        {
            var updatedSession = await sessionService.UpdateTestingSessionAsync(session);
            return Ok(updatedSession);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Testing session {SessionId} not found or could not be updated", id);
            return NotFound("The requested testing session was not found or could not be updated.");
        }
    }

    // DELETE: testing/sessions/{id}
    [HttpDelete("sessions/{id}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Delete)]
    public async Task<ActionResult> DeleteTestingSession(Guid id)
    {
        var result = await sessionService.DeleteTestingSessionAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    // POST: testing/sessions/{id}:restore
    [HttpPost("sessions/{id}:restore")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Edit)]
    public async Task<ActionResult> RestoreTestingSession(Guid id)
    {
        var result = await sessionService.RestoreTestingSessionAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    // GET: testing/public/sessions
    /// <summary>
    /// Public endpoint returning "published" testing sessions (Scheduled or Active). No authentication required.
    /// </summary>
    [HttpGet("public/sessions")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TestingSession>>> GetPublicTestingSessions([FromQuery] int take = 100)
    {
        var sessions = await sessionService.GetPublicTestingSessionsAsync(take);
        return Ok(sessions);
    }

    // GET: testing/sessions/by-request/{testingRequestId}
    [HttpGet("sessions/by-request/{testingRequestId}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByRequest(Guid testingRequestId)
    {
        var sessions = await sessionService.GetTestingSessionsByRequestAsync(testingRequestId);
        return Ok(sessions);
    }

    // GET: testing/sessions/by-location/{locationId}
    [HttpGet("sessions/by-location/{locationId}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByLocation(Guid locationId)
    {
        var sessions = await sessionService.GetTestingSessionsByLocationAsync(locationId);
        return Ok(sessions);
    }

    // GET: testing/sessions/by-status/{status}
    [HttpGet("sessions/by-status/{status}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByStatus(SessionStatus status)
    {
        var sessions = await sessionService.GetTestingSessionsByStatusAsync(status);
        return Ok(sessions);
    }

    // GET: testing/sessions/by-manager/{managerId}
    [HttpGet("sessions/by-manager/{managerId}")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingSession>>> GetTestingSessionsByManager(Guid managerId)
    {
        var sessions = await sessionService.GetTestingSessionsByManagerAsync(managerId);
        return Ok(sessions);
    }

    // GET: testing/sessions/search
    [HttpGet("sessions/search")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingSession>>> SearchTestingSessions([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return BadRequest("Search term is required");

        var sessions = await sessionService.SearchTestingSessionsAsync(searchTerm);
        return Ok(sessions);
    }

    // GET: testing/sessions/{sessionId}/statistics
    [HttpGet("sessions/{sessionId}/statistics")]
    [RequireResourcePermission<TestingSessionPermission, TestingSession>(PermissionType.Read, "sessionId")]
    public async Task<ActionResult<object>> GetTestingSessionStatistics(Guid sessionId)
    {
        var statistics = await sessionService.GetTestingSessionStatisticsAsync(sessionId);
        return Ok(statistics);
    }

    // GET: testing/attendance/sessions
    [HttpGet("attendance/sessions")]
    [RequireResourcePermission<SessionRegistrationPermission, SessionRegistration>(PermissionType.Read)]
    public async Task<ActionResult<object>> GetSessionAttendanceReport()
    {
        var report = await sessionService.GetSessionAttendanceReportAsync();
        return Ok(report);
    }

    // POST: testing/sessions/{id}/attendance
    [HttpPost("sessions/{sessionId}/attendance")]
    [RequireResourcePermission<SessionRegistrationPermission, SessionRegistration>(PermissionType.Edit, "sessionId")]
    public async Task<ActionResult> UpdateAttendance(Guid sessionId, UpdateAttendanceDto attendanceDto)
    {
        var currentUserId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (currentUserId == null)
            return Unauthorized("User ID not found in token");

        await sessionService.UpdateSessionAttendanceAsync(sessionId, attendanceDto.UserId, attendanceDto.AttendanceStatus, currentUserId.Value);

        return Ok(new { message = "Attendance updated successfully" });
    }
}
