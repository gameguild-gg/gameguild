#!/usr/bin/env python3
"""
Fix CS1715 TenantId type mismatch errors.
This script changes 'Guid TenantId' to 'Guid? TenantId' to match EntityBase signature.
"""
import os
import re


def fix_tenantid_type_mismatches(file_path):
    """Fix TenantId type mismatches in a file."""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        original_content = content

        # Pattern to match the wrong TenantId declaration
        # Look for "public override Guid TenantId" and change to "public override Guid? TenantId"
        patterns = [
            (
                r"public\s+override\s+Guid\s+TenantId\s*{",
                r"public override Guid? TenantId {",
            ),
            (
                r"public\s+override\s+Guid\s+TenantId\s*=>",
                r"public override Guid? TenantId =>",
            ),
            # Also handle cases where it might not have override but should
            (r"public\s+Guid\s+TenantId\s*{", r"public override Guid? TenantId {"),
            (r"public\s+Guid\s+TenantId\s*=>", r"public override Guid? TenantId =>"),
        ]

        changes_made = 0
        for pattern, replacement in patterns:
            new_content, count = re.subn(
                pattern, replacement, content, flags=re.MULTILINE
            )
            if count > 0:
                changes_made += count
                content = new_content
                print(
                    f"  Fixed {count} TenantId type mismatch(es) with pattern: {pattern}"
                )

        if changes_made > 0:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content)
            print(f"✅ Fixed {changes_made} TenantId type mismatches in {file_path}")
            return changes_made

        return 0

    except Exception as e:
        print(f"❌ Error processing {file_path}: {e}")
        return 0


# Files with CS1715 errors from build output
error_files = [
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/CostAllocationReport.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/RetentionAndCapacity.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/SlaImpactAnalysis.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/ResourceQuota.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/ResourceThrottlingPolicy.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/ResourceUsageRecord.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Resources/Entities/ResourceUsageTrend.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/DeveloperPortal/Entities/ApiKey.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/DeveloperPortal/Entities/ApiUsageLog.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/DeveloperPortal/Entities/DeveloperOnboarding.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantFeature.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantMember.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantRoleApplication.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantStatistics.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantSubscription.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/TenantWebhook.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/UsageTracking.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Tenants/Entities/UserTenantRole.cs",
    "w:/repositories/game-guild/game-guild/apps/api/Source/Modules/Permissions/Entities/DelegatedAdminScope.cs",
]

total_changes = 0
for file_path in error_files:
    if os.path.exists(file_path):
        changes = fix_tenantid_type_mismatches(file_path)
        total_changes += changes
    else:
        print(f"⚠️  File not found: {file_path}")

print(f"\n🎯 Total CS1715 TenantId type mismatches fixed: {total_changes}")
