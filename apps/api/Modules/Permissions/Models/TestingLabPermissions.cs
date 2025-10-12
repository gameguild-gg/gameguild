using GameGuild.Modules.Permissions;

namespace GameGuild.Core.Domain.Permissions;

/// <summary> Testing Lab specific permissions result </summary>
public class TestingLabPermissions
{
    public bool CanCreateSessions { get; set; }

    public bool CanEditSessions { get; set; }

    public bool CanDeleteSessions { get; set; }

    public bool CanManageTesters { get; set; }

    public bool CanViewReports { get; set; }

    public bool CanExportData { get; set; }

    public bool CanAdminister { get; set; }

    public List<string> AssignedRoles { get; set; } = [];

    public List<PermissionConstraint> Constraints { get; set; } = [];
}
