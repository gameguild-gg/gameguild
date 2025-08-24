using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations {
  /// <inheritdoc />
  public partial class CleanupDuplicateAdminPermissions : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      // Clean up duplicate permission records for admin user and ensure correct permissions
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
                        all_permissions_flags1 := 9223372036854775807::BIGINT;  -- All 63 bits set
                        all_permissions_flags2 := 268435455::BIGINT;           -- All 28 bits set (permissions 64-91)
                        
                        -- CLEAN UP: Delete all existing tenant permissions for admin
                        DELETE FROM ""TenantPermissions"" 
                        WHERE ""UserId"" = super_admin_id;
                        
                        -- CLEAN UP: Delete all existing content type permissions for admin
                        DELETE FROM ""ContentTypePermissions"" 
                        WHERE ""UserId"" = super_admin_id;
                        
                        -- Insert fresh global tenant permissions for super admin
                        INSERT INTO ""TenantPermissions"" (
                            ""Id"", ""UserId"", ""TenantId"", ""PermissionFlags1"", ""PermissionFlags2"",
                            ""CreatedAt"", ""UpdatedAt"", ""ExpiresAt"", ""DeletedAt""
                        )
                        VALUES (
                            gen_random_uuid(), super_admin_id, NULL, all_permissions_flags1, all_permissions_flags2,
                            NOW(), NOW(), NULL, NULL
                        );
                        
                        -- Insert fresh content type permissions for all content types
                        FOREACH content_type IN ARRAY content_types
                        LOOP
                            INSERT INTO ""ContentTypePermissions"" (
                                ""Id"", ""UserId"", ""TenantId"", ""ContentType"", 
                                ""PermissionFlags1"", ""PermissionFlags2"",
                                ""CreatedAt"", ""UpdatedAt"", ""ExpiresAt"", ""DeletedAt""
                            )
                            VALUES (
                                gen_random_uuid(), super_admin_id, NULL, content_type,
                                all_permissions_flags1, all_permissions_flags2,
                                NOW(), NOW(), NULL, NULL
                            );
                        END LOOP;
                        
                        RAISE NOTICE 'Successfully cleaned up and recreated all permissions for super admin user: %', super_admin_id;
                        RAISE NOTICE 'Admin now has clean permission records with Draft (21) and all other permissions (1-91)';
                    ELSE
                        RAISE NOTICE 'Super admin user with email admin@gameguild.local not found';
                    END IF;
                END
                $$;
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      // This migration cleans up permissions, so rollback would remove all admin permissions
      // which could lock out the admin. For safety, we're not removing permissions in rollback.
      migrationBuilder.Sql(@"
                -- Note: This migration cleans up duplicate admin permissions. 
                -- Rolling back would remove all admin permissions which could lock out the admin.
                -- For safety, we're not removing permissions in the rollback.
                SELECT 1; -- No-op to ensure valid SQL
            ");
    }
  }
}
