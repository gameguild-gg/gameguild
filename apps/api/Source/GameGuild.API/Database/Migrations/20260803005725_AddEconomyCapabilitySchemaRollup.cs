using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyCapabilitySchemaRollup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_provider_cost_facts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderUsageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    InputCostUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    OutputCostUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ExactProviderCostUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ChargedSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    RateCardVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_provider_cost_facts", x => x.Id);
                    table.CheckConstraint("ck_ai_provider_cost_facts_charge_positive", "\"ChargedSoftUnits\" > 0");
                    table.CheckConstraint("ck_ai_provider_cost_facts_cost_conservation", "\"InputCostUsdNanos\" >= 0 AND \"OutputCostUsdNanos\" >= 0 AND \"ExactProviderCostUsdNanos\" = \"InputCostUsdNanos\" + \"OutputCostUsdNanos\"");
                    table.CheckConstraint("ck_ai_provider_cost_facts_token_conservation", "\"InputTokens\" >= 0 AND \"OutputTokens\" >= 0 AND \"TotalTokens\" = \"InputTokens\" + \"OutputTokens\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_network_policy_versions",
                columns: table => new
                {
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuanceMode = table.Column<int>(type: "integer", nullable: false),
                    YieldState = table.Column<int>(type: "integer", nullable: false),
                    EstimatedNetEcpmUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ContractedRevenueSharePpm = table.Column<int>(type: "integer", nullable: false),
                    SafetyBufferPpm = table.Column<int>(type: "integer", nullable: false),
                    MinimumVisiblePpm = table.Column<int>(type: "integer", nullable: false),
                    MaximumFocusLossTicks = table.Column<long>(type: "bigint", nullable: false),
                    MaximumRewardSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReportsCurrentThrough = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReportStaleAfterTicks = table.Column<long>(type: "bigint", nullable: false),
                    Ranking = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_network_policy_versions", x => new { x.Network, x.Version });
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_ppm", "\"ContractedRevenueSharePpm\" BETWEEN 0 AND 1000000 AND \"SafetyBufferPpm\" BETWEEN 0 AND 999999 AND \"MinimumVisiblePpm\" BETWEEN 0 AND 1000000");
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_values", "\"Version\" > 0 AND \"EstimatedNetEcpmUsdNanos\" > 0 AND \"MaximumRewardSoftUnits\" > 0 AND \"MaximumFocusLossTicks\" >= 0 AND \"ReportStaleAfterTicks\" > 0 AND \"Ranking\" >= 0");
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_provider_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReportId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActualRevenueUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    VerifiedSessionIds = table.Column<string>(type: "jsonb", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Signature = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_provider_reports", x => x.Id);
                    table.CheckConstraint("ck_economy_ad_provider_reports_revenue", "\"ActualRevenueUsdNanos\" >= 0");
                    table.CheckConstraint("ck_economy_ad_provider_reports_version", "\"Version\" > 0");
                    table.CheckConstraint("ck_economy_ad_provider_reports_window", "\"PeriodEnd\" > \"PeriodStart\" AND \"ImportedAt\" >= \"PeriodEnd\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_accumulators",
                columns: table => new
                {
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    RemainderNumerator = table.Column<string>(type: "text", nullable: false),
                    CanonicalDenominator = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_accumulators", x => new { x.WalletId, x.Network });
                    table.CheckConstraint("ck_economy_ad_reward_accumulators_numbers", "\"RemainderNumerator\" ~ '^[0-9]+$' AND \"CanonicalDenominator\" ~ '^[1-9][0-9]*$'");
                    table.CheckConstraint("ck_economy_ad_reward_accumulators_version", "\"PolicyVersion\" > 0 AND \"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_attributions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    ProviderBatchId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EstimatedRevenueUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    RewardSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_attributions", x => x.SessionId);
                    table.CheckConstraint("ck_economy_ad_reward_attributions_nonnegative", "\"EstimatedRevenueUsdNanos\" >= 0 AND \"RewardSoftUnits\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_budget_consumptions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceRiskHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    LossBudgetUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_budget_consumptions", x => x.SessionId);
                    table.CheckConstraint("ck_economy_ad_reward_budget_consumptions_positive", "\"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_completions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    RewardSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostingId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutputLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_completions", x => x.SessionId);
                    table.CheckConstraint("ck_economy_ad_reward_completions_issued_binding", "\"State\" <> 1 OR (\"RewardSoftUnits\" > 0 AND \"SourceStampId\" IS NOT NULL AND \"PostingId\" IS NOT NULL AND \"OutputLotId\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_ad_reward_completions_reward_nonnegative", "\"RewardSoftUnits\" >= 0");
                    table.CheckConstraint("ck_economy_ad_reward_completions_state", "\"State\" BETWEEN 1 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "economy_bounties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PosterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PosterWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    EscrowWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReclaimFeePpm = table.Column<int>(type: "integer", nullable: false),
                    RequiresPrerequisite = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumReputation = table.Column<int>(type: "integer", nullable: false),
                    RequiresInstructorVerification = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_bounties", x => x.Id);
                    table.CheckConstraint("ck_economy_bounties_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_bounties_fee", "\"ReclaimFeePpm\" BETWEEN 0 AND 999999");
                    table.CheckConstraint("ck_economy_bounties_reputation", "\"MinimumReputation\" >= 0");
                    table.CheckConstraint("ck_economy_bounties_state", "\"Status\" BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_economy_bounties_version", "\"Version\" > 0");
                    table.CheckConstraint("ck_economy_bounties_window", "\"ExpiresAt\" > \"PostedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_currency_policy_versions",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    HardPriceUnits = table.Column<long>(type: "bigint", nullable: false),
                    SoftPriceUnits = table.Column<long>(type: "bigint", nullable: false),
                    PlatformFeePpm = table.Column<int>(type: "integer", nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_currency_policy_versions", x => new { x.ProductId, x.Version });
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_fee", "\"PlatformFeePpm\" BETWEEN 0 AND 999999");
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_prices", "(\"Mode\" = 1 AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" = 0) OR (\"Mode\" = 2 AND \"HardPriceUnits\" = 0 AND \"SoftPriceUnits\" > 0) OR (\"Mode\" IN (3, 4) AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" > 0)");
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformFeeWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EntitlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundHoldUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_settlements", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_settlements_hold", "\"RefundHoldUntil\" > \"SettledAt\"");
                    table.CheckConstraint("ck_economy_marketplace_settlements_state", "\"Status\" BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_economy_marketplace_settlements_version", "\"PolicyVersion\" > 0 AND \"Version\" > 0");
                    table.CheckConstraint("ck_economy_marketplace_settlements_wallets", "\"BuyerWalletId\" <> \"SellerWalletId\" AND \"BuyerWalletId\" <> \"PlatformFeeWalletId\" AND \"SellerWalletId\" <> \"PlatformFeeWalletId\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_reconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReportId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EstimatedRevenueUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    PreviousActualRevenueUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ActualRevenueUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ActualDeltaUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    VarianceUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    HistoricalRewardSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReconciledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_reconciliations", x => x.Id);
                    table.CheckConstraint("ck_economy_ad_reward_reconciliations_conservation", "\"ActualDeltaUsdNanos\" = \"ActualRevenueUsdNanos\" - \"PreviousActualRevenueUsdNanos\" AND \"VarianceUsdNanos\" = \"ActualRevenueUsdNanos\" - \"EstimatedRevenueUsdNanos\"");
                    table.CheckConstraint("ck_economy_ad_reward_reconciliations_nonnegative", "\"EstimatedRevenueUsdNanos\" >= 0 AND \"PreviousActualRevenueUsdNanos\" >= 0 AND \"ActualRevenueUsdNanos\" >= 0 AND \"HistoricalRewardSoftUnits\" >= 0");
                    table.CheckConstraint("ck_economy_ad_reward_reconciliations_version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_ad_reward_reconciliations_economy_ad_provider_repor~",
                        column: x => x.ProviderReportId,
                        principalTable: "economy_ad_provider_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_bounty_escrow_fragments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    TraceUnitsPerCoinUnit = table.Column<long>(type: "bigint", nullable: false),
                    SelectedRootRanges = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_bounty_escrow_fragments", x => x.Id);
                    table.CheckConstraint("ck_economy_bounty_escrow_fragments_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_bounty_escrow_fragments_scale_positive", "\"TraceUnitsPerCoinUnit\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_bounty_escrow_fragments_economy_bounties_BountyId",
                        column: x => x.BountyId,
                        principalTable: "economy_bounties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_bounty_terminal_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProceedsSourceStampId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProceedsLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnedUnits = table.Column<long>(type: "bigint", nullable: false),
                    FeeUnits = table.Column<long>(type: "bigint", nullable: false),
                    FirstJournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    OutputLots = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_bounty_terminal_events", x => x.Id);
                    table.CheckConstraint("ck_economy_bounty_terminal_events_claim_binding", "\"Status\" <> 3 OR (\"RiskDecisionId\" IS NOT NULL AND \"ProceedsSourceStampId\" IS NOT NULL AND \"ProceedsLotId\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_bounty_terminal_events_state", "\"Status\" IN (3, 4)");
                    table.CheckConstraint("ck_economy_bounty_terminal_events_units", "\"ReturnedUnits\" >= 0 AND \"FeeUnits\" >= 0 AND \"FirstJournalSequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_bounty_terminal_events_economy_bounties_BountyId",
                        column: x => x.BountyId,
                        principalTable: "economy_bounties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_funding_fragments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    TraceUnitsPerCoinUnit = table.Column<long>(type: "bigint", nullable: false),
                    SelectedRootRanges = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_funding_fragments", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_funding_fragments_amount", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_marketplace_funding_fragments_scale", "\"TraceUnitsPerCoinUnit\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_funding_fragments_economy_marketplace_s~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsFullRefund = table.Column<bool>(type: "boolean", nullable: false),
                    EntitlementRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    FirstJournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    RefundedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_refunds", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_refunds_sequence", "\"FirstJournalSequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refunds_economy_marketplace_settlements~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_settlement_credits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    RefundHoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundHoldUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ParentLineage = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_settlement_credits", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_settlement_credits_amount", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_marketplace_settlement_credits_purpose", "\"Purpose\" BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_settlement_credits_economy_marketplace_~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_settlement_legs",
                columns: table => new
                {
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Units = table.Column<long>(type: "bigint", nullable: false),
                    SellerUnits = table.Column<long>(type: "bigint", nullable: false),
                    PlatformFeeUnits = table.Column<long>(type: "bigint", nullable: false),
                    RefundedUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_settlement_legs", x => new { x.SettlementId, x.Currency });
                    table.CheckConstraint("ck_economy_marketplace_settlement_legs_conservation", "\"Units\" > 0 AND \"SellerUnits\" >= 0 AND \"PlatformFeeUnits\" >= 0 AND \"SellerUnits\" + \"PlatformFeeUnits\" = \"Units\"");
                    table.CheckConstraint("ck_economy_marketplace_settlement_legs_refund", "\"RefundedUnits\" BETWEEN 0 AND \"Units\"");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_settlement_legs_economy_marketplace_set~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_refund_legs",
                columns: table => new
                {
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_refund_legs", x => new { x.RefundId, x.Currency });
                    table.CheckConstraint("ck_economy_marketplace_refund_legs_amount", "\"Units\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refund_legs_economy_marketplace_refunds~",
                        column: x => x.RefundId,
                        principalTable: "economy_marketplace_refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refund_legs_economy_marketplace_settlem~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_cost_facts_AuthorizationId",
                table: "ai_provider_cost_facts",
                column: "AuthorizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_cost_facts_Provider_ProviderUsageId",
                table: "ai_provider_cost_facts",
                columns: new[] { "Provider", "ProviderUsageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_cost_facts_ServiceCode_CompletedAt",
                table: "ai_provider_cost_facts",
                columns: new[] { "ServiceCode", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_cost_facts_TenantId_CompletedAt",
                table: "ai_provider_cost_facts",
                columns: new[] { "TenantId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_network_policy_versions_Network_EffectiveAt_Expi~",
                table: "economy_ad_network_policy_versions",
                columns: new[] { "Network", "EffectiveAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_provider_reports_Network_BatchId_Version",
                table: "economy_ad_provider_reports",
                columns: new[] { "Network", "BatchId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_provider_reports_Network_ReportId_Version",
                table: "economy_ad_provider_reports",
                columns: new[] { "Network", "ReportId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_attributions_Network_ProviderBatchId_Comp~",
                table: "economy_ad_reward_attributions",
                columns: new[] { "Network", "ProviderBatchId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_budget_consumptions_DeviceRiskHash_Consum~",
                table: "economy_ad_reward_budget_consumptions",
                columns: new[] { "DeviceRiskHash", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_budget_consumptions_Network_ConsumedAt",
                table: "economy_ad_reward_budget_consumptions",
                columns: new[] { "Network", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_budget_consumptions_UserId_ConsumedAt",
                table: "economy_ad_reward_budget_consumptions",
                columns: new[] { "UserId", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_completions_IdempotencyKey",
                table: "economy_ad_reward_completions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_completions_Network_PolicyVersion",
                table: "economy_ad_reward_completions",
                columns: new[] { "Network", "PolicyVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_completions_ProviderEventId",
                table: "economy_ad_reward_completions",
                column: "ProviderEventId",
                unique: true,
                filter: "\"ProviderEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_completions_UserId_CompletedAt",
                table: "economy_ad_reward_completions",
                columns: new[] { "UserId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_reconciliations_Network_ReportId_Version",
                table: "economy_ad_reward_reconciliations",
                columns: new[] { "Network", "ReportId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_reconciliations_ProviderReportId",
                table: "economy_ad_reward_reconciliations",
                column: "ProviderReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_IdempotencyKey",
                table: "economy_bounties",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_PosterId_Status_ExpiresAt",
                table: "economy_bounties",
                columns: new[] { "PosterId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_Status_ExpiresAt",
                table: "economy_bounties",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounty_escrow_fragments_BountyId_ParentLotId",
                table: "economy_bounty_escrow_fragments",
                columns: new[] { "BountyId", "ParentLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounty_terminal_events_BountyId",
                table: "economy_bounty_terminal_events",
                column: "BountyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounty_terminal_events_IdempotencyKey",
                table: "economy_bounty_terminal_events",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_currency_policy_versions_ProductId_Effe~",
                table: "economy_marketplace_currency_policy_versions",
                columns: new[] { "ProductId", "EffectiveAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_funding_fragments_SettlementId_ParentLo~",
                table: "economy_marketplace_funding_fragments",
                columns: new[] { "SettlementId", "ParentLotId", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refund_legs_SettlementId_Currency",
                table: "economy_marketplace_refund_legs",
                columns: new[] { "SettlementId", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refunds_IdempotencyKey",
                table: "economy_marketplace_refunds",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refunds_SettlementId_RefundedAt",
                table: "economy_marketplace_refunds",
                columns: new[] { "SettlementId", "RefundedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlement_credits_CreditLotId",
                table: "economy_marketplace_settlement_credits",
                column: "CreditLotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlement_credits_SettlementId",
                table: "economy_marketplace_settlement_credits",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlement_credits_SourceStampId",
                table: "economy_marketplace_settlement_credits",
                column: "SourceStampId",
                unique: true,
                filter: "\"SourceStampId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlements_BuyerId_SettledAt",
                table: "economy_marketplace_settlements",
                columns: new[] { "BuyerId", "SettledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlements_IdempotencyKey",
                table: "economy_marketplace_settlements",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlements_OrderId",
                table: "economy_marketplace_settlements",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_settlements_SellerId_SettledAt",
                table: "economy_marketplace_settlements",
                columns: new[] { "SellerId", "SettledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_provider_cost_facts");

            migrationBuilder.DropTable(
                name: "economy_ad_network_policy_versions");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_accumulators");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_attributions");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_budget_consumptions");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_completions");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_reconciliations");

            migrationBuilder.DropTable(
                name: "economy_bounty_escrow_fragments");

            migrationBuilder.DropTable(
                name: "economy_bounty_terminal_events");

            migrationBuilder.DropTable(
                name: "economy_marketplace_currency_policy_versions");

            migrationBuilder.DropTable(
                name: "economy_marketplace_funding_fragments");

            migrationBuilder.DropTable(
                name: "economy_marketplace_refund_legs");

            migrationBuilder.DropTable(
                name: "economy_marketplace_settlement_credits");

            migrationBuilder.DropTable(
                name: "economy_marketplace_settlement_legs");

            migrationBuilder.DropTable(
                name: "economy_ad_provider_reports");

            migrationBuilder.DropTable(
                name: "economy_bounties");

            migrationBuilder.DropTable(
                name: "economy_marketplace_refunds");

            migrationBuilder.DropTable(
                name: "economy_marketplace_settlements");
        }
    }
}
