using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileDriftedModelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_provider_disputes",
                columns: table => new
                {
                    ProviderDisputeReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BaselineReversedHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    CumulativeDisputedHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    FrozenHardEquivalentUnits = table.Column<long>(type: "bigint", nullable: false),
                    LatestProviderSequence = table.Column<long>(type: "bigint", nullable: false),
                    ResponsibleWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversalIdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_provider_disputes", x => x.ProviderDisputeReference);
                    table.CheckConstraint("ck_economy_provider_disputes_amount_partition", "\"CumulativeDisputedHardUnits\" > 0 AND \"BaselineReversedHardUnits\" >= 0 AND \"BaselineReversedHardUnits\" <= \"CumulativeDisputedHardUnits\" AND \"FrozenHardEquivalentUnits\" >= 0 AND \"FrozenHardEquivalentUnits\" <= (\"CumulativeDisputedHardUnits\" - \"BaselineReversedHardUnits\")");
                    table.CheckConstraint("ck_economy_provider_disputes_lifecycle", "(\"Status\" = 1 AND \"ReversalIdempotencyKey\" IS NULL) OR (\"Status\" = 2 AND \"FrozenHardEquivalentUnits\" = 0 AND \"ReversalIdempotencyKey\" IS NULL) OR (\"Status\" = 3 AND \"FrozenHardEquivalentUnits\" = 0 AND \"ReversalIdempotencyKey\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_provider_disputes_sequence_positive", "\"LatestProviderSequence\" > 0");
                    table.CheckConstraint("ck_economy_provider_disputes_version_positive", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_provider_disputes_economy_source_stamps_SourceStamp~",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_provider_disputes_economy_wallets_ResponsibleWallet~",
                        column: x => x.ResponsibleWalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscountTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TargetSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "economy_dispute_fragment_freezes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    CreditLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProviderDisputeReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RootSourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TerminalAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_dispute_fragment_freezes", x => x.Id);
                    table.CheckConstraint("ck_economy_dispute_fragment_freezes_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_dispute_fragment_freezes_state_timestamp", "(\"Status\" = 1 AND \"TerminalAt\" IS NULL) OR (\"Status\" IN (2, 3) AND \"TerminalAt\" >= \"PlacedAt\")");
                    table.ForeignKey(
                        name: "FK_economy_dispute_fragment_freezes_economy_credit_lots_Credit~",
                        column: x => x.CreditLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_dispute_fragment_freezes_economy_provider_disputes_~",
                        column: x => x.ProviderDisputeReference,
                        principalTable: "economy_provider_disputes",
                        principalColumn: "ProviderDisputeReference",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_dispute_fragment_freezes_economy_source_stamps_Root~",
                        column: x => x.RootSourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_dispute_fragment_freezes_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_provider_dispute_events",
                columns: table => new
                {
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CumulativeDisputedHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProviderDisputeReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderSequence = table.Column<long>(type: "bigint", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_provider_dispute_events", x => x.ProviderEventId);
                    table.CheckConstraint("ck_economy_provider_dispute_events_amount_positive", "\"CumulativeDisputedHardUnits\" > 0");
                    table.CheckConstraint("ck_economy_provider_dispute_events_sequence_positive", "\"ProviderSequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_provider_dispute_events_economy_provider_disputes_P~",
                        column: x => x.ProviderDisputeReference,
                        principalTable: "economy_provider_disputes",
                        principalColumn: "ProviderDisputeReference",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_provider_dispute_events_economy_source_stamps_Sourc~",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdditionalContext = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InitiatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_audit_logs_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    BasePriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BillingIntervalSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrencySnapshot = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsSubscription = table.Column<bool>(type: "boolean", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceVersionSnapshot = table.Column<int>(type: "integer", nullable: false),
                    PricingTierNameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductPricingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductPricingVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromoCodesApplied = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SalePriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_line_items_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_line_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_line_items_user_products_UserProductId",
                        column: x => x.UserProductId,
                        principalTable: "user_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "economy_dispute_fragment_ranges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeFragmentFreezeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndExclusive = table.Column<long>(type: "bigint", nullable: false),
                    ReversalEpoch = table.Column<long>(type: "bigint", nullable: false),
                    StartInclusive = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_dispute_fragment_ranges", x => x.Id);
                    table.CheckConstraint("ck_economy_dispute_fragment_ranges_half_open", "\"StartInclusive\" >= 0 AND \"EndExclusive\" > \"StartInclusive\" AND \"ReversalEpoch\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_dispute_fragment_ranges_economy_dispute_fragment_fr~",
                        column: x => x.DisputeFragmentFreezeId,
                        principalTable: "economy_dispute_fragment_freezes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_dispute_fragment_freezes_CreditLotId",
                table: "economy_dispute_fragment_freezes",
                column: "CreditLotId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_dispute_fragment_freezes_ProviderDisputeReference",
                table: "economy_dispute_fragment_freezes",
                column: "ProviderDisputeReference");

            migrationBuilder.CreateIndex(
                name: "ix_economy_dispute_fragment_freezes_root_status",
                table: "economy_dispute_fragment_freezes",
                columns: new[] { "RootSourceStampId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_dispute_fragment_freezes_WalletId",
                table: "economy_dispute_fragment_freezes",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_dispute_fragment_ranges_freeze_interval",
                table: "economy_dispute_fragment_ranges",
                columns: new[] { "DisputeFragmentFreezeId", "StartInclusive", "EndExclusive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_provider_dispute_events_SourceStampId",
                table: "economy_provider_dispute_events",
                column: "SourceStampId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_provider_dispute_events_dispute_sequence",
                table: "economy_provider_dispute_events",
                columns: new[] { "ProviderDisputeReference", "ProviderSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_provider_disputes_ResponsibleWalletId",
                table: "economy_provider_disputes",
                column: "ResponsibleWalletId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_provider_disputes_active_source",
                table: "economy_provider_disputes",
                column: "SourceStampId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_NewStatus",
                table: "order_audit_logs",
                column: "NewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_OccurredAt",
                table: "order_audit_logs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_OrderId",
                table: "order_audit_logs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_TenantId",
                table: "order_audit_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_OrderId",
                table: "order_line_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_ProductId",
                table: "order_line_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_UserProductId",
                table: "order_line_items",
                column: "UserProductId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt",
                table: "orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_orders_IdempotencyKey",
                table: "orders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status",
                table: "orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId",
                table: "orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId",
                table: "orders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "economy_dispute_fragment_ranges");

            migrationBuilder.DropTable(
                name: "economy_provider_dispute_events");

            migrationBuilder.DropTable(
                name: "order_audit_logs");

            migrationBuilder.DropTable(
                name: "order_line_items");

            migrationBuilder.DropTable(
                name: "economy_dispute_fragment_freezes");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "economy_provider_disputes");
        }
    }
}
