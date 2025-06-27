using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Modules.Program.Models;

namespace GameGuild.Modules.Program.Models;

/// <summary>
/// DEPRECATED: Resource-level permissions for individual ProgramContent entries.
/// This model is no longer used. ProgramContent now inherits permissions from parent Program entities.
/// Use ProgramPermission instead of ProgramContentPermission.
/// This file is kept for database migration compatibility only.
/// </summary>
[Obsolete("ProgramContent now inherits permissions from parent Program. Use ProgramPermission instead.")]
[Table("program_content_permissions")]
[Index(nameof(UserId))]
[Index(nameof(TenantId))]
[Index(nameof(ResourceId))]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), IsUnique = true)]
public class ProgramContentPermission : ResourcePermission<ProgramContent>
{
    // This entity inherits all required properties from ResourcePermission<ProgramContent>:
    // - Id (Guid)
    // - UserId (Guid) 
    // - TenantId (Guid)
    // - ResourceId (Guid) - Points to ProgramContent.Id
    // - PermissionFlags1/PermissionFlags2 (ulong) - Bitwise flags for permissions
    // - CreatedAt (DateTime)
    // - UpdatedAt (DateTime?)
    // - DeletedAt (DateTime?) for soft delete
    
    // Navigation property to the actual ProgramContent resource
    public override ProgramContent? Resource { get; set; }
}
