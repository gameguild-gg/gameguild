using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.TestingLab;

/// <summary>
/// Controller for testing location CRUD operations.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing")]
[Authorize]
public class TestingLocationsController(
    ITestingLocationOperations locationService,
    ILogger<TestingLocationsController> _logger) : BaseApiController
{
    // GET: testing/locations
    [HttpGet("locations")]
    [RequireResourcePermission<PermissionType, TestingLocation>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingLocation>>> GetTestingLocations([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var locations = await locationService.GetTestingLocationsAsync(skip, take).ConfigureAwait(false);
        return Ok(locations);
    }

    // GET: testing/locations/{id}
    [HttpGet("locations/{id}")]
    [RequireResourcePermission<PermissionType, TestingLocation>(PermissionType.Read)]
    public async Task<ActionResult<TestingLocation>> GetTestingLocation(Guid id)
    {
        var location = await locationService.GetTestingLocationByIdAsync(id).ConfigureAwait(false);
        if (location == null) return NotFound();
        return Ok(location);
    }

    // POST: testing/locations
    [HttpPost("locations")]
    [RequireResourcePermission<PermissionType, TestingLocation>(PermissionType.Create)]
    public async Task<ActionResult<TestingLocation>> CreateTestingLocation(CreateTestingLocationDto locationDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var location = locationDto.ToTestingLocation();
        var createdLocation = await locationService.CreateTestingLocationAsync(location).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetTestingLocation), new { id = createdLocation.Id }, createdLocation);
    }

    // PUT: testing/locations/{id}
    [HttpPut("locations/{id}")]
    [RequireResourcePermission<PermissionType, TestingLocation>(PermissionType.Edit)]
    public async Task<ActionResult<TestingLocation>> UpdateTestingLocation(Guid id, UpdateTestingLocationDto locationDto)
    {
        var existingLocation = await locationService.GetTestingLocationByIdAsync(id).ConfigureAwait(false);
        if (existingLocation == null) return NotFound();

        locationDto.UpdateTestingLocation(existingLocation);
        var updatedLocation = await locationService.UpdateTestingLocationAsync(existingLocation).ConfigureAwait(false);

        return Ok(updatedLocation);
    }

    // DELETE: testing/locations/{id}
    [HttpDelete("locations/{id}")]
    [RequireResourcePermission<PermissionType, TestingLocation>(PermissionType.Delete)]
    public async Task<ActionResult> DeleteTestingLocation(Guid id)
    {
        var result = await locationService.DeleteTestingLocationAsync(id).ConfigureAwait(false);
        if (!result) return NotFound();
        return NoContent();
    }

    // POST: testing/locations/{id}/restore
    [HttpPost("locations/{id}/restore")]
    [RequireResourcePermission<PermissionType, TestingLocation>(PermissionType.Edit)]
    public async Task<ActionResult> RestoreTestingLocation(Guid id)
    {
        var result = await locationService.RestoreTestingLocationAsync(id).ConfigureAwait(false);
        if (!result) return NotFound();
        return Ok(new { message = "Testing location restored successfully" });
    }
}
