using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations {
  /// <inheritdoc />
  public partial class AddUsernameColumnFixed : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      // Add Username column if it doesn't exist (conditional SQL)
      migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    user_record RECORD;
                    base_username TEXT;
                    final_username TEXT;
                    counter INTEGER;
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Users' AND column_name = 'Username') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""Username"" character varying(50) DEFAULT '';
                        
                        -- Generate unique usernames for each user
                        FOR user_record IN SELECT ""Id"", ""Name"", ""Email"" FROM ""Users""
                        LOOP
                            -- Generate base username
                            base_username := CASE 
                                WHEN user_record.""Name"" IS NOT NULL AND user_record.""Name"" != '' THEN 
                                    LOWER(REGEXP_REPLACE(REGEXP_REPLACE(user_record.""Name"", '[^a-zA-Z0-9\s-]', '', 'g'), '\s+', '-', 'g'))
                                WHEN user_record.""Email"" IS NOT NULL AND user_record.""Email"" != '' THEN 
                                    LOWER(SPLIT_PART(user_record.""Email"", '@', 1))
                                ELSE 
                                    'user-' || user_record.""Id""::text
                            END;
                            
                            -- Ensure username is unique by adding counter if needed
                            final_username := base_username;
                            counter := 1;
                            
                            WHILE EXISTS (SELECT 1 FROM ""Users"" WHERE ""Username"" = final_username AND ""Id"" != user_record.""Id"")
                            LOOP
                                final_username := base_username || '-' || counter;
                                counter := counter + 1;
                            END LOOP;
                            
                            -- Update the user with the unique username
                            UPDATE ""Users"" SET ""Username"" = final_username WHERE ""Id"" = user_record.""Id"";
                        END LOOP;
                        
                        -- Make Username NOT NULL and add unique constraint
                        ALTER TABLE ""Users"" ALTER COLUMN ""Username"" SET NOT NULL;
                        CREATE UNIQUE INDEX ""IX_Users_Username"" ON ""Users"" (""Username"");
                        
                        RAISE NOTICE 'Username column added successfully';
                    ELSE
                        RAISE NOTICE 'Username column already exists';
                    END IF;
                END $$;
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      // Remove Username column and index if they exist
      migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Users' AND column_name = 'Username') THEN
                        DROP INDEX IF EXISTS ""IX_Users_Username"";
                        ALTER TABLE ""Users"" DROP COLUMN ""Username"";
                        RAISE NOTICE 'Username column removed successfully';
                    ELSE
                        RAISE NOTICE 'Username column does not exist';
                    END IF;
                END $$;
            ");
    }
  }
}
