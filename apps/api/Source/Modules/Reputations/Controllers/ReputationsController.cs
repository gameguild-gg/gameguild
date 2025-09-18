using GameGuild.Authorization.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Reputations;

[ApiController]
[Route("api/[controller]")]
public class ReputationsController : ControllerBase {
    private readonly IReputationService _reputationService;
    private readonly ILogger<ReputationsController> _logger;

    public ReputationsController(
        IReputationService reputationService,
        ILogger<ReputationsController> logger) {
        _reputationService = reputationService;
        _logger = logger;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IReputation>> GetUserReputation(Guid userId, [FromQuery] Guid? tenantId = null) {
        try {
            var reputation = await _reputationService.GetUserReputationAsync(userId, tenantId);
            if (reputation == null) {
                return NotFound();
            }
            return Ok(reputation);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving user reputation for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("user/{userId}/update")]
    public async Task<ActionResult<IReputation>> UpdateUserReputation(
        Guid userId,
        [FromBody] int scoreChange,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? reason = null) {
        try {
            var reputation = await _reputationService.UpdateReputationAsync(userId, scoreChange, tenantId, reason);
            return Ok(reputation);
        }
        catch (ArgumentException ex) {
            return BadRequest(ex.Message);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error updating user reputation for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("tier/{tier}")]
    public async Task<ActionResult<IEnumerable<IReputation>>> GetUsersByReputationTier(
        ReputationTier tier,
        [FromQuery] Guid? tenantId = null) {
        try {
            var users = await _reputationService.GetUsersByReputationTierAsync(tier, tenantId);
            return Ok(users);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving users by reputation tier {Tier}", tier);
            return StatusCode(500, "Internal server error");
        }
    }
}