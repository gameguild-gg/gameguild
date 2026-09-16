using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810104200_PublishAcceptedRegisteredPostings")]
public partial class PublishAcceptedRegisteredPostings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InstallRegisteredPostingOutbox(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}