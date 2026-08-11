using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileBountyEscrowModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The schema was already changed by PreserveBountyEscrowProvenance and
            // PersistBountyEscrowLedgerLots. This migration records their model state.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Snapshot-only reconciliation; the owning migrations retain rollback responsibility.
        }
    }
}
