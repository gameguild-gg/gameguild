using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Learning.Courses;

/// <summary>
/// REST API controller for program lifecycle management:
/// submit, approve, reject, withdraw, archive, restore, publish, unpublish, schedule.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses")]
[Authorize]
public class ProgramLifecycleController(IProgramLifecycleService lifecycleService) : BaseApiController {

  /// <summary> Submit a program for review (resource-level submit permission) </summary>
  [HttpPost("{id}:submit")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Submit)]
  public async Task<ActionResult<Program>> SubmitProgram(Guid id) {
    var program = await lifecycleService.SubmitProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Approve a program (resource-level approve permission) </summary>
  [HttpPost("{id}:approve")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Approve)]
  public async Task<ActionResult<Program>> ApproveProgram(Guid id) {
    var program = await lifecycleService.ApproveProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Reject a program (resource-level reject permission) </summary>
  [HttpPost("{id}:reject")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Reject)]
  public async Task<ActionResult<Program>> RejectProgram(Guid id, [FromBody] RejectProgramDto rejectDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await lifecycleService.RejectProgramAsync(id, rejectDto.Reason);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Withdraw a program from review (resource-level withdraw permission) </summary>
  [HttpPost("{id}:withdraw")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Withdraw)]
  public async Task<ActionResult<Program>> WithdrawProgram(Guid id) {
    var program = await lifecycleService.WithdrawProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Archive a program (resource-level archive permission) </summary>
  [HttpPost("{id}:archive")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Archive)]
  public async Task<ActionResult<Program>> ArchiveProgram(Guid id) {
    var program = await lifecycleService.ArchiveProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Restore an archived program (resource-level restore permission) </summary>
  [HttpPost("{id}:restore")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Restore)]
  public async Task<ActionResult<Program>> RestoreProgram(Guid id) {
    var program = await lifecycleService.RestoreProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Publish a program (resource-level publish permission) </summary>
  [HttpPost("{id}:publish")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Publish)]
  public async Task<ActionResult<Program>> PublishProgram(Guid id) {
    var program = await lifecycleService.PublishProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Unpublish a program (resource-level unpublish permission) </summary>
  [HttpPost("{id}:unpublish")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Unpublish)]
  public async Task<ActionResult<Program>> UnpublishProgram(Guid id) {
    var program = await lifecycleService.UnpublishProgramAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Schedule a program for publishing (resource-level schedule permission) </summary>
  [HttpPost("{id}:schedule")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Schedule)]
  public async Task<ActionResult<Program>> ScheduleProgram(Guid id, [FromBody] ScheduleProgramDto scheduleDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await lifecycleService.ScheduleProgramAsync(id, scheduleDto.PublishAt);

    if (program == null) return NotFound();

    return Ok(program);
  }
}
