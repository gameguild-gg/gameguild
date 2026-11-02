[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.ScanSensitiveFieldsCommand),
    "compliance.audit.field-level.scan-sensitive-fields",
    NoDomainEventReason = "Classification scan over existing field access observations; the durable generic operation event records the scan.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.AnalyzeFieldAccessPatternsCommand),
    "compliance.audit.field-level.analyze-access-patterns",
    NoDomainEventReason = "Derived analysis over existing observations; anomaly findings are persisted audit records, not cross-module events.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.EvaluateBulkExportCommand),
    "compliance.audit.field-level.evaluate-bulk-export",
    NoDomainEventReason = "Threshold evaluation records its own bulk export event and security alert; no additional cross-module event is declared.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.SaveGdprActivityCommand),
    "compliance.audit.field-level.save-article30-activity",
    NoDomainEventReason = "Processing register mutation is compliance-local; the durable generic operation event records the change.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.DeleteGdprActivityCommand),
    "compliance.audit.field-level.delete-article30-activity",
    NoDomainEventReason = "Processing register mutation is compliance-local; the durable generic operation event records the change.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.SeedGdprActivitiesCommand),
    "compliance.audit.field-level.seed-article30-activities",
    NoDomainEventReason = "Seeding derives register rows from the platform catalog; the durable generic operation event records the seed.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.MarkGdprActivityReviewedCommand),
    "compliance.audit.field-level.review-article30-activity",
    NoDomainEventReason = "Review stamp on a register row; the durable generic operation event records the review.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.ValidateCrossBorderTransferCommand),
    "compliance.audit.field-level.validate-cross-border-transfer",
    NoDomainEventReason = "Validation writes its own transfer log row; the durable generic operation event records the validation.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.ConfigureSensitiveDataRetentionCommand),
    "compliance.audit.field-level.configure-sensitive-retention",
    NoDomainEventReason = "Retention policy configuration is compliance-local; the durable generic operation event records the change.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.EnforceSensitiveDataRetentionCommand),
    "compliance.audit.field-level.enforce-sensitive-retention",
    NoDomainEventReason = "Enforcement records its own execution summary; no additional cross-module event is declared.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.AcknowledgeFieldAccessAnomalyCommand),
    "compliance.audit.field-level.acknowledge-anomaly",
    NoDomainEventReason = "Investigation state transition on an audit finding; the durable generic operation event records it.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.ResolveFieldAccessAnomalyCommand),
    "compliance.audit.field-level.resolve-anomaly",
    NoDomainEventReason = "Investigation state transition on an audit finding; the durable generic operation event records it.")]
[assembly: global::GameGuild.UseCaseEventContractAttribute(
    typeof(global::GameGuild.Compliance.Audit.RecordFieldAccessCommand),
    "compliance.audit.field-level.record-field-access",
    NoDomainEventReason = "The field access observation row is itself the durable record; no cross-module event is declared.")]
