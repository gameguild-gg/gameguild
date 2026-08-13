using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.TestingLab;

/// <summary>
/// Controller for testing location CRUD operations.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing")]
[Authorize]
public class TestingLocationsController(
    ITestingLocationOperations locationService) : BaseApiController
{
    // GET: testing/locations
    [HttpGet("locations")]
    [RequireTestingLabPermission(TestingLabActions.Read, TestingLabResourceTypes.Location)]
    public async Task<ActionResult<IEnumerable<TestingLocation>>> GetTestingLocations([FromQuery] int skip = 0, [FromQuery] int take = 50, [FromQuery] bool includeArchived = false)
    {
        var locations = await locationService.GetTestingLocationsAsync(skip, take, includeArchived).ConfigureAwait(false);
        return Ok(locations);
    }

    // GET: testing/locations/{id}
    [HttpGet("locations/{id}")]
    [RequireTestingLabPermission(TestingLabActions.Read, TestingLabResourceTypes.Location, "id")]
    public async Task<ActionResult<TestingLocation>> GetTestingLocation(Guid id)
    {
        var location = await locationService.GetTestingLocationByIdAsync(id).ConfigureAwait(false);
        if (location == null) return NotFound();
        return Ok(location);
    }

    // POST: testing/locations
    [HttpPost("locations")]
    [RequireTestingLabPermission(TestingLabActions.Create, TestingLabResourceTypes.Location)]
    public async Task<ActionResult<TestingLocation>> CreateTestingLocation(CreateTestingLocationDto locationDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var location = locationDto.ToTestingLocation();
        var createdLocation = await locationService.CreateTestingLocationAsync(location).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetTestingLocation), new { id = createdLocation.Id }, createdLocation);
    }

    // PUT: testing/locations/{id}
    [HttpPut("locations/{id}")]
    [RequireTestingLabPermission(TestingLabActions.Edit, TestingLabResourceTypes.Location, "id")]
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
    [RequireTestingLabPermission(TestingLabActions.Delete, TestingLabResourceTypes.Location, "id")]
    public async Task<ActionResult> DeleteTestingLocation(Guid id)
    {
        try
        {
            var result = await locationService.DeleteTestingLocationAsync(id).ConfigureAwait(false);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    // POST: testing/locations/{id}/restore
    [HttpPost("locations/{id}/restore")]
    [RequireTestingLabPermission(TestingLabActions.Edit, TestingLabResourceTypes.Location, "id")]
    public async Task<ActionResult> RestoreTestingLocation(Guid id)
    {
        var result = await locationService.RestoreTestingLocationAsync(id).ConfigureAwait(false);
        if (!result) return NotFound();
        return Ok(new { message = "Testing location restored successfully" });
    }
}
