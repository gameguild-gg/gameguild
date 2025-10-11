#!/usr/bin/env python3
"""
Fix CS0546 TenantId set accessor errors.
This script removes the set accessor from TenantId overrides since EntityBase only provides a getter.
"""
import os
import re


def fix_tenantid_set_accessor(file_path):
    """Fix TenantId set accessor issues in a file."""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        original_content = content

        # Pattern to match TenantId with set accessor and remove the set part
        # Look for patterns like:
        # public override Guid? TenantId { get; set; } -> public override Guid? TenantId => base.TenantId;
        # or just remove the entire override if it's trying to add a setter
        patterns = [
            # Remove set accessor from TenantId - convert to expression body or just remove override
            (
                r"public\s+override\s+Guid\?\s+TenantId\s*{\s*get;\s*set;\s*}",
                r"// TenantId inherited from EntityBase (no override needed)",
            ),
            (
                r"public\s+override\s+Guid\?\s+TenantId\s*{\s*set;\s*}",
                r"// TenantId inherited from EntityBase (no override needed)",
            ),
            # Handle multiline versions
            (
                r"public\s+override\s+Guid\?\s+TenantId\s*\{\s*\n\s*get;\s*\n\s*set;\s*\n\s*\}",
                r"// TenantId inherited from EntityBase (no override needed)",
            ),
        ]

        changes_made = 0
        for pattern, replacement in patterns:
            new_content, count = re.subn(
                pattern, replacement, content, flags=re.MULTILINE | re.DOTALL
            )
            if count > 0:
                changes_made += count
                content = new_content
                print(f"  Fixed {count} TenantId set accessor issue(s)")

        if changes_made > 0:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content)
            print(
                f"✅ Fixed {changes_made} TenantId set accessor issues in {file_path}"
            )
            return changes_made

        return 0

    except Exception as e:
        print(f"❌ Error processing {file_path}: {e}")
        return 0


# Files with CS0546 errors from build output - TenantId set accessor issues
error_files = [
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tags/Entities/Tag.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/SlaMonitoring/Entities/ServiceLevelObjective.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Teams/Entities/Team.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/RetentionAndCapacity.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantSettings.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Experiments/Entities/PricingExperiment.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Experiments/Entities/UserAssignment.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Payments/Payments.Domain/Entities/AuditTrail.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Permissions/Entities/AccessReview.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Permissions/Entities/ConditionalPolicy.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Permissions/Entities/DataMaskingRule.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Permissions/Entities/PermissionAuditLog.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Payments/Payments.Domain/Entities/RevenueEvent.cs",
]

total_changes = 0
for file_path in error_files:
    if os.path.exists(file_path):
        changes = fix_tenantid_set_accessor(file_path)
        total_changes += changes
    else:
        print(f"⚠️  File not found: {file_path}")

print(f"\n🎯 Total CS0546 TenantId set accessor issues fixed: {total_changes}")
