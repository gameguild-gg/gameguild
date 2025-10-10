using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class PermissionsModuleRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenant_permissions_deleted_at",
                table: "tenant_permissions");

            migrationBuilder.CreateTable(
                name: "permission_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permissions = table.Column<PermissionType[]>(type: "jsonb", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    permission_layer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    content_type_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_audit_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "permission_delegations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    delegator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delegate_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delegated_permissions = table.Column<PermissionType[]>(type: "jsonb", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    can_sub_delegate = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    conditions = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_delegations", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_delegations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "permission_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    permissions = table.Column<PermissionType[]>(type: "jsonb", nullable: false),
                    module = table.Column<int>(type: "integer", nullable: true),
                    is_system_template = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    minimum_tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_templates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permission_covering",
                table: "tenant_permissions",
                columns: new[] { "user_id", "tenant_id", "expires_at" },
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "permission_flags1", "permission_flags2", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permission_expiration",
                table: "tenant_permissions",
                columns: new[] { "expires_at", "deleted_at" },
                filter: "expires_at IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permission_global_defaults",
                table: "tenant_permissions",
                column: "deleted_at",
                filter: "user_id IS NULL AND tenant_id IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permission_tenant",
                table: "tenant_permissions",
                columns: new[] { "tenant_id", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permission_user",
                table: "tenant_permissions",
                columns: new[] { "user_id", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permission_user_tenant_active",
                table: "tenant_permissions",
                columns: new[] { "user_id", "tenant_id", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permission_content_type",
                table: "content_type_permissions",
                columns: new[] { "content_type", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permission_type_defaults",
                table: "content_type_permissions",
                columns: new[] { "tenant_id", "content_type", "deleted_at" },
                filter: "user_id IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permission_user",
                table: "content_type_permissions",
                columns: new[] { "user_id", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permission_user_tenant_type",
                table: "content_type_permissions",
                columns: new[] { "user_id", "tenant_id", "content_type", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_log_analytics",
                table: "permission_audit_logs",
                columns: new[] { "tenant_id", "operation", "performed_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_log_failures_time",
                table: "permission_audit_logs",
                columns: new[] { "is_success", "performed_at" },
                descending: new[] { false, true },
                filter: "is_success = false");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_log_operation_time",
                table: "permission_audit_logs",
                columns: new[] { "operation", "performed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_log_resource_time",
                table: "permission_audit_logs",
                columns: new[] { "resource_id", "performed_at" },
                descending: new[] { false, true },
                filter: "resource_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_log_tenant_time",
                table: "permission_audit_logs",
                columns: new[] { "tenant_id", "performed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_log_user_time",
                table: "permission_audit_logs",
                columns: new[] { "user_id", "performed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_created_at",
                table: "permission_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_deleted_at",
                table: "permission_audit_logs",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_operation",
                table: "permission_audit_logs",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_performed_at",
                table: "permission_audit_logs",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_resource_id",
                table: "permission_audit_logs",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_tenant_id",
                table: "permission_audit_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_audit_logs_user_id",
                table: "permission_audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegation_delegate_active",
                table: "permission_delegations",
                columns: new[] { "delegate_user_id", "tenant_id", "is_active", "expires_at" },
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegation_delegator",
                table: "permission_delegations",
                columns: new[] { "delegator_user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegation_expiration",
                table: "permission_delegations",
                columns: new[] { "expires_at", "is_active" },
                filter: "expires_at IS NOT NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegation_resource",
                table: "permission_delegations",
                columns: new[] { "resource_id", "is_active" },
                filter: "resource_id IS NOT NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegation_tenant",
                table: "permission_delegations",
                columns: new[] { "tenant_id", "is_active" },
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_created_at",
                table: "permission_delegations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_delegate_user_id",
                table: "permission_delegations",
                column: "delegate_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_delegator_user_id",
                table: "permission_delegations",
                column: "delegator_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_deleted_at",
                table: "permission_delegations",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_expires_at",
                table: "permission_delegations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_is_active",
                table: "permission_delegations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_resource_id",
                table: "permission_delegations",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_delegations_tenant_id",
                table: "permission_delegations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_template_category",
                table: "permission_templates",
                columns: new[] { "category", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_permission_template_module",
                table: "permission_templates",
                columns: new[] { "module", "is_active" },
                filter: "module IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_permission_template_name_unique",
                table: "permission_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permission_template_system",
                table: "permission_templates",
                columns: new[] { "is_system_template", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_permission_templates_created_at",
                table: "permission_templates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_templates_deleted_at",
                table: "permission_templates",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_permission_templates_is_system_template",
                table: "permission_templates",
                column: "is_system_template");

            migrationBuilder.CreateIndex(
                name: "ix_permission_templates_module",
                table: "permission_templates",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_permission_templates_name",
                table: "permission_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permission_templates_tenant_id",
                table: "permission_templates",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission_audit_logs");

            migrationBuilder.DropTable(
                name: "permission_delegations");

            migrationBuilder.DropTable(
                name: "permission_templates");

            migrationBuilder.DropIndex(
                name: "ix_tenant_permission_covering",
                table: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_permission_expiration",
                table: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_permission_global_defaults",
                table: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_permission_tenant",
                table: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_permission_user",
                table: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_permission_user_tenant_active",
                table: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "ix_content_type_permission_content_type",
                table: "content_type_permissions");

            migrationBuilder.DropIndex(
                name: "ix_content_type_permission_type_defaults",
                table: "content_type_permissions");

            migrationBuilder.DropIndex(
                name: "ix_content_type_permission_user",
                table: "content_type_permissions");

            migrationBuilder.DropIndex(
                name: "ix_content_type_permission_user_tenant_type",
                table: "content_type_permissions");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_deleted_at",
                table: "tenant_permissions",
                column: "deleted_at");
        }
    }
}
