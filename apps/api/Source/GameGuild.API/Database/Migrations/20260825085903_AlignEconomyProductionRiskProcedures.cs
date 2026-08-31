using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AlignEconomyProductionRiskProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            InstallDurableRiskCounterReservationProcedure(migrationBuilder);
            InstallTenantScopedSelfServiceRiskDecisionProcedure(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RestoreLegacySelfServiceRiskDecisionProcedure(migrationBuilder);
            RestoreLegacyRiskCounterReservationProcedure(migrationBuilder);
        }
    }
}
