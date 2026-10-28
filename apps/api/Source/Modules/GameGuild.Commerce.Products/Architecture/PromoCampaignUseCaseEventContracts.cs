using GameGuild;
using GameGuild.Commerce.Products;

// Campaign mutations use the same durable generic operation envelope as promo-code
// mutations. Their detailed state changes are also recorded by IPromoCodeAuditLogger.
[assembly: UseCaseEventContract(typeof(CreatePromoCampaignCommand), "commerce.products.create-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(UpdatePromoCampaignCommand), "commerce.products.update-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(ActivatePromoCampaignCommand), "commerce.products.activate-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(PausePromoCampaignCommand), "commerce.products.pause-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(CompletePromoCampaignCommand), "commerce.products.complete-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(CancelPromoCampaignCommand), "commerce.products.cancel-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(ArchivePromoCampaignCommand), "commerce.products.archive-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
[assembly: UseCaseEventContract(typeof(DeletePromoCampaignCommand), "commerce.products.delete-promo-campaign",
    NoDomainEventReason = "Campaign state is read from the repository; mutation observability uses the durable generic operation event and campaign audit record.")]
