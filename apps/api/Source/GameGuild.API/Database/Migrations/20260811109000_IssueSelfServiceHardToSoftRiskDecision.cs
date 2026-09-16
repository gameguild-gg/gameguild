using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811109000_IssueSelfServiceHardToSoftRiskDecision")]
public partial class IssueSelfServiceHardToSoftRiskDecision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallSelfServiceHardToSoftRiskDecisionIssuer(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RemoveSelfServiceHardToSoftRiskDecisionIssuer(migrationBuilder);
}
