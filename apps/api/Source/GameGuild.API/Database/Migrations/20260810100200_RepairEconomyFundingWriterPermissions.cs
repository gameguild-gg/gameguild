using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class RepairEconomyFundingWriterPermissions : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RepairFundingWriterPermissions(migrationBuilder);
            RepairWithdrawalAuditSequenceLookup(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveFundingWriterPermissions(migrationBuilder);
        }
    }
}
