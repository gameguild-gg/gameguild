using GameGuild.Common;
using GameGuild.Modules.Auth;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.DTOs;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Services;
using Microsoft.AspNetCore.Mvc;
using ProgramEntity = GameGuild.Modules.Programs.Models.Program;


namespace GameGuild.Modules.Programs.Controllers;

/// <summary>
/// REST API controller for managing programs
/// Implements 3-layer DAC permission system for all routes
/// 
/// DAC Attribute Usage Examples:
/// - Tenant Level: [RequireTenantPermission(PermissionType.Create)]
/// - Content-Type Level: [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
/// - Resource Level (Preferred): [RequireResourcePermission<ProgramEntity>(PermissionType.Update)]
/// - Resource Level (Explicit): [RequireResourcePermission<ProgramPermission, ProgramEntity>(PermissionType.Update)]
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProgramController(IProgramService programService) : ControllerBase {
  // ===== CONTENT-TYPE LEVEL OPERATIONS =====

  /// <summary>
  /// Get all programs (content-type level read permission)
  /// </summary>
  [HttpGet]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetPrograms(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var programs = await programService.GetProgramsAsync(skip, take);

    return Ok(programs);
  }

  /// <summary>
  /// Get published programs (no permission required - public access)
  /// </summary>
  [HttpGet("published")]
  [Public]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetPublishedPrograms(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var programs = await programService.GetPublishedProgramsAsync(skip, take);

    return Ok(programs);
  }

  /// <summary>
  /// Get programs by category (content-type level read permission)
  /// </summary>
  [HttpGet("category/{category}")]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetProgramsByCategory(
    ProgramCategory category,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var programs = await programService.GetProgramsByCategoryAsync(category, skip, take);

    return Ok(programs);
  }

  /// <summary>
  /// Get programs by difficulty level (content-type level read permission)
  /// </summary>
  [HttpGet("difficulty/{difficulty}")]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetProgramsByDifficulty(
    ProgramDifficulty difficulty,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var programs = await programService.GetProgramsByDifficultyAsync(difficulty, skip, take);

    return Ok(programs);
  }

  /// <summary>
  /// Search programs (content-type level read permission)
  /// </summary>
  [HttpGet("search")]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> SearchPrograms(
    [FromQuery] string searchTerm,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var programs = await programService.SearchProgramsAsync(searchTerm, skip, take);

    return Ok(programs);
  }

  /// <summary>
  /// Get programs by creator (content-type level read permission)
  /// </summary>
  [HttpGet("creator/{creatorId}")]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetProgramsByCreator(
    Guid creatorId,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var programs = await programService.GetProgramsByCreatorAsync(creatorId, skip, take);

    return Ok(programs);
  }

  /// <summary>
  /// Get popular programs (content-type level read permission)
  /// </summary>
  [HttpGet("popular")]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetPopularPrograms([FromQuery] int count = 10) {
    var programs = await programService.GetPopularProgramsAsync(count);

    return Ok(programs);
  }

  /// <summary>
  /// Get recent programs (content-type level read permission)
  /// </summary>
  [HttpGet("recent")]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramEntity>>> GetRecentPrograms([FromQuery] int count = 10) {
    var programs = await programService.GetRecentProgramsAsync(count);

    return Ok(programs);
  }

  /// <summary>
  /// Create a new program (content-type level draft permission)
  /// </summary>
  [HttpPost]
  [RequireContentTypePermission<ProgramEntity>(PermissionType.Draft)]
  public async Task<ActionResult<ProgramEntity>> CreateProgram([FromBody] CreateProgramDto createDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.CreateProgramAsync(createDto);

    return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
  }

  // ===== RESOURCE-LEVEL OPERATIONS =====

  /// <summary>
  /// Get a specific program by ID (resource-level read permission)
  /// </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProgramEntity>> GetProgram(Guid id) {
    var program = await programService.GetProgramByIdAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Get a specific program with all content included (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/with-content")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProgramEntity>> GetProgramWithContent(Guid id) {
    var program = await programService.GetProgramWithContentAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Update a program (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramEntity>> UpdateProgram(Guid id, [FromBody] UpdateProgramDto updateDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.UpdateProgramAsync(id, updateDto);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Delete a program (resource-level delete permission)
  /// </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProgram(Guid id) {
    var existingProgram = await programService.GetProgramByIdAsync(id);

    if (existingProgram == null) return NotFound();

    await programService.DeleteProgramAsync(id);

    return NoContent();
  }

  /// <summary>
  /// Clone/duplicate a program (resource-level clone permission)
  /// </summary>
  [HttpPost("{id}/clone")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Clone)]
  public async Task<ActionResult<ProgramEntity>> CloneProgram(Guid id, [FromBody] CloneProgramDto cloneDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.CloneProgramAsync(id, cloneDto.NewTitle);

    if (program == null) return NotFound();

    return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
  }

  /// <summary>
  /// Get a specific program by slug (resource-level read permission)
  /// </summary>
  [HttpGet("slug/{slug}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProgramEntity>> GetProgramBySlug(string slug) {
    var program = await programService.GetProgramBySlugAsync(slug);

    if (program == null) return NotFound();

    return Ok(program);
  }

  // ===== CONTENT MANAGEMENT ENDPOINTS =====

  /// <summary>
  /// Add content to a program (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/content")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramContent>> AddContent(Guid id, [FromBody] CreateContentDto contentDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var content = await programService.AddContentAsync(id, contentDto);

    if (content == null) return NotFound("Program not found");

    return Ok(content);
  }

  /// <summary>
  /// Update program content (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}/content/{contentId}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramContent>> UpdateContent(
    Guid id, Guid contentId,
    [FromBody] UpdateContentDto contentDto
  ) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var content = await programService.UpdateContentAsync(id, contentId, contentDto);

    if (content == null) return NotFound();

    return Ok(content);
  }

  /// <summary>
  /// Remove content from a program (resource-level edit permission)
  /// </summary>
  [HttpDelete("{id}/content/{contentId}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> RemoveContent(Guid id, Guid contentId) {
    var success = await programService.RemoveContentAsync(id, contentId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Reorder content in a program (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/content/reorder")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> ReorderContent(Guid id, [FromBody] ReorderContentDto reorderDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.ReorderContentAsync(id, reorderDto.ContentIds);

    if (program == null) return NotFound();

    return NoContent();
  }

  // ===== USER PARTICIPATION ENDPOINTS =====

  /// <summary>
  /// Add a user to a program (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/users/{userId}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult<UserProgressDto>> AddUserToProgram(Guid id, Guid userId) {
    var progress = await programService.AddUserToProgramAsync(id, userId);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary>
  /// Remove a user from a program (resource-level edit permission)
  /// </summary>
  [HttpDelete("{id}/users/{userId}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> RemoveUserFromProgram(Guid id, Guid userId) {
    var success = await programService.RemoveUserFromProgramAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Get all users in a program (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/users")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<UserProgressDto>>> GetProgramUsers(
    Guid id, [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var users = await programService.GetProgramUsersAsync(id, skip, take);

    return Ok(users);
  }

  /// <summary>
  /// Get a specific user's progress in a program (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/users/{userId}/progress")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<UserProgressDto>> GetUserProgress(Guid id, Guid userId) {
    var progress = await programService.GetUserProgressDtoAsync(id, userId);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary>
  /// Update a user's progress in a program (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}/users/{userId}/progress")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult<UserProgressDto>> UpdateUserProgress(
    Guid id, Guid userId,
    [FromBody] UpdateProgressDto progressDto
  ) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var progress = await programService.UpdateUserProgressAsync(id, userId, progressDto);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary>
  /// Mark content as completed for a user (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/users/{userId}/content/{contentId}/complete")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> MarkContentCompleted(Guid id, Guid userId, Guid contentId) {
    var success = await programService.MarkContentCompletedAsync(id, userId, contentId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Reset user progress in a program (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/users/{userId}/reset")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> ResetUserProgress(Guid id, Guid userId) {
    var success = await programService.ResetUserProgressAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  // ===== LIFECYCLE MANAGEMENT ENDPOINTS =====

  /// <summary>
  /// Submit a program for review (resource-level submit permission)
  /// </summary>
  [HttpPost("{id}/submit")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Submit)]
  public async Task<ActionResult<ProgramEntity>> SubmitProgram(Guid id) {
    var program = await programService.SubmitProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Approve a program (resource-level approve permission)
  /// </summary>
  [HttpPost("{id}/approve")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Approve)]
  public async Task<ActionResult<ProgramEntity>> ApproveProgram(Guid id) {
    var program = await programService.ApproveProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Reject a program (resource-level reject permission)
  /// </summary>
  [HttpPost("{id}/reject")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Reject)]
  public async Task<ActionResult<ProgramEntity>> RejectProgram(Guid id, [FromBody] RejectProgramDto rejectDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.RejectProgramAsync(id, rejectDto.Reason);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Withdraw a program from review (resource-level withdraw permission)
  /// </summary>
  [HttpPost("{id}/withdraw")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Withdraw)]
  public async Task<ActionResult<ProgramEntity>> WithdrawProgram(Guid id) {
    var program = await programService.WithdrawProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Archive a program (resource-level archive permission)
  /// </summary>
  [HttpPost("{id}/archive")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Archive)]
  public async Task<ActionResult<ProgramEntity>> ArchiveProgram(Guid id) {
    var program = await programService.ArchiveProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Restore an archived program (resource-level restore permission)
  /// </summary>
  [HttpPost("{id}/restore")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Restore)]
  public async Task<ActionResult<ProgramEntity>> RestoreProgram(Guid id) {
    var program = await programService.RestoreProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  // ===== PUBLISHING ENDPOINTS =====

  /// <summary>
  /// Publish a program (resource-level publish permission)
  /// </summary>
  [HttpPost("{id}/publish")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Publish)]
  public async Task<ActionResult<ProgramEntity>> PublishProgram(Guid id) {
    var program = await programService.PublishProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Unpublish a program (resource-level unpublish permission)
  /// </summary>
  [HttpPost("{id}/unpublish")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Unpublish)]
  public async Task<ActionResult<ProgramEntity>> UnpublishProgram(Guid id) {
    var program = await programService.UnpublishProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Schedule a program for publishing (resource-level schedule permission)
  /// </summary>
  [HttpPost("{id}/schedule")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Schedule)]
  public async Task<ActionResult<ProgramEntity>> ScheduleProgram(Guid id, [FromBody] ScheduleProgramDto scheduleDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.ScheduleProgramAsync(id, scheduleDto.PublishAt);

    if (program == null) return NotFound();

    return Ok(program);
  }

  // ===== MONETIZATION ENDPOINTS =====

  /// <summary>
  /// Enable monetization for a program (resource-level monetize permission)
  /// </summary>
  [HttpPost("{id}/monetize")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Monetize)]
  public async Task<ActionResult<ProgramEntity>> EnableMonetization(Guid id, [FromBody] MonetizationDto monetizationDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.EnableMonetizationAsync(id, monetizationDto);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Disable monetization for a program (resource-level monetize permission)
  /// </summary>
  [HttpPost("{id}/disable-monetization")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Monetize)]
  public async Task<ActionResult<ProgramEntity>> DisableMonetization(Guid id) {
    var program = await programService.DisableMonetizationAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary>
  /// Get program pricing information (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<PricingDto>> GetProgramPricing(Guid id) {
    var pricing = await programService.GetProgramPricingAsync(id);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  /// <summary>
  /// Update program pricing (resource-level pricing permission)
  /// </summary>
  [HttpPut("{id}/pricing")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Pricing)]
  public async Task<ActionResult<PricingDto>> UpdateProgramPricing(Guid id, [FromBody] UpdatePricingDto pricingDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var pricing = await programService.UpdateProgramPricingAsync(id, pricingDto);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  // ===== ANALYTICS ENDPOINTS =====

  /// <summary>
  /// Get program analytics (resource-level analytics permission)
  /// </summary>
  [HttpGet("{id}/analytics")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Analytics)]
  public async Task<ActionResult<ProgramAnalyticsDto>> GetProgramAnalytics(Guid id) {
    var analytics = await programService.GetProgramAnalyticsAsync(id);

    if (analytics == null) return NotFound();

    return Ok(analytics);
  }

  /// <summary>
  /// Get user completion rates for a program (resource-level analytics permission)
  /// </summary>
  [HttpGet("{id}/analytics/completion-rates")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Analytics)]
  public async Task<ActionResult<CompletionRatesDto>> GetCompletionRates(Guid id) {
    var rates = await programService.GetCompletionRatesAsync(id);

    if (rates == null) return NotFound();

    return Ok(rates);
  }

  /// <summary>
  /// Get program engagement metrics (resource-level analytics permission)
  /// </summary>
  [HttpGet("{id}/analytics/engagement")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Analytics)]
  public async Task<ActionResult<EngagementMetricsDto>> GetEngagementMetrics(Guid id) {
    var metrics = await programService.GetEngagementMetricsAsync(id);

    if (metrics == null) return NotFound();

    return Ok(metrics);
  }

  /// <summary>
  /// Get program revenue analytics (resource-level revenue permission)
  /// </summary>
  [HttpGet("{id}/analytics/revenue")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Revenue)]
  public async Task<ActionResult<RevenueAnalyticsDto>> GetRevenueAnalytics(Guid id) {
    var revenue = await programService.GetRevenueAnalyticsAsync(id);

    if (revenue == null) return NotFound();

    return Ok(revenue);
  }

  // ===== PRODUCT INTEGRATION ENDPOINTS =====

  /// <summary>
  /// Create a product from a program (resource-level edit permission for program, content-type level draft permission for product)
  /// </summary>
  [HttpPost("{id}/create-product")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult<Guid>> CreateProductFromProgram(
    Guid id,
    [FromBody] CreateProductFromProgramDto productDto
  ) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var productId = await programService.CreateProductFromProgramAsync(id, productDto);

    if (productId == null) return NotFound();

    return Ok(new { ProductId = productId });
  }

  /// <summary>
  /// Link a program to an existing product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/link-product/{productId}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> LinkProgramToProduct(Guid id, Guid productId) {
    var success = await programService.LinkProgramToProductAsync(id, productId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Unlink a program from a product (resource-level edit permission)
  /// </summary>
  [HttpDelete("{id}/link-product/{productId}")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Edit)]
  public async Task<ActionResult> UnlinkProgramFromProduct(Guid id, Guid productId) {
    var success = await programService.UnlinkProgramFromProductAsync(id, productId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Get all products linked to a program (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/products")]
  [RequireResourcePermission<ProgramEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Guid>>> GetLinkedProducts(Guid id) {
    var productIds = await programService.GetLinkedProductsAsync(id);

    return Ok(productIds);
  }
}
