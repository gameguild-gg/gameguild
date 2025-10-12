using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations;

/// <inheritdoc />
public partial class AddPostgreSqlRls : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        EnableRowLevelSecurity(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DisableRowLevelSecurity(migrationBuilder);
    }

    private static void EnableRowLevelSecurity(MigrationBuilder migrationBuilder)
    {
        // Create application user role for RLS
        migrationBuilder.Sql(
            @"
-- Create application user role if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'application_user') THEN
        CREATE ROLE application_user;
    END IF;
END
$$;

-- Grant necessary permissions to application_user
GRANT CONNECT ON DATABASE current_database() TO application_user;
GRANT USAGE ON SCHEMA public TO application_user;

-- Function to safely get current tenant ID
CREATE OR REPLACE FUNCTION get_current_tenant_id()
RETURNS uuid AS $$
BEGIN
    RETURN COALESCE(
        NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
        NULL
    );
EXCEPTION
    WHEN others THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to check if RLS should be bypassed
CREATE OR REPLACE FUNCTION should_bypass_rls()
RETURNS boolean AS $$
BEGIN
    RETURN current_setting('app.bypass_rls', true) = 'true';
EXCEPTION
    WHEN others THEN
        RETURN false;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Grant execute permissions on utility functions
GRANT EXECUTE ON FUNCTION get_current_tenant_id() TO application_user;
GRANT EXECUTE ON FUNCTION should_bypass_rls() TO application_user;
"
        );

        // Enable RLS on tenant-scoped tables
        migrationBuilder.Sql(
            @"
-- Enable RLS for UserProfiles (example tenant-scoped table)
ALTER TABLE ""UserProfiles"" ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON ""UserProfiles""
    FOR ALL
    TO application_user
    USING (
        ""TenantId"" = get_current_tenant_id()
        OR should_bypass_rls()
    );

-- Enable RLS for Resources (if exists)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Resources') THEN
        EXECUTE 'ALTER TABLE ""Resources"" ENABLE ROW LEVEL SECURITY';
        EXECUTE 'CREATE POLICY tenant_isolation ON ""Resources""
            FOR ALL
            TO application_user
            USING (
                ""TenantId"" = get_current_tenant_id()
                OR should_bypass_rls()
            )';
    END IF;
END
$$;

-- Enable RLS for any other tenant-scoped tables that exist
DO $$
BEGIN
    -- Add more tables here as needed for tenant isolation
    -- This can be extended based on the actual schema
END
$$;
"
        );

        // Grant permissions to application_user
        migrationBuilder.Sql(
            @"
-- Grant permissions to application_user on tenant-scoped tables
GRANT SELECT, INSERT, UPDATE, DELETE ON ""UserProfiles"" TO application_user;

-- Grant permissions on other tables as they become tenant-scoped
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Resources') THEN
        GRANT SELECT, INSERT, UPDATE, DELETE ON ""Resources"" TO application_user;
    END IF;
END
$$;
"
        );
    }

    private static void DisableRowLevelSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"
-- Disable RLS for tenant-scoped tables
ALTER TABLE ""UserProfiles"" DISABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ""UserProfiles"";

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Resources') THEN
        EXECUTE 'ALTER TABLE ""Resources"" DISABLE ROW LEVEL SECURITY';
        EXECUTE 'DROP POLICY IF EXISTS tenant_isolation ON ""Resources""';
    END IF;
END
$$;

-- Drop utility functions
DROP FUNCTION IF EXISTS get_current_tenant_id();
DROP FUNCTION IF EXISTS should_bypass_rls();

-- Drop application user role
DROP ROLE IF EXISTS application_user;
"
        );
    }
}