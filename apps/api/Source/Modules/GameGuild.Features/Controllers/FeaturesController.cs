using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;









using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Features;

/// <summary>
///     Management surface for feature flags. Feature flags gate product behavior, so no
///     endpoint here is public: reads require the <c>Features.Read</c> policy and every
///     mutation requires <c>Features.Manage</c>. Evaluation of flags for the caller happens
///     through the evaluation/SDK surfaces, not through this controller.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/features")]
[Authorize]
public sealed class FeaturesController(ISender sender) : BaseApiController
{
    // GET /api/v1/features - Get all feature flags with optional filtering
    [HttpGet]
    [Authorize(Policy = Policies.FeaturesRead)]
    [ProducesResponseType(typeof(IEnumerable<FeatureFlagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isEnabled, CancellationToken ct)
    {
        var query = new GetAllFeatureFlagsQuery { IsEnabled = isEnabled };
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    // GET /features/{key}
    [HttpGet("{key}", Name = "GetFeatureByKey")]
    [Authorize(Policy = Policies.FeaturesRead)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var feature = await sender.Send(new GetFeatureFlagByKeyQuery { Key = key }, ct).ConfigureAwait(false);

        return feature is null ? NotFound() : Ok(feature);
    }

    // GET /features/{key}/exists
    [HttpGet("{key}/exists")]
    [Authorize(Policy = Policies.FeaturesRead)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckExists(string key, [FromQuery] string? environment, CancellationToken ct)
    {
        var exists = await sender.Send(new FeatureFlagExistsQuery { Key = key, Environment = environment }, ct).ConfigureAwait(false);

        return Ok(exists);
    }

    // POST /features
    [HttpPost]
    [Authorize(Policy = Policies.FeaturesManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFeatureRequest body, CancellationToken ct)
    {
        var id = await sender.Send(new CreateFeatureFlagCommand(body.Key, body.Name, body.Description, body.IsEnabled, body.TenantId), ct).ConfigureAwait(false);

        // After create, return the feature by key
        return CreatedAtRoute("GetFeatureByKey", new { key = body.Key }, new { id, body.Key, body.Name, body.IsEnabled });
    }

    // PUT /features/{key}
    [HttpPut("{key}")]
    [Authorize(Policy = Policies.FeaturesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateFeatureRequest body, CancellationToken ct)
    {
        var existing = await sender.Send(new GetFeatureFlagByKeyQuery { Key = key }, ct).ConfigureAwait(false);

        if (existing is null) return NotFound();

        await sender.Send(new UpdateFeatureFlagCommand(existing.Id, body.Name, body.Description, body.IsEnabled, body.RolloutPercentage, body.EnabledValue, body.DefaultValue), ct).ConfigureAwait(false);

        return NoContent();
    }

    // DELETE /features/{key}
    [HttpDelete("{key}")]
    [Authorize(Policy = Policies.FeaturesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string key, CancellationToken ct)
    {
        var existing = await sender.Send(new GetFeatureFlagByKeyQuery { Key = key }, ct).ConfigureAwait(false);

        if (existing is null) return NotFound();

        await sender.Send(new DeleteFeatureFlagCommand(existing.Id), ct).ConfigureAwait(false);

        return NoContent();
    }

    // POST /features/{id}:enable
    [HttpPost("{id:guid}:enable")]
    [Authorize(Policy = Policies.FeaturesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
    {
        await sender.Send(new EnableFeatureFlagCommand(id), ct).ConfigureAwait(false);

        return NoContent();
    }

    // POST /features/{id}:disable
    [HttpPost("{id:guid}:disable")]
    [Authorize(Policy = Policies.FeaturesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        await sender.Send(new DisableFeatureFlagCommand(id), ct).ConfigureAwait(false);

        return NoContent();
    }

    // POST /features/{id}:toggle
    [HttpPost("{id:guid}:toggle")]
    [Authorize(Policy = Policies.FeaturesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(Guid id, [FromBody] ToggleFeatureRequest body, CancellationToken ct)
    {
        await sender.Send(new ToggleFeatureFlagCommand(id, body.IsEnabled), ct).ConfigureAwait(false);

        return NoContent();
    }
}
