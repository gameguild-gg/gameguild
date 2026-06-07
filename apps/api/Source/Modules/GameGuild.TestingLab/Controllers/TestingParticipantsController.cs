using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.TestingLab;

/// <summary>
/// Controller for participant management, session registration, and waitlist operations.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing")]
[Authorize]
public class TestingParticipantsController(
    ITestingParticipantOperations participantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<TestingParticipantsController> _logger) : BaseApiController
{
    #region Participant Management

    // POST: testing/requests/{requestId}/participants/{userId}
    [HttpPost("requests/{requestId}/participants/{userId}")]
    [RequireResourcePermission<PermissionType, TestingParticipant>(PermissionType.Create)]
    public async Task<ActionResult<TestingParticipant>> AddParticipant(Guid requestId, Guid userId)
    {
        var participant = await participantService.AddParticipantAsync(requestId, userId).ConfigureAwait(false);
        return Ok(participant);
    }

    // DELETE: testing/requests/{requestId}/participants/{userId}
    [HttpDelete("requests/{requestId}/participants/{userId}")]
    [RequireResourcePermission<PermissionType, TestingParticipant>(PermissionType.Delete)]
    public async Task<ActionResult> RemoveParticipant(Guid requestId, Guid userId)
    {
        var result = await participantService.RemoveParticipantAsync(requestId, userId).ConfigureAwait(false);
        if (!result) return NotFound();
        return NoContent();
    }

    // GET: testing/requests/{requestId}/participants
    [HttpGet("requests/{requestId}/participants")]
    [RequireResourcePermission<PermissionType, TestingParticipant>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingParticipant>>> GetTestingRequestParticipants(Guid requestId)
    {
        var participants = await participantService.GetTestingRequestParticipantsAsync(requestId).ConfigureAwait(false);
        return Ok(participants);
    }

    // GET: testing/requests/{requestId}/participants/{userId}/check
    [HttpGet("requests/{requestId}/participants/{userId}/check")]
    [RequireResourcePermission<PermissionType, TestingParticipant>(PermissionType.Read)]
    public async Task<ActionResult<bool>> CheckUserParticipation(Guid requestId, Guid userId)
    {
        var isParticipant = await participantService.IsUserParticipantAsync(requestId, userId).ConfigureAwait(false);
        return Ok(isParticipant);
    }

    #endregion

    #region Session Registration

    // POST: testing/sessions/{sessionId}/register
    [HttpPost("sessions/{sessionId}/register")]
    [RequireResourcePermission<PermissionType, SessionRegistration>(PermissionType.Create, "sessionId")]
    public async Task<ActionResult<SessionRegistration>> RegisterForSession(Guid sessionId, [FromBody] SessionRegistrationRequest request)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var registration = await participantService.RegisterForSessionAsync(sessionId, userId.Value, request.RegistrationType, request.Notes).ConfigureAwait(false);
        return Ok(registration);
    }

    // DELETE: testing/sessions/{sessionId}/register
    [HttpDelete("sessions/{sessionId}/register")]
    [RequireResourcePermission<PermissionType, SessionRegistration>(PermissionType.Delete, "sessionId")]
    public async Task<ActionResult> UnregisterFromSession(Guid sessionId)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var result = await participantService.UnregisterFromSessionAsync(sessionId, userId.Value).ConfigureAwait(false);
        if (!result) return NotFound();
        return NoContent();
    }

    // GET: testing/sessions/{sessionId}/registrations
    [HttpGet("sessions/{sessionId}/registrations")]
    [RequireResourcePermission<PermissionType, SessionRegistration>(PermissionType.Read, "sessionId")]
    public async Task<ActionResult<IEnumerable<SessionRegistration>>> GetSessionRegistrations(Guid sessionId)
    {
        var registrations = await participantService.GetSessionRegistrationsAsync(sessionId).ConfigureAwait(false);
        return Ok(registrations);
    }

    #endregion

    #region Waitlist

    // POST: testing/sessions/{sessionId}/waitlist
    [HttpPost("sessions/{sessionId}/waitlist")]
    [RequireResourcePermission<PermissionType, SessionWaitlist>(PermissionType.Create, "sessionId")]
    public async Task<ActionResult<SessionWaitlist>> AddToWaitlist(Guid sessionId, [FromBody] SessionRegistrationRequest request)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var waitlistEntry = await participantService.AddToWaitlistAsync(sessionId, userId.Value, request.RegistrationType, request.Notes).ConfigureAwait(false);
        return Ok(waitlistEntry);
    }

    // DELETE: testing/sessions/{sessionId}/waitlist
    [HttpDelete("sessions/{sessionId}/waitlist")]
    [RequireResourcePermission<PermissionType, SessionWaitlist>(PermissionType.Delete, "sessionId")]
    public async Task<ActionResult> RemoveFromWaitlist(Guid sessionId)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var result = await participantService.RemoveFromWaitlistAsync(sessionId, userId.Value).ConfigureAwait(false);
        if (!result) return NotFound();
        return NoContent();
    }

    // GET: testing/sessions/{sessionId}/waitlist
    [HttpGet("sessions/{sessionId}/waitlist")]
    [RequireResourcePermission<PermissionType, SessionWaitlist>(PermissionType.Read, "sessionId")]
    public async Task<ActionResult<IEnumerable<SessionWaitlist>>> GetSessionWaitlist(Guid sessionId)
    {
        var waitlist = await participantService.GetSessionWaitlistAsync(sessionId).ConfigureAwait(false);
        return Ok(waitlist);
    }

    #endregion

    #region User Activity & Attendance

    // GET: testing/users/{userId}/activity
    [HttpGet("users/{userId}/activity")]
    [RequireResourcePermission<PermissionType, TestingParticipant>(PermissionType.Read, "userId")]
    public async Task<ActionResult<object>> GetUserTestingActivity(Guid userId)
    {
        var activity = await participantService.GetUserTestingActivityAsync(userId).ConfigureAwait(false);
        return Ok(activity);
    }

    // GET: testing/attendance/students
    [HttpGet("attendance/students")]
    [RequireResourcePermission<PermissionType, SessionRegistration>(PermissionType.Read)]
    public async Task<ActionResult<object>> GetStudentAttendanceReport()
    {
        var report = await participantService.GetStudentAttendanceReportAsync().ConfigureAwait(false);
        return Ok(report);
    }

    #endregion
}
