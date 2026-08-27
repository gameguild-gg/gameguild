using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyDriftedObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dead FK from the surviving ProjectJamSubmissions table into the legacy
            // "Jams" table (the model keeps only the IX_ProjectJamSubmissions_Jam index);
            // Postgres refuses DROP TABLE "Jams" while this constraint exists.
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectJamSubmissions_Jams_JamId",
                schema: "public",
                table: "ProjectJamSubmissions");

            // Referencing tables drop before the tables they reference.
            migrationBuilder.DropTable(
                name: "JamScores",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Jams",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TeamMembers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "public");

            migrationBuilder.DropTable(
                name: "economy_wallet_debt_events_legacy",
                schema: "public");

            migrationBuilder.DropTable(
                name: "economy_wallet_debts_legacy",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "is_sms_enabled",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "sms_phone_number",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "sms_verification_code_hash",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "sms_verification_expires_at",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "public",
                table: "UserMetadata");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "public",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "public",
                table: "UserPreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The 7 dead columns, recreated exactly as the migrations that introduced
            // them (20260210011036_AddCoursesModule; 20260611105409_AddSmsMfaResourceIntegrationFields).
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "public",
                table: "UserPreferences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "public",
                table: "UserNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "public",
                table: "UserMetadata",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId1",
                schema: "public",
                table: "UserPreferences",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId1",
                schema: "public",
                table: "UserNotifications",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserMetadata_UserId1",
                schema: "public",
                table: "UserMetadata",
                column: "UserId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMetadata_Users_UserId1",
                schema: "public",
                table: "UserMetadata",
                column: "UserId1",
                principalTable: "Users",
                principalSchema: "public",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserNotifications_Users_UserId1",
                schema: "public",
                table: "UserNotifications",
                column: "UserId1",
                principalTable: "Users",
                principalSchema: "public",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_Users_UserId1",
                schema: "public",
                table: "UserPreferences",
                column: "UserId1",
                principalTable: "Users",
                principalSchema: "public",
                principalColumn: "Id");

            migrationBuilder.AddColumn<bool>(
                name: "is_sms_enabled",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sms_phone_number",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sms_verification_code_hash",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sms_verification_expires_at",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "timestamp with time zone",
                nullable: true);

            // The 6 legacy tables, recreated verbatim from a read-only pg_dump of
            // production (2026-08-27; evidence: .omo/evidence/drift-remediation/phase2b-legacy-ddl-reduced.sql).
            migrationBuilder.Sql(
                """
            CREATE TABLE public."JamScores" (
                "Id" uuid NOT NULL,
                "JamSubmissionId" uuid NOT NULL,
                "JudgeId" uuid NOT NULL,
                "Score" numeric NOT NULL,
                "Category" character varying(500),
                "Comments" character varying(2000),
                "Version" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "DeletedAt" timestamp with time zone,
                "TenantId" uuid
            );
            CREATE TABLE public."Jams" (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(2000),
                "StartDate" timestamp with time zone NOT NULL,
                "EndDate" timestamp with time zone NOT NULL,
                "IsActive" boolean NOT NULL,
                "Version" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "DeletedAt" timestamp with time zone,
                "TenantId" uuid
            );
            CREATE TABLE public."TeamMembers" (
                "Id" uuid NOT NULL,
                "TeamId" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "Role" character varying(100) NOT NULL,
                "JoinedAt" timestamp with time zone NOT NULL,
                "IsActive" boolean NOT NULL,
                "Version" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "DeletedAt" timestamp with time zone,
                "TenantId" uuid
            );
            CREATE TABLE public."Teams" (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(2000),
                "IsActive" boolean NOT NULL,
                "Version" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "DeletedAt" timestamp with time zone,
                "TenantId" uuid
            );
            CREATE TABLE public.economy_wallet_debt_events_legacy (
                "Id" uuid NOT NULL,
                "DebtId" uuid NOT NULL,
                "OperationId" uuid NOT NULL,
                "Kind" integer NOT NULL,
                "AmountUnits" bigint NOT NULL,
                "OccurredAt" timestamp with time zone NOT NULL,
                CONSTRAINT ck_economy_wallet_debt_events_amount CHECK (("AmountUnits" > 0))
            );
            CREATE TABLE public.economy_wallet_debts_legacy (
                "Id" uuid NOT NULL,
                "WalletId" uuid NOT NULL,
                "RootSourceStampId" uuid NOT NULL,
                "Currency" integer NOT NULL,
                "AmountUnits" bigint NOT NULL,
                "OutstandingUnits" bigint NOT NULL,
                "State" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT ck_economy_wallet_debts_amounts CHECK ((("Currency" = 1) AND ("AmountUnits" > 0) AND ("OutstandingUnits" >= 0) AND ("OutstandingUnits" <= "AmountUnits")))
            );
            ALTER TABLE ONLY public."JamScores"
                ADD CONSTRAINT "PK_JamScores" PRIMARY KEY ("Id");
            ALTER TABLE ONLY public."Jams"
                ADD CONSTRAINT "PK_Jams" PRIMARY KEY ("Id");
            ALTER TABLE ONLY public."TeamMembers"
                ADD CONSTRAINT "PK_TeamMembers" PRIMARY KEY ("Id");
            ALTER TABLE ONLY public."Teams"
                ADD CONSTRAINT "PK_Teams" PRIMARY KEY ("Id");
            ALTER TABLE ONLY public.economy_wallet_debt_events_legacy
                ADD CONSTRAINT economy_wallet_debt_events_pkey PRIMARY KEY ("Id");
            ALTER TABLE ONLY public.economy_wallet_debts_legacy
                ADD CONSTRAINT economy_wallet_debts_pkey PRIMARY KEY ("Id");
            CREATE INDEX "IX_JamScores_JamSubmissionId" ON public."JamScores" USING btree ("JamSubmissionId");
            CREATE INDEX "IX_TeamMembers_TeamId" ON public."TeamMembers" USING btree ("TeamId");
            CREATE INDEX "IX_TeamMembers_UserId" ON public."TeamMembers" USING btree ("UserId");
            CREATE INDEX ix_economy_wallet_debts_wallet_root ON public.economy_wallet_debts_legacy USING btree ("WalletId", "RootSourceStampId");
            CREATE UNIQUE INDEX ux_economy_wallet_debt_events_operation ON public.economy_wallet_debt_events_legacy USING btree ("OperationId");
            ALTER TABLE ONLY public."JamScores"
                ADD CONSTRAINT "FK_JamScores_ProjectJamSubmissions_JamSubmissionId" FOREIGN KEY ("JamSubmissionId") REFERENCES public."ProjectJamSubmissions"("Id") ON DELETE CASCADE;
            ALTER TABLE ONLY public."TeamMembers"
                ADD CONSTRAINT "FK_TeamMembers_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE CASCADE;
            ALTER TABLE ONLY public."TeamMembers"
                ADD CONSTRAINT "FK_TeamMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;
            ALTER TABLE ONLY public.economy_wallet_debt_events_legacy
                ADD CONSTRAINT "economy_wallet_debt_events_DebtId_fkey" FOREIGN KEY ("DebtId") REFERENCES public.economy_wallet_debts_legacy("Id") ON DELETE RESTRICT;
            ALTER TABLE ONLY public.economy_wallet_debts_legacy
                ADD CONSTRAINT "economy_wallet_debts_RootSourceStampId_fkey" FOREIGN KEY ("RootSourceStampId") REFERENCES public.economy_source_stamps("Id") ON DELETE RESTRICT;
            ALTER TABLE ONLY public.economy_wallet_debts_legacy
                ADD CONSTRAINT "economy_wallet_debts_WalletId_fkey" FOREIGN KEY ("WalletId") REFERENCES public.economy_wallets("Id") ON DELETE RESTRICT;
            """);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectJamSubmissions_Jams_JamId",
                schema: "public",
                table: "ProjectJamSubmissions",
                column: "JamId",
                principalTable: "Jams",
                principalSchema: "public",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
