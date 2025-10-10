using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class FixDependencyInjectionMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "default_language",
                table: "tenant_settings");

            migrationBuilder.AddColumn<string>(
                name: "family_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "given_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "default_language_id",
                table: "tenant_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_profile_id",
                table: "resource_localizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "languages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "authentication_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_successful = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processing_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    device_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_suspicious = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authentication_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_authentication_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "content_type_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    permission_flags1 = table.Column<long>(type: "bigint", nullable: false),
                    permission_flags2 = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_type_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_type_permissions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_content_type_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_credentials_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_credentials_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mfa_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_successful = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mfa_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_mfa_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "resource_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    type = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    soft_limit = table.Column<long>(type: "bigint", nullable: true),
                    hard_limit = table.Column<long>(type: "bigint", nullable: true),
                    current_usage = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    period = table.Column<int>(type: "integer", nullable: false),
                    last_reset = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reset_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    reset_day_of_week = table.Column<int>(type: "integer", nullable: true),
                    reset_day_of_month = table.Column<int>(type: "integer", nullable: true),
                    notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    notification_thresholds = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_quotas", x => x.id);
                    table.ForeignKey(
                        name: "fk_resource_quotas_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_usage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    type = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    average_per_day = table.Column<double>(type: "double precision", nullable: true),
                    peak_usage = table.Column<long>(type: "bigint", nullable: true),
                    peak_usage_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_usage_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_resource_usage_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    permission_flags1 = table.Column<long>(type: "bigint", nullable: false),
                    permission_flags2 = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_permissions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tenant_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trusted_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_fingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    device_info = table.Column<string>(type: "jsonb", nullable: false),
                    trusted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    associated_ip_addresses = table.Column<string>(type: "jsonb", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trusted_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_trusted_devices_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_mfa_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    totp_secret_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    backup_codes = table.Column<string>(type: "jsonb", nullable: true),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_out_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    preferred_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    qr_code_setup_data = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_setup_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_mfa_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_mfa_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_level = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    access_token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    device_fingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    device_info = table.Column<string>(type: "jsonb", nullable: true),
                    location = table.Column<string>(type: "jsonb", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    termination_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    terminated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_trusted_device = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    trusted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_default_language_id",
                table: "tenant_settings",
                column: "default_language_id");

            migrationBuilder.CreateIndex(
                name: "ix_resource_localizations_user_profile_id",
                table: "resource_localizations",
                column: "user_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_language_unique_default",
                table: "languages",
                column: "is_default",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_attempted_at",
                table: "authentication_attempts",
                column: "attempted_at");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_correlation_id",
                table: "authentication_attempts",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_created_at",
                table: "authentication_attempts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_deleted_at",
                table: "authentication_attempts",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_email",
                table: "authentication_attempts",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_email_attempted_at",
                table: "authentication_attempts",
                columns: new[] { "email", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_ip_address",
                table: "authentication_attempts",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_ip_address_attempted_at",
                table: "authentication_attempts",
                columns: new[] { "ip_address", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_is_successful",
                table: "authentication_attempts",
                column: "is_successful");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_is_suspicious",
                table: "authentication_attempts",
                column: "is_suspicious");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_risk_score",
                table: "authentication_attempts",
                column: "risk_score");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_session_id",
                table: "authentication_attempts",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_tenant_id",
                table: "authentication_attempts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_user_id",
                table: "authentication_attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_attempts_user_id_is_successful_attempted_at",
                table: "authentication_attempts",
                columns: new[] { "user_id", "is_successful", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_content_type_tenant",
                table: "content_type_permissions",
                columns: new[] { "content_type", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_content_type_user_tenant",
                table: "content_type_permissions",
                columns: new[] { "content_type", "user_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_created_at",
                table: "content_type_permissions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_deleted_at",
                table: "content_type_permissions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_expires_at",
                table: "content_type_permissions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_tenant_id",
                table: "content_type_permissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_type_permissions_user_id",
                table: "content_type_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_created_at",
                table: "credentials",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_deleted_at",
                table: "credentials",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id",
                table: "credentials",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_user_id_type",
                table: "credentials",
                columns: new[] { "user_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_created_at",
                table: "mfa_attempts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_deleted_at",
                table: "mfa_attempts",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_ip_address",
                table: "mfa_attempts",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_ip_address_created_at",
                table: "mfa_attempts",
                columns: new[] { "ip_address", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_is_successful",
                table: "mfa_attempts",
                column: "is_successful");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_method",
                table: "mfa_attempts",
                column: "method");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_session_id",
                table: "mfa_attempts",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_tenant_id",
                table: "mfa_attempts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_user_id",
                table: "mfa_attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_attempts_user_id_is_successful_created_at",
                table: "mfa_attempts",
                columns: new[] { "user_id", "is_successful", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_created_at",
                table: "refresh_tokens",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_deleted_at",
                table: "refresh_tokens",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_is_revoked",
                table: "refresh_tokens",
                column: "is_revoked");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_tenant_id",
                table: "refresh_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_resource_quotas_created_at",
                table: "resource_quotas",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_resource_quotas_deleted_at",
                table: "resource_quotas",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_resource_quotas_tenant_id_type",
                table: "resource_quotas",
                columns: new[] { "tenant_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_resource_usage_records_created_at",
                table: "resource_usage_records",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_resource_usage_records_deleted_at",
                table: "resource_usage_records",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_resource_usage_records_tenant_id_type_period_start",
                table: "resource_usage_records",
                columns: new[] { "tenant_id", "type", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_created_at",
                table: "tenant_permissions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_deleted_at",
                table: "tenant_permissions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_expires_at",
                table: "tenant_permissions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_tenant_id",
                table: "tenant_permissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_user_id",
                table: "tenant_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_permissions_user_tenant",
                table: "tenant_permissions",
                columns: new[] { "user_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_created_at",
                table: "trusted_devices",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_deleted_at",
                table: "trusted_devices",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_device_fingerprint",
                table: "trusted_devices",
                column: "device_fingerprint");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_expires_at",
                table: "trusted_devices",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_is_active",
                table: "trusted_devices",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_last_used_at",
                table: "trusted_devices",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_tenant_id",
                table: "trusted_devices",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_trusted_at",
                table: "trusted_devices",
                column: "trusted_at");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_user_id",
                table: "trusted_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_user_id_device_fingerprint",
                table: "trusted_devices",
                columns: new[] { "user_id", "device_fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_user_id_is_active",
                table: "trusted_devices",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_created_at",
                table: "user_mfa_configurations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_deleted_at",
                table: "user_mfa_configurations",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_enabled_at",
                table: "user_mfa_configurations",
                column: "enabled_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_is_enabled",
                table: "user_mfa_configurations",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_last_used_at",
                table: "user_mfa_configurations",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_preferred_method",
                table: "user_mfa_configurations",
                column: "preferred_method");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_tenant_id",
                table: "user_mfa_configurations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configurations_user_id",
                table: "user_mfa_configurations",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_created_at",
                table: "user_profiles",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_deleted_at",
                table: "user_profiles",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_tenant_id",
                table: "user_profiles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_created_at",
                table: "user_sessions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_deleted_at",
                table: "user_sessions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_device_fingerprint",
                table: "user_sessions",
                column: "device_fingerprint");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_expires_at",
                table: "user_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_ip_address_created_at",
                table: "user_sessions",
                columns: new[] { "ip_address", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_is_active",
                table: "user_sessions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_is_trusted_device",
                table: "user_sessions",
                column: "is_trusted_device");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_last_used_at",
                table: "user_sessions",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_refresh_token",
                table: "user_sessions",
                column: "refresh_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_tenant_id",
                table: "user_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id_is_active",
                table: "user_sessions",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id_is_trusted_device",
                table: "user_sessions",
                columns: new[] { "user_id", "is_trusted_device" });

            migrationBuilder.AddForeignKey(
                name: "fk_resource_localizations_user_profiles_user_profile_id",
                table: "resource_localizations",
                column: "user_profile_id",
                principalTable: "user_profiles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_settings_languages_default_language_id",
                table: "tenant_settings",
                column: "default_language_id",
                principalTable: "languages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_resource_localizations_user_profiles_user_profile_id",
                table: "resource_localizations");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_settings_languages_default_language_id",
                table: "tenant_settings");

            migrationBuilder.DropTable(
                name: "authentication_attempts");

            migrationBuilder.DropTable(
                name: "content_type_permissions");

            migrationBuilder.DropTable(
                name: "credentials");

            migrationBuilder.DropTable(
                name: "mfa_attempts");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "resource_quotas");

            migrationBuilder.DropTable(
                name: "resource_usage_records");

            migrationBuilder.DropTable(
                name: "tenant_permissions");

            migrationBuilder.DropTable(
                name: "trusted_devices");

            migrationBuilder.DropTable(
                name: "user_mfa_configurations");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_settings_default_language_id",
                table: "tenant_settings");

            migrationBuilder.DropIndex(
                name: "ix_resource_localizations_user_profile_id",
                table: "resource_localizations");

            migrationBuilder.DropIndex(
                name: "ix_language_unique_default",
                table: "languages");

            migrationBuilder.DropColumn(
                name: "family_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "given_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "default_language_id",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "user_profile_id",
                table: "resource_localizations");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "languages");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "default_language",
                table: "tenant_settings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en-US");
        }
    }
}
