using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810104300_DeduplicateSpecializedPostingOutbox")]
public partial class DeduplicateSpecializedPostingOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InstallSpecializedPostingOutboxDeduplication(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}