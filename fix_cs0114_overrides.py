#!/usr/bin/env python3

import os
import re


def fix_cs0114_hidden_members():
    """Fix CS0114 warnings by adding override keywords to hidden inherited members."""

    # Common patterns to fix
    patterns = [
        # TenantId properties
        (
            r"^(\s*)(public\s+(?:override\s+)?)(Guid\?\s+TenantId\s*{\s*get;\s*(?:protected\s+|private\s+)?set;\s*})",
            r"\1public override \3",
        ),
        (
            r"^(\s*)(public\s+(?:override\s+)?)(Guid\?\s+TenantId\s*{\s*get;\s*set;\s*})",
            r"\1public override \3",
        ),
        (
            r"^(\s*)(public\s+(?:override\s+)?)(Guid\s+TenantId\s*{\s*get;\s*(?:protected\s+|private\s+)?set;\s*})",
            r"\1public override \3",
        ),
        (
            r"^(\s*)(public\s+(?:override\s+)?)(Guid\s+TenantId\s*{\s*get;\s*set;\s*})",
            r"\1public override \3",
        ),
        # Tenant navigation properties
        (
            r"^(\s*)(public\s+(?:override\s+)?)((?:Tenant|virtual\s+Tenant)\s+Tenant\s*{\s*get;\s*set;\s*})",
            r"\1public override \3",
        ),
        # Id properties
        (
            r"^(\s*)(public\s+(?:override\s+)?)(Guid\s+Id\s*{\s*get;\s*(?:protected\s+|private\s+)?set;\s*})",
            r"\1public override \3",
        ),
        # IsGlobal properties
        (
            r"^(\s*)(public\s+(?:override\s+)?)(bool\s+IsGlobal\s*{\s*get[^}]*})",
            r"\1public override \3",
        ),
        # Version properties
        (
            r"^(\s*)(public\s+(?:override\s+)?)(int\s+Version\s*{\s*get;\s*(?:protected\s+|private\s+)?set;\s*})",
            r"\1public override \3",
        ),
        # UpdatedAt properties
        (
            r"^(\s*)(public\s+(?:override\s+)?)(DateTime\s+UpdatedAt\s*{\s*get;\s*(?:protected\s+|private\s+)?set;\s*})",
            r"\1public override \3",
        ),
    ]

    # Get all CS0114 errors to target specific files
    target_files = [
        "apps/api/Source/Modules/Resources/Entities/CostAllocationReport.cs",
        "apps/api/Source/Modules/Tags/Entities/Tag.cs",
        "apps/api/Source/Modules/Teams/Entities/Team.cs",
        "apps/api/Source/Modules/SlaMonitoring/Entities/ServiceLevelObjective.cs",
        "apps/api/Source/Modules/Resources/Entities/RetentionAndCapacity.cs",
        "apps/api/Source/Modules/Resources/Entities/SlaImpactAnalysis.cs",
        "apps/api/Source/Modules/Permissions/Entities/AccessReview.cs",
        "apps/api/Source/Modules/Permissions/Entities/ConditionalPolicy.cs",
        "apps/api/Source/Modules/Permissions/Entities/DataMaskingRule.cs",
        "apps/api/Source/Modules/Permissions/Entities/JitElevationRequest.cs",
        "apps/api/Source/Modules/Permissions/Entities/DelegatedAdminScope.cs",
        "apps/api/Source/Modules/Resources/Entities/ResourceQuota.cs",
        "apps/api/Source/Modules/Permissions/Entities/PermissionAuditLog.cs",
        "apps/api/Source/Modules/Resources/Entities/ResourceThrottlingPolicy.cs",
        "apps/api/Source/Modules/Resources/Entities/ResourceUsageRecord.cs",
        "apps/api/Source/Modules/Permissions/Entities/PermissionDelegation.cs",
        "apps/api/Source/Modules/Resources/Entities/ResourceUsageTrend.cs",
        "apps/api/Source/Modules/Permissions/Entities/PermissionTemplateVersion.cs",
        "apps/api/Source/Modules/Permissions/Entities/PolicyBundle.cs",
        "apps/api/Source/Modules/Permissions/Entities/SoDRule.cs",
        "apps/api/Source/Modules/Permissions/Models/AbacPolicy.cs",
        "apps/api/Source/Modules/Permissions/Models/UserPermission.cs",
        "apps/api/Source/Modules/Permissions/Models/WithPermissions.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantDomain.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantFeature.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantMember.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantRoleApplication.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantSettings.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantStatistics.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantSubscription.cs",
        "apps/api/Source/Modules/Tenants/Entities/TenantWebhook.cs",
        "apps/api/Source/Modules/Tenants/Entities/UsageTracking.cs",
        "apps/api/Source/Modules/Tenants/Entities/UserTenantRole.cs",
        "apps/api/Source/Modules/Subscriptions/Subscriptions.Domain/Entities/Subscription.cs",
        "apps/api/Source/Modules/Users/Entities/BehavioralAnalytics.cs",
        "apps/api/Source/Modules/Programs/Entities/ActivityGrade.cs",
        "apps/api/Source/Modules/Programs/Entities/ContentInteraction.cs",
        "apps/api/Source/Modules/Programs/Entities/Program.cs",
        "apps/api/Source/Modules/Programs/Entities/ProgramContent.cs",
        "apps/api/Source/Modules/Programs/Entities/ProgramUser.cs",
        "apps/api/Source/Modules/Programs/Entities/ProgramWishlist.cs",
        "apps/api/Source/Modules/TestingLab/Entities/FeedbackQualityRating.cs",
        "apps/api/Source/Modules/TestingLab/Entities/TestingFeedback.cs",
        "apps/api/Source/Modules/TestingLab/Entities/TestingFeedbackForm.cs",
        "apps/api/Source/Modules/TestingLab/Entities/TestingLocation.cs",
        "apps/api/Source/Modules/TestingLab/Entities/TestingParticipant.cs",
        "apps/api/Source/Modules/TestingLab/Entities/TestingRequest.cs",
        "apps/api/Source/Modules/TestingLab/Entities/SessionRegistration.cs",
        "apps/api/Source/Modules/TestingLab/Entities/TestingSession.cs",
        "apps/api/Source/Modules/DataArchival/Entities/ArchivalJob.cs",
        "apps/api/Source/Modules/DataArchival/Entities/ArchivalPolicy.cs",
        "apps/api/Source/Modules/ErrorTracking/Entities/ErrorEvent.cs",
        "apps/api/Source/Modules/ErrorTracking/Entities/ErrorIssue.cs",
        "apps/api/Source/Modules/FileUpload/Entities/UploadedFile.cs",
        "apps/api/Source/Modules/Experiments/Entities/PricingExperiment.cs",
        "apps/api/Source/Modules/Experiments/Entities/UserAssignment.cs",
        "apps/api/Source/Modules/DeveloperPortal/Entities/ApiKey.cs",
        "apps/api/Source/Modules/Certificates/Entities/Certificate.cs",
        "apps/api/Source/Modules/DeveloperPortal/Entities/ApiUsageLog.cs",
        "apps/api/Source/Modules/Certificates/Entities/CertificateTag.cs",
        "apps/api/Source/Modules/DeveloperPortal/Entities/DeveloperOnboarding.cs",
        "apps/api/Source/Modules/Certificates/Entities/UserCertificate.cs",
        "apps/api/Source/Modules/Feedbacks/Entities/ProgramRating.cs",
        "apps/api/Source/Modules/Payments/Payments.Domain/Entities/AuditTrail.cs",
        "apps/api/Source/Modules/Payments/Payments.Domain/Entities/RevenueEvent.cs",
    ]

    fixed_count = 0
    total_changes = 0

    for file_path in target_files:
        if not os.path.exists(file_path):
            print(f"⚠️  File not found: {file_path}")
            continue

        try:
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()

            original_content = content
            changes_made = 0

            # Apply each pattern
            for pattern, replacement in patterns:
                new_content, count = re.subn(
                    pattern, replacement, content, flags=re.MULTILINE
                )
                if count > 0:
                    print(
                        f"  🔄 {os.path.basename(file_path)}: Applied pattern {count} times"
                    )
                    content = new_content
                    changes_made += count

            # Only write if we made changes
            if changes_made > 0:
                with open(file_path, "w", encoding="utf-8") as f:
                    f.write(content)

                print(
                    f"✅ Fixed {file_path} - made {changes_made} override keyword additions"
                )
                fixed_count += 1
                total_changes += changes_made
            else:
                print(f"ℹ️  No changes needed in {os.path.basename(file_path)}")

        except Exception as e:
            print(f"❌ Error processing {file_path}: {str(e)}")

    return fixed_count, total_changes


if __name__ == "__main__":
    print("🔧 Fixing CS0114 hidden inherited members by adding override keywords...")
    fixed_count, total_changes = fix_cs0114_hidden_members()
    print(f"\n✨ Fixed {total_changes} override keywords in {fixed_count} files")
