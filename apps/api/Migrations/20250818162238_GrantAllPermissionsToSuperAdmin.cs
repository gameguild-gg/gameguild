using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class GrantAllPermissionsToSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Grant all permissions to the super admin user (admin@gameguild.local) on the global tenant
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    super_admin_id UUID;
                    all_permissions_flags1 BIGINT;
                    all_permissions_flags2 BIGINT;
                BEGIN
                    -- Find the super admin user
                    SELECT ""Id"" INTO super_admin_id 
                    FROM ""Users"" 
                    WHERE ""Email"" = 'admin@gameguild.local' AND ""DeletedAt"" IS NULL;
                    
                    IF super_admin_id IS NOT NULL THEN
                        -- Calculate permission flags for all available permissions (1-91)
                        -- PermissionFlags1 covers permissions 1-63 (bits 0-62)
                        -- PermissionFlags2 covers permissions 64-91 (bits 0-27)
                        
                        -- Set all bits for permissions 1-63 in PermissionFlags1
                        -- Use 9223372036854775807 which is 2^63 - 1 (max signed bigint)
                        all_permissions_flags1 := 9223372036854775807::BIGINT;
                        
                        -- Set bits for permissions 64-91 in PermissionFlags2 (28 bits: 0-27)
                        -- Use 268435455 which is 2^28 - 1
                        all_permissions_flags2 := 268435455::BIGINT;
                        
                        -- Update or insert global tenant permissions for super admin
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
                        
                        -- Grant all content type permissions for common content types
                        DECLARE
                            content_type TEXT;
                            content_types TEXT[] := ARRAY[
                                'Project', 'TenantDomain', 'TenantUserGroup', 'TenantUserGroupMembership', 
                                'User', 'Tenant', 'Comment', 'Product', 'Program', 'TestingSession', 
                                'TestingRequest', 'TestingFeedback', 'SessionRegistration', 'TestingLabSettings'
                            ];
                        BEGIN
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
                        END;
                        
                        RAISE NOTICE 'Successfully granted all permissions to super admin user: %', super_admin_id;
                    ELSE
                        RAISE NOTICE 'Super admin user with email admin@gameguild.local not found';
                    END IF;
                END
                $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all permissions from the super admin user
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    super_admin_id UUID;
                BEGIN
                    -- Find the super admin user
                    SELECT ""Id"" INTO super_admin_id 
                    FROM ""Users"" 
                    WHERE ""Email"" = 'admin@gameguild.local' AND ""DeletedAt"" IS NULL;
                    
                    IF super_admin_id IS NOT NULL THEN
                        -- Remove global tenant permissions
                        DELETE FROM ""TenantPermissions"" 
                        WHERE ""UserId"" = super_admin_id AND ""TenantId"" IS NULL;
                        
                        -- Remove all content type permissions
                        DELETE FROM ""ContentTypePermissions"" 
                        WHERE ""UserId"" = super_admin_id AND ""TenantId"" IS NULL;
                        
                        RAISE NOTICE 'Successfully removed all permissions from super admin user: %', super_admin_id;
                    ELSE
                        RAISE NOTICE 'Super admin user with email admin@gameguild.local not found';
                    END IF;
                END
                $$;
            ");
        }
    }
}
