using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            // Update existing users with generated usernames from their names
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""Username"" = LOWER(REPLACE(REPLACE(""Name"", ' ', '-'), '''', '')) || '-' || SUBSTRING(""Id""::text FROM 1 FOR 8)
                WHERE ""Username"" = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");
        }
    }
}
