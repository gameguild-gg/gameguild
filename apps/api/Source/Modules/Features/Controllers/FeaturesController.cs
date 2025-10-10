using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using GameGuild.Modules.Features.Models;
using GameGuild.Modules.Features.Commands;
using GameGuild.Modules.Features.Queries;

namespace GameGuild.Modules.Features.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class FeaturesController : ControllerBase
{
    private readonly ISender _sender;

    public FeaturesController(ISender sender) { _sender = sender; }

    // GET /features/enabled
    [HttpGet("enabled")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnabled(CancellationToken ct)
    {
        var result = await _sender.Send(new GetAllFeatureFlagsQuery { IsEnabled = true }, ct);
        return Ok(result);
    }

    // GET /features/{key}
    [HttpGet("{key}", Name = "GetFeatureByKey")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var feature = await _sender.Send(new GetFeatureFlagByKeyQuery { Key = key }, ct);
        return feature is null ? NotFound() : Ok(feature);
    }

    // GET /features/{key}/exists
    [HttpGet("{key}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckExists(string key, [FromQuery] string? environment, CancellationToken ct)
    {
        var exists = await _sender.Send(new FeatureFlagExistsQuery { Key = key, Environment = environment }, ct);
        return Ok(exists);
    }

    // POST /features
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFeatureRequest body, CancellationToken ct)
    {
        var id = await _sender.Send(new CreateFeatureFlagCommand(body.Key, body.Name, body.Description, body.IsEnabled, body.TenantId), ct);

        // After create, return the feature by key
        return CreatedAtRoute(
            "GetFeatureByKey",
            new { key = body.Key },
            new { id, body.Key, body.Name, body.IsEnabled }
        );
    }

    // POST /features/{id}/enable
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
    {
        await _sender.Send(new EnableFeatureFlagCommand(id), ct);
        return NoContent();
    }

    // POST /features/{id}/disable
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DisableFeatureFlagCommand(id), ct);
        return NoContent();
    }

    // POST /features/{id}/toggle
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(Guid id, [FromBody] ToggleFeatureRequest body, CancellationToken ct)
    {
        await _sender.Send(new ToggleFeatureFlagCommand(id, body.IsEnabled), ct);
        return NoContent();
    }
}

