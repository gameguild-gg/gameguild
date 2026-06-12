using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tag_proficiencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ProficiencyLevel = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_proficiencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certificate_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagProficiencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_tags_tag_proficiencies_TagProficiencyId",
                        column: x => x.TagProficiencyId,
                        principalTable: "tag_proficiencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_CertificateId",
                table: "certificate_tags",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_CertificateId_TagProficiencyId",
                table: "certificate_tags",
                columns: new[] { "CertificateId", "TagProficiencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_TagProficiencyId",
                table: "certificate_tags",
                column: "TagProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_IsActive",
                table: "tag_proficiencies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_Name",
                table: "tag_proficiencies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_ProficiencyLevel",
                table: "tag_proficiencies",
                column: "ProficiencyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_Type",
                table: "tag_proficiencies",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificate_tags");

            migrationBuilder.DropTable(
                name: "tag_proficiencies");
        }
    }
}
