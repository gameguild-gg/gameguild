using GameGuild.Common;
using GameGuild.Modules.Permissions;
using GameGuild.Source.Modules.Certificates.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CertificateEntity = GameGuild.Modules.Certificates.Certificate;
using CreateCertificateDto = GameGuild.Source.Modules.Certificates.Dtos.CreateCertificateDto;
using UpdateCertificateDto = GameGuild.Source.Modules.Certificates.Dtos.UpdateCertificateDto;
using AddCertificateTagDto = GameGuild.Source.Modules.Certificates.Dtos.AddCertificateTagDto;


namespace GameGuild.Modules.Certificates.Controllers;

/// <summary>
/// Manage certificates for a specific Program, including skill tags.
/// Protected by resource-level Program permissions.
/// </summary>
[ApiController]
[Route("api/programs/{programId}/certificates")]
[Authorize]
public class ProgramCertificatesController(Database.ApplicationDbContext db) : ControllerBase {
  /// <summary>
  /// List all certificates associated with a program
  /// </summary>
  [HttpGet]
  [RequireResourcePermission<Programs.ProgramPermission, Programs.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<CertificateEntity>>> GetProgramCertificates(Guid programId) {
    var items = await db.Certificates.Where(c => c.ProgramId == programId).ToListAsync();
    return Ok(items);
  }

  /// <summary>
  /// Create a new certificate under a program
  /// </summary>
  [HttpPost]
  [RequireResourcePermission<Programs.ProgramPermission, Programs.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<CertificateEntity>> CreateCertificate(Guid programId, [FromBody] CreateCertificateDto dto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var entity = dto.ToEntity(programId);
    db.Certificates.Add(entity);
    await db.SaveChangesAsync();
    return CreatedAtAction(nameof(GetProgramCertificates), new { programId }, entity);
  }

  /// <summary>
  /// Update an existing certificate under a program
  /// </summary>
  [HttpPut("{certificateId}")]
  [RequireResourcePermission<Programs.ProgramPermission, Programs.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<CertificateEntity>> UpdateCertificate(
    Guid programId, Guid certificateId, [FromBody] UpdateCertificateDto dto
  ) {
    if (!ModelState.IsValid) return BadRequest(ModelState);
    if (dto.Id != certificateId) return BadRequest("Certificate ID mismatch");

    var existing = await db.Certificates.FirstOrDefaultAsync(c => c.Id == certificateId);
    if (existing == null || existing.ProgramId != programId) return NotFound();

    existing.ApplyUpdates(dto);
    db.Certificates.Update(existing);
    await db.SaveChangesAsync();
    return Ok(existing);
  }

  /// <summary>
  /// Delete a certificate under a program
  /// </summary>
  [HttpDelete("{certificateId}")]
  [RequireResourcePermission<Programs.ProgramPermission, Programs.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> DeleteCertificate(Guid programId, Guid certificateId) {
    var existing = await db.Certificates.FirstOrDefaultAsync(c => c.Id == certificateId);
    if (existing == null || existing.ProgramId != programId) return NotFound();

    db.Certificates.Remove(existing);
    await db.SaveChangesAsync();
    return NoContent();
  }

  /// <summary>
  /// Add a skill tag to a certificate
  /// </summary>
  [HttpPost("{certificateId}/tags")]
  [RequireResourcePermission<Programs.ProgramPermission, Programs.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<CertificateTag>> AddCertificateTag(
    Guid programId, Guid certificateId, [FromBody] AddCertificateTagDto dto
  ) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

  var cert = await db.Certificates.FirstOrDefaultAsync(c => c.Id == certificateId);
    if (cert == null || cert.ProgramId != programId) return NotFound();

    // Verify tag exists
    var tag = await db.TagProficiencies.FindAsync(dto.TagId);
    if (tag == null) return NotFound("Tag not found");

    // Enforce uniqueness (CertificateId, TagId)
    var exists = await db.CertificateTags
      .Where(ct => ct.CertificateId == certificateId && ct.TagId == dto.TagId)
      .AnyAsync();
    if (exists) return Conflict("Tag already added to certificate");

    var ct = new CertificateTag {
      CertificateId = certificateId,
      TagId = dto.TagId,
      RelationshipType = dto.RelationshipType,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
    db.CertificateTags.Add(ct);
    await db.SaveChangesAsync();
    return Ok(ct);
  }

  /// <summary>
  /// Remove a skill tag from a certificate
  /// </summary>
  [HttpDelete("{certificateId}/tags/{tagId}")]
  [RequireResourcePermission<Programs.ProgramPermission, Programs.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> RemoveCertificateTag(Guid programId, Guid certificateId, Guid tagId) {
  var cert = await db.Certificates.FirstOrDefaultAsync(c => c.Id == certificateId);
    if (cert == null || cert.ProgramId != programId) return NotFound();

    var ct = await db.CertificateTags
      .Where(x => x.CertificateId == certificateId && x.TagId == tagId)
      .FirstOrDefaultAsync();
    if (ct == null) return NotFound();

    db.CertificateTags.Remove(ct);
    await db.SaveChangesAsync();
    return NoContent();
  }
}
