using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitializeEconomyGenesisChainHead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO public.economy_chain_head ("Id", "Sequence", "Hash", "UpdatedAt")
                SELECT 1, 0, repeat('0', 64), CURRENT_TIMESTAMP
                WHERE NOT EXISTS (SELECT 1 FROM public.economy_chain_head)
                  AND NOT EXISTS (SELECT 1 FROM public.economy_journal_entries);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM public.economy_chain_head head
                WHERE head."Id" = 1
                  AND head."Sequence" = 0
                  AND head."Hash" = repeat('0', 64)
                  AND NOT EXISTS (SELECT 1 FROM public.economy_journal_entries)
                  AND NOT EXISTS (SELECT 1 FROM public.economy_journal_verification_checkpoints);
                """);
        }
    }
}
