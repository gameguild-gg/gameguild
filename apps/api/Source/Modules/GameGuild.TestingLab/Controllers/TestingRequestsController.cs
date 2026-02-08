using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.TestingLab;

/// <summary>
/// Controller for testing request CRUD operations, queries, and search.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing")]
[Authorize]
public class TestingRequestsController(
    ITestingRequestOperations requestService,
    IActorContextAccessor actorContextAccessor,
    ILogger<TestingRequestsController> _logger) : BaseApiController
{
    // GET: testing/requests
    [HttpGet("requests")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequests([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var requests = await requestService.GetTestingRequestsAsync(skip, take);
        return Ok(requests);
    }

    // GET: testing/requests/{id}
    [HttpGet("requests/{id}")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<TestingRequest>> GetTestingRequest(Guid id)
    {
        var request = await requestService.GetTestingRequestByIdAsync(id);
        if (request == null) return NotFound();
        return Ok(request);
    }

    // GET: testing/requests/{id}/details
    [HttpGet("requests/{id}/details")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<TestingRequest>> GetTestingRequestWithDetails(Guid id)
    {
        var request = await requestService.GetTestingRequestByIdWithDetailsAsync(id);
        if (request == null) return NotFound();
        return Ok(request);
    }

    // POST: testing/requests
    [HttpPost("requests")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Create)]
    public async Task<ActionResult<TestingRequest>> CreateTestingRequest(CreateTestingRequestDto requestDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var request = requestDto.ToTestingRequest(userId.Value);
        var createdRequest = await requestService.CreateTestingRequestAsync(request);

        return CreatedAtAction(nameof(GetTestingRequest), new { id = createdRequest.Id }, createdRequest);
    }

    // PUT: testing/requests/{id}
    [HttpPut("requests/{id}")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Edit)]
    public async Task<ActionResult<TestingRequest>> UpdateTestingRequest(Guid id, TestingRequest request)
    {
        if (id != request.Id) return BadRequest("ID mismatch");

        try
        {
            var updatedRequest = await requestService.UpdateTestingRequestAsync(request);
            return Ok(updatedRequest);
        }
        catch (InvalidOperationException ex) { return NotFound(ex.Message); }
    }

    // DELETE: testing/requests/{id}
    [HttpDelete("requests/{id}")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Delete)]
    public async Task<ActionResult> DeleteTestingRequest(Guid id)
    {
        var result = await requestService.DeleteTestingRequestAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    // POST: testing/requests/{id}:restore
    [HttpPost("requests/{id}:restore")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Edit)]
    public async Task<ActionResult> RestoreTestingRequest(Guid id)
    {
        var result = await requestService.RestoreTestingRequestAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    // GET: testing/requests/by-project-version/{projectVersionId}
    [HttpGet("requests/by-project-version/{projectVersionId}")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequestsByProjectVersion(Guid projectVersionId)
    {
        var requests = await requestService.GetTestingRequestsByProjectVersionAsync(projectVersionId);
        return Ok(requests);
    }

    // GET: testing/requests/by-creator/{creatorId}
    [HttpGet("requests/by-creator/{creatorId}")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequestsByCreator(Guid creatorId)
    {
        var requests = await requestService.GetTestingRequestsByCreatorAsync(creatorId);
        return Ok(requests);
    }

    // GET: testing/requests/by-status/{status}
    [HttpGet("requests/by-status/{status}")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> GetTestingRequestsByStatus(TestingRequestStatus status)
    {
        var requests = await requestService.GetTestingRequestsByStatusAsync(status);
        return Ok(requests);
    }

    // GET: testing/requests/search
    [HttpGet("requests/search")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> SearchTestingRequests([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return BadRequest("Search term is required");

        var requests = await requestService.SearchTestingRequestsAsync(searchTerm);
        return Ok(requests);
    }

    // POST: testing/submit-simple
    [HttpPost("submit-simple")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Create)]
    public async Task<ActionResult<TestingRequest>> SubmitSimpleTestingRequest(CreateSimpleTestingRequestDto requestDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var request = await requestService.CreateSimpleTestingRequestAsync(requestDto, userId.Value);

        return CreatedAtAction(nameof(GetTestingRequest), new { id = request.Id }, request);
    }

    // GET: testing/my-requests
    [HttpGet("my-requests")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> GetMyTestingRequests()
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var requests = await requestService.GetTestingRequestsByCreatorAsync(userId.Value);
        return Ok(requests);
    }

    // GET: testing/available-for-testing
    [HttpGet("available-for-testing")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingRequest>>> GetAvailableTestingRequests()
    {
        var requests = await requestService.GetActiveTestingRequestsAsync();
        return Ok(requests);
    }

    // GET: testing/requests/{requestId}/statistics
    [HttpGet("requests/{requestId}/statistics")]
    [RequireResourcePermission<TestingRequestPermission, TestingRequest>(PermissionType.Read)]
    public async Task<ActionResult<object>> GetTestingRequestStatistics(Guid requestId, [FromServices] ITestingFeedbackOperations feedbackService)
    {
        var statistics = await feedbackService.GetTestingRequestStatisticsAsync(requestId);
        return Ok(statistics);
    }
}
