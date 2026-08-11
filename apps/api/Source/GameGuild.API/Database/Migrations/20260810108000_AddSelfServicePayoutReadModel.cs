using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810108000_AddSelfServicePayoutReadModel")]
public partial class AddSelfServicePayoutReadModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddSelfServicePayoutReadModelSecurity(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveSelfServicePayoutReadModelSecurity(migrationBuilder);
    }
}
