using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations {
  /// <inheritdoc />
  public partial class FixAdminPermissionsForAllContentTypes : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      // Ensure admin has all permissions including Draft permission for all content types
      migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    super_admin_id UUID;
                    all_permissions_flags1 BIGINT;
                    all_permissions_flags2 BIGINT;
                    content_type TEXT;
                    content_types TEXT[] := ARRAY[
                        'Project', 'TenantDomain', 'TenantUserGroup', 'TenantUserGroupMembership', 
                        'User', 'Tenant', 'Comment', 'Product', 'Program', 'TestingSession', 
                        'TestingRequest', 'TestingFeedback', 'SessionRegistration', 'TestingLabSettings',
                        'Course', 'Lesson', 'Assignment', 'Submission', 'Grade', 'Enrollment'
                    ];
                BEGIN
                    -- Find the super admin user
                    SELECT ""Id"" INTO super_admin_id 
                    FROM ""Users"" 
                    WHERE ""Email"" = 'admin@gameguild.local' AND ""DeletedAt"" IS NULL;
                    
                    IF super_admin_id IS NOT NULL THEN
                        -- Calculate permission flags for ALL available permissions (1-91)
                        -- This ensures we include Draft (21) and all other permissions
                        
                        -- Set all bits for permissions 1-63 in PermissionFlags1
                        -- Use maximum signed bigint value (all 63 bits set)
                        all_permissions_flags1 := 9223372036854775807::BIGINT;
                        
                        -- Set bits for permissions 64-91 in PermissionFlags2 (28 bits: 0-27)
                        -- Use 268435455 which is 2^28 - 1 (all 28 bits set)
                        all_permissions_flags2 := 268435455::BIGINT;
                        
                        -- Update global tenant permissions for super admin
                        INSERT INTO ""TenantPermissions"" (
                            ""Id"", ""UserId"", ""TenantId"", ""PermissionFlags1"", ""PermissionFlags2"",
                            ""CreatedAt"", ""UpdatedAt"", ""ExpiresAt"", ""DeletedAt""
                        )
                        VALUES (
                            gen_random_uuid(), super_admin_id, NULL, all_permissions_flags1, all_permissions_flags2,
                            NOW(), NOW(), NULL, NULL
                        )
                        ON CONFLICT (""UserId"", ""TenantId"") 
                        DO UPDATE SET 
                            ""PermissionFlags1"" = all_permissions_flags1,
                            ""PermissionFlags2"" = all_permissions_flags2,
                            ""UpdatedAt"" = NOW();
                        
                        -- Grant all content type permissions for all content types
                        FOREACH content_type IN ARRAY content_types
                        LOOP
                            -- Insert or update content type permissions for super admin (global)
                            INSERT INTO ""ContentTypePermissions"" (
                                ""Id"", ""UserId"", ""TenantId"", ""ContentType"", 
                                ""PermissionFlags1"", ""PermissionFlags2"",
                                ""CreatedAt"", ""UpdatedAt"", ""ExpiresAt"", ""DeletedAt""
                            )
                            VALUES (
                                gen_random_uuid(), super_admin_id, NULL, content_type,
                                all_permissions_flags1, all_permissions_flags2,
                                NOW(), NOW(), NULL, NULL
                            )
                            ON CONFLICT (""ContentType"", ""UserId"", ""TenantId"") 
                            DO UPDATE SET 
                                ""PermissionFlags1"" = all_permissions_flags1,
                                ""PermissionFlags2"" = all_permissions_flags2,
                                ""UpdatedAt"" = NOW();
                        END LOOP;
                        
                        RAISE NOTICE 'Successfully updated all permissions for super admin user: %', super_admin_id;
                        RAISE NOTICE 'Granted permissions include Draft (21) and all other permissions (1-91)';
                    ELSE
                        RAISE NOTICE 'Super admin user with email admin@gameguild.local not found';
                    END IF;
                END
                $$;
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      // This migration is designed to fix permissions, so rollback would remove all permissions
      // which is not recommended for production. For safety, we'll leave permissions intact.
      migrationBuilder.Sql(@"
                -- Note: This migration fixes admin permissions. 
                -- Rolling back would remove all admin permissions which could lock out the admin.
                -- For safety, we're not removing permissions in the rollback.
                SELECT 1; -- No-op to ensure valid SQL
            ");
    }
  }
}
