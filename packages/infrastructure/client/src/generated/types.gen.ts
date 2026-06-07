/**
 * @game-guild/client - Generated Types and Zod Schemas
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 *
 * Generated from: GameGuild API
 * API Version: 1.0
 */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { z } from 'zod';

export type BillingCycle = 'Weekly' | 'Monthly' | 'Quarterly' | 'SemiAnnually' | 'Annually' | 'Biannually';

export interface BulkOperationError {
  tenantId?: string;
  tenantName?: string | null;
  errorMessage?: string | null;
  errorCode?: string | null;
}

export interface CommercePaymentsTaxJurisdictionDto {
  id?: string;
  code?: string | null;
  name?: string | null;
  country?: string | null;
  state?: string | null;
  taxType?: string | null;
  defaultRate?: number;
  isActive?: boolean;
}

export interface CommercePaymentsTaxRuleDto {
  id?: string;
  jurisdictionCode?: string | null;
  productCategory?: string | null;
  customerType?: string | null;
  rate?: number;
  effectiveFrom?: string;
  effectiveTo?: string | null;
  description?: string | null;
  isActive?: boolean;
}

export type ContentStatus = 'Draft' | 'Review' | 'Published' | 'Archived' | 'Deleted';

export type ContentVisibility = 'Private' | 'Internal' | 'Friends' | 'Protected' | 'Public';

export interface IdentityTenantsTenantSettingsDto {
  id?: string;
  systemConfiguration?: IdentityTenantsTenantSystemConfiguration;
  featureFlags?: Record<string, boolean> | null;
  businessRules?: IdentityTenantsTenantBusinessRules;
  userInterfaceSettings?: IdentityTenantsTenantUiSettings;
  securitySettings?: IdentityTenantsTenantSecuritySettings;
  integrationSettings?: IdentityTenantsTenantIntegrationSettings;
  systemLimits?: IdentityTenantsTenantSystemLimits;
  createdAt?: string;
  updatedAt?: string;
}

export interface KeyValuePairStringAuthenticationExtensionsPRFValues {
  key?: string | null;
  value?: ObjectsAuthenticationExtensionsPRFValues;
}

export interface Money {
  amount?: number;
  currency?: string | null;
}

export interface PagedResult1GameGuildCommerceProductsPromoCodeDtoGameGuildCommerceProductsVersion100PagedResultPromoCodeDto {
  items?: Array<CommerceProductsPromoCode> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface PagedResult1GameGuildIdentityTenantsTenantGameGuildIdentityTenantsVersion100PagedResultTenant {
  items?: Array<IdentityTenantsTenant> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface PagedResult1GameGuildIdentityTenantsTenantAuditLogEntryGameGuildIdentityTenantsVersion100PagedResultTenantAuditLogEntry {
  items?: Array<IdentityTenantsTenantAuditLogEntry> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface PagedResult1GameGuildIdentityUsersUserDtoGameGuildIdentityUsersVersion100PagedResultUserDto {
  items?: Array<IdentityUsersUser> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface PagedResult1GameGuildIdentityUsersUserNotificationDtoGameGuildIdentityUsersVersion100PagedResultUserNotificationDto {
  items?: Array<IdentityUsersUserNotification> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface PagedResult1GameGuildIdentityUsersUserProfileDtoGameGuildIdentityUsersVersion100PagedResultUserProfileDto {
  items?: Array<IdentityUsersUserProfile> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export type ProgramCategory =
  | 'General'
  | 'Programming'
  | 'DataScience'
  | 'WebDevelopment'
  | 'MobileDevelopment'
  | 'GameDevelopment'
  | 'AI'
  | 'Cybersecurity'
  | 'DevOps'
  | 'Database'
  | 'Business'
  | 'Design'
  | 'Marketing'
  | 'ProjectManagement'
  | 'PersonalDevelopment'
  | 'CreativeArts'
  | 'Science'
  | 'Language'
  | 'Other';

export interface TenantInfo {
  id?: string;
  name?: string | null;
  slug?: string | null;
  isActive?: boolean;
}

export interface AIAiChatMessage {
  role?: string | null;
  content?: string | null;
}

export interface AIAiChatInput {
  provider?: string | null;
  model?: string | null;
  systemPrompt?: string | null;
  messages?: Array<AIAiChatMessage> | null;
  temperature?: number | null;
  maxTokens?: number | null;
}

export interface AIAiCompletionOutput {
  provider?: string | null;
  model?: string | null;
  text?: string | null;
  finishReason?: string | null;
  usage?: AIAiUsage;
}

export interface AIAiConversationHistoryEntry {
  id?: string;
  userId?: string | null;
  requestKind?: string | null;
  provider?: string | null;
  model?: string | null;
  requestText?: string | null;
  systemPrompt?: string | null;
  responseText?: string | null;
  outcome?: string | null;
  outcomeCode?: string | null;
  outcomeReason?: string | null;
  finishReason?: string | null;
  usage?: AIAiUsage;
  occurredAt?: string;
}

export interface AIAiGenerateInput {
  provider?: string | null;
  model?: string | null;
  systemPrompt?: string | null;
  prompt?: string | null;
  temperature?: number | null;
  maxTokens?: number | null;
}

export interface AIAiGeneratedContentDraftInput {
  subject?: string | null;
  context?: string | null;
  audience?: string | null;
  tone?: string | null;
  provider?: string | null;
  model?: string | null;
  maxTokens?: number | null;
}

export type AIAiGeneratedContentKind = 'Email' | 'Report' | 'ListingDescription';

export interface AIAiGeneratedContentInput {
  kind?: AIAiGeneratedContentKind;
  subject?: string | null;
  context?: string | null;
  audience?: string | null;
  tone?: string | null;
  provider?: string | null;
  model?: string | null;
  maxTokens?: number | null;
}

export interface AIAiPromptTemplate {
  id?: string;
  tenantId?: string | null;
  key?: string | null;
  name?: string | null;
  description?: string | null;
  category?: string | null;
  systemPrompt?: string | null;
  prompt?: string | null;
  isActive?: boolean;
  isSystemTemplate?: boolean;
  createdByUserId?: string | null;
  updatedByUserId?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface AIAiPromptTemplateGenerateInput {
  variables?: Record<string, string | null> | null;
  provider?: string | null;
  model?: string | null;
  temperature?: number | null;
  maxTokens?: number | null;
}

export interface AIAiPromptTemplateRenderInput {
  variables?: Record<string, string | null> | null;
}

export interface AIAiPromptTemplateRenderOutput {
  templateId?: string;
  key?: string | null;
  systemPrompt?: string | null;
  prompt?: string | null;
  variables?: Record<string, string | null> | null;
}

export interface AIAiProviderStatus {
  provider?: string | null;
  configured?: boolean;
  defaultModel?: string | null;
  baseUrl?: string | null;
  credentialsConfigured?: boolean;
}

export interface AIAiQuotaStatus {
  resourceType?: string | null;
  currentUsage?: number;
  softLimit?: number | null;
  hardLimit?: number | null;
  remaining?: number;
  usagePercent?: number;
  period?: string | null;
  isActive?: boolean;
  lastReset?: string | null;
  nextReset?: string | null;
}

export interface AIAiQuotaStatusOutput {
  tenantId?: string;
  quotas?: Array<AIAiQuotaStatus> | null;
  generatedAtUtc?: string;
}

export interface AIAiStatusOutput {
  enabled?: boolean;
  defaultProvider?: string | null;
  allowTenantOverrides?: boolean;
  providers?: Array<AIAiProviderStatus> | null;
}

export interface AIAiUsage {
  inputTokens?: number | null;
  outputTokens?: number | null;
  totalTokens?: number | null;
}

export interface AICreateAiPromptTemplateInput {
  key?: string | null;
  name?: string | null;
  prompt?: string | null;
  description?: string | null;
  category?: string | null;
  systemPrompt?: string | null;
  isActive?: boolean | null;
}

export interface AIUpdateAiPromptTemplateInput {
  name?: string | null;
  prompt?: string | null;
  description?: string | null;
  category?: string | null;
  systemPrompt?: string | null;
  isActive?: boolean | null;
}

export interface APIControllersApplicationDetails {
  name?: string | null;
  version?: string | null;
  informationalVersion?: string | null;
  description?: string | null;
}

export interface APIControllersApplicationInfoOutput {
  application?: APIControllersApplicationDetails;
  build?: APIControllersBuildDetails;
  runtime?: APIControllersRuntimeDetails;
  process?: APIControllersProcessDetails;
  timestamp?: string;
}

export interface APIControllersBuildDetails {
  timestamp?: string | null;
  configuration?: string | null;
  framework?: string | null;
}

export interface APIControllersDependencyHealthItem {
  name?: string | null;
  status?: string | null;
  duration?: string;
  description?: string | null;
  isHealthy?: boolean;
  tags?: Array<string> | null;
  data?: Record<string, string> | null;
  exception?: string | null;
}

export interface APIControllersDependencyHealthOutput {
  status?: string | null;
  totalDuration?: string;
  timestamp?: string;
  healthyCount?: number;
  unhealthyCount?: number;
  dependencies?: Array<APIControllersDependencyHealthItem> | null;
  error?: string | null;
}

export interface APIControllersHealthinessOutput {
  status?: string | null;
  duration?: string;
  timestamp?: string;
  checks?: Record<string, APIControllersHealthinessResponseItem> | null;
  error?: string | null;
}

export interface APIControllersHealthinessResponseItem {
  status?: string | null;
  duration?: string;
  description?: string | null;
  data?: Record<string, Record<string, unknown>> | null;
}

export interface APIControllersLivenessOutput {
  status?: string | null;
  alive?: boolean;
  timestamp?: string;
  uptime?: string;
  version?: string | null;
}

export interface APIControllersProcessDetails {
  startTime?: string;
  uptime?: string;
}

export interface APIControllersReadinessOutput {
  status?: string | null;
  ready?: boolean;
  timestamp?: string;
  services?: Record<string, boolean> | null;
  error?: string | null;
}

export interface APIControllersRuntimeDetails {
  dotNetVersion?: string | null;
  osDescription?: string | null;
  osArchitecture?: string | null;
  processArchitecture?: string | null;
}

export interface BulkOperationOutput {
  totalRequested?: number;
  successfulOperations?: number;
  failedOperations?: number;
  errors?: Array<BulkOperationError> | null;
  isComplete?: boolean;
  successRate?: number;
}

export interface CQRSIDomainEvent {
  eventId?: string;
  occurredAt?: string;
  version?: number;
}

export interface CommerceBillingInvoicePaymentRetryResult {
  invoiceId?: string;
  invoiceNumber?: string | null;
  invoiceStatus?: CommerceBillingInvoiceStatus;
  accepted?: boolean;
  code?: string | null;
  message?: string | null;
  retryScheduledAt?: string | null;
}

export type CommerceBillingInvoiceStatus = 'Draft' | 'Open' | 'Paid' | 'Void' | 'PastDue' | 'Uncollectible';

export interface CommercePaymentsAddFundsInput {
  userId: string;
  amount: number;
  description: string | null;
  referenceId?: string | null;
}

export interface CommercePaymentsCalculateTaxInput {
  jurisdictionCode: string | null;
  amount: number;
  currency: string | null;
  customerType: string | null;
  productCategory?: string | null;
  customerVatNumber?: string | null;
  isTaxInclusive?: boolean;
  transactionDate?: string | null;
  applicableExemptions?: Array<string> | null;
}

export interface CommercePaymentsCreateTaxJurisdictionInput {
  code?: string | null;
  name?: string | null;
  country?: string | null;
  state?: string | null;
  taxType?: string | null;
  defaultRate?: number;
}

export interface CommercePaymentsCreateTaxRuleInput {
  jurisdictionCode?: string | null;
  productCategory?: string | null;
  customerType?: string | null;
  rate?: number;
  effectiveFrom?: string;
  effectiveTo?: string | null;
  description?: string | null;
}

export interface CommercePaymentsCreateWalletInput {
  userId: string;
  currency?: string | null;
}

export type CommercePaymentsCustomerType = 'B2C' | 'B2B';

export interface CommercePaymentsDeductFundsInput {
  userId: string;
  amount: number;
  description: string | null;
  referenceId?: string | null;
}

export interface CommercePaymentsLockWalletInput {
  reason: string | null;
}

export interface CommercePaymentsModelsFreezeWalletInput {
  reason?: string | null;
}

export interface CommercePaymentsModelsPatchWalletInput {
  currency?: string | null;
  dailyLimit?: number | null;
  monthlyLimit?: number | null;
}

export interface CommercePaymentsPatchTaxJurisdictionInput {
  name?: string | null;
  taxType?: string | null;
  defaultRate?: number | null;
  isActive?: boolean | null;
}

export interface CommercePaymentsPatchTaxRuleInput {
  rate?: number | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  description?: string | null;
  isActive?: boolean | null;
}

export interface CommercePaymentsPaymentCancellationResult {
  paymentId: string;
  cancellationReason: string | null;
  canceledAt: string;
  canceledBy?: string | null;
  success: boolean;
  errorMessage?: string | null;
  refundProcessed?: boolean;
  refundAmount?: number | null;
}

export interface CommercePaymentsPaymentResult {
  success?: boolean;
  transactionId?: string | null;
  paymentId?: string | null;
  amount?: Money;
  processedAt?: string | null;
  failureReason?: string | null;
  paymentMethodId?: string | null;
  status?: CommercePaymentsPaymentStatus;
  invoiceId?: string | null;
}

export interface CommercePaymentsPaymentRetryResult {
  success?: boolean;
  retryAttempt?: number;
  nextRetryAt?: string | null;
  paymentResult?: CommercePaymentsPaymentResult;
  maxRetriesReached?: boolean;
  failureReason?: string | null;
}

export type CommercePaymentsPaymentStatus = 'Pending' | 'Processing' | 'Succeeded' | 'Failed' | 'Cancelled' | 'RequiresAction' | 'Refunded' | 'Disputed';

export interface CommercePaymentsPaymentsControllerCancelPaymentInput {
  cancellationReason?: string | null;
  canceledBy?: string | null;
  notes?: string | null;
}

export interface CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInput {
  tenantId?: string;
  subscriptionId?: string;
  paymentMethodId?: string | null;
}

export interface CommercePaymentsPaymentsControllerCreateSetupIntentInput {
  tenantId?: string;
  subscriptionId?: string;
  customerEmail?: string | null;
  customerName?: string | null;
}

export interface CommercePaymentsPaymentsControllerCreateSetupIntentOutput {
  subscriptionId?: string;
  customerId?: string | null;
  setupIntentId?: string | null;
  clientSecret?: string | null;
}

export interface CommercePaymentsPaymentsControllerProcessPaymentInput {
  tenantId?: string;
  subscriptionId?: string;
  amount?: number;
  paymentMethodId?: string | null;
}

export interface CommercePaymentsPaymentsControllerRefundInput {
  amount?: number | null;
  reason?: string | null;
}

export interface CommercePaymentsProcessRefundResult {
  refundId: string;
  paymentId: string;
  refundedAmount: number;
  currency: string | null;
  status: CommercePaymentsTransactionStatus;
  reason: string | null;
  processedAt: string;
  referenceNumber?: string | null;
  estimatedCompletionDate?: string | null;
  processingFee?: number;
  isSuccess?: boolean;
  errorMessage?: string | null;
  isSuccessful?: boolean;
}

export interface CommercePaymentsTaxBreakdown {
  taxType?: CommercePaymentsTaxType;
  description?: string | null;
  rate?: number;
  taxableAmount?: number;
  taxAmount?: number;
  jurisdictionCode?: string | null;
}

export interface CommercePaymentsTaxCalculationResult {
  subtotalAmount?: number;
  taxAmount?: number;
  totalAmount?: number;
  effectiveTaxRate?: number;
  jurisdictionCode?: string | null;
  jurisdictionName?: string | null;
  taxType?: CommercePaymentsTaxType;
  taxDescription?: string | null;
  isTaxExempt?: boolean;
  isReverseCharge?: boolean;
  taxBreakdowns?: Array<CommercePaymentsTaxBreakdown> | null;
  exemptionReason?: string | null;
}

export interface CommercePaymentsTaxExemptionValidationResult {
  isValid?: boolean;
  exemptionType?: string | null;
  exemptionRate?: number;
  validFrom?: string | null;
  validTo?: string | null;
  validationMessage?: string | null;
  warnings?: Array<string> | null;
}

export interface CommercePaymentsTaxJurisdiction {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  code: string;
  name: string;
  type?: CommercePaymentsTaxJurisdictionType;
  parentJurisdictionId?: string | null;
  parentJurisdiction?: CommercePaymentsTaxJurisdiction;
  childJurisdictions?: Array<CommercePaymentsTaxJurisdiction> | null;
  isActive?: boolean;
  taxRegistrationNumber?: string | null;
  isReverseChargeApplicable?: boolean;
  taxRules?: Array<CommercePaymentsTaxRule> | null;
}

export type CommercePaymentsTaxJurisdictionType = 'Country' | 'State' | 'Province' | 'Region' | 'City' | 'County' | 'District';

export interface CommercePaymentsTaxRate {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  taxJurisdictionId: string;
  taxJurisdiction?: CommercePaymentsTaxJurisdiction;
  taxType?: CommercePaymentsTaxType;
  rate?: number;
  productCategory?: string | null;
  effectiveFrom?: string;
  effectiveTo?: string | null;
  isActive?: boolean;
  minimumTaxableAmount?: number | null;
  maximumTaxableAmount?: number | null;
  description?: string | null;
}

export interface CommercePaymentsTaxRule {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  name: string;
  description?: string | null;
  taxJurisdictionId: string;
  taxJurisdiction?: CommercePaymentsTaxJurisdiction;
  ruleType?: CommercePaymentsTaxRuleType;
  priority?: number;
  isActive?: boolean;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  customerTypeFilter?: CommercePaymentsCustomerType;
  productCategories?: string | null;
  minimumAmount?: number | null;
  maximumAmount?: number | null;
  isTaxInclusive?: boolean;
  isReverseCharge?: boolean;
  exemptionConditions?: string | null;
  defaultTaxRateId?: string | null;
  defaultTaxRate?: CommercePaymentsTaxRate;
}

export type CommercePaymentsTaxRuleType = 'Standard' | 'Reduced' | 'ZeroRated' | 'Exempt' | 'ReverseCharge' | 'WithholdingTax' | 'Compound' | 'Custom';

export type CommercePaymentsTaxType = 'VAT' | 'GST' | 'SalesTax' | 'ServiceTax' | 'WithholdingTax' | 'ExciseTax' | 'CustomsDuty' | 'Other';

export type CommercePaymentsTransactionStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled' | 'Reversed';

export interface CommercePaymentsTransferFundsInput {
  fromUserId: string;
  toUserId: string;
  amount: number;
  description: string | null;
  referenceId?: string | null;
}

export interface CommercePaymentsTransferResult {
  debitTransaction?: CommercePaymentsWalletTransaction;
  creditTransaction?: CommercePaymentsWalletTransaction;
}

export interface CommercePaymentsUserWallet {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  userId: string;
  balance?: number;
  currency: string;
  isActive?: boolean;
  isLocked?: boolean;
  lockReason?: string | null;
  lastTransactionAt?: string | null;
  dailyLimit?: number | null;
  monthlyLimit?: number | null;
  transactions?: Array<CommercePaymentsWalletTransaction> | null;
}

export interface CommercePaymentsValidateTaxExemptionInput {
  jurisdictionCode?: string | null;
  exemptionType?: string | null;
  exemptionCertificateNumber?: string | null;
  customerVatNumber?: string | null;
  customerId?: string | null;
  transactionDate?: string | null;
}

export interface CommercePaymentsWalletTransaction {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  walletId: string;
  wallet?: CommercePaymentsUserWallet;
  type?: CommercePaymentsWalletTransactionType;
  amount?: number;
  balanceAfter?: number;
  description: string;
  referenceId?: string | null;
  status?: CommercePaymentsTransactionStatus;
  metadata?: string | null;
  notes?: string | null;
  processedAt?: string | null;
}

export type CommercePaymentsWalletTransactionType = 'Credit' | 'Debit' | 'TransferIn' | 'TransferOut' | 'Refund' | 'Fee' | 'Adjustment';

export interface CommerceProductsAppliedPromoCode {
  code?: string | null;
  discountAmount?: number;
  discountPercentage?: number | null;
}

export interface CommerceProductsApplyPromoCodesInput {
  orderAmount?: number;
  promoCodes?: Array<string> | null;
  productId?: string | null;
}

export interface CommerceProductsBatchCreateProductsInput {
  products?: Array<CommerceProductsBatchProductCreateItem> | null;
  tenantId?: string | null;
}

export interface CommerceProductsBatchProductCreateItem {
  name?: string | null;
  description?: string | null;
  shortDescription?: string | null;
  imageUrl?: string | null;
  type?: CommerceProductsProductType;
  isBundle?: boolean;
  creatorId?: string | null;
  bundleItems?: Array<string> | null;
  referralCommissionPercentage?: number;
  maxAffiliateDiscount?: number;
  affiliateCommissionPercentage?: number;
}

export interface CommerceProductsCheckMultipleAccessInput {
  productIds?: Array<string> | null;
}

export interface CommerceProductsCreateProductInput {
  name?: string | null;
  description?: string | null;
  shortDescription?: string | null;
  imageUrl?: string | null;
  type?: CommerceProductsProductType;
  isBundle?: boolean;
  creatorId?: string | null;
  bundleItems?: Array<string> | null;
  referralCommissionPercentage?: number;
  maxAffiliateDiscount?: number;
  affiliateCommissionPercentage?: number;
  tenantId?: string | null;
}

export interface CommerceProductsCreatePromoCodeInput {
  code?: string | null;
  name?: string | null;
  description?: string | null;
  type?: CommerceProductsPromoCodeType;
  discountPercentage?: number | null;
  discountAmount?: number | null;
  currency?: string | null;
  minimumOrderAmount?: number | null;
  maxUses?: number | null;
  maxUsesPerUser?: number | null;
  validFrom?: string | null;
  validUntil?: string | null;
  isActive?: boolean;
  isExclusive?: boolean;
  stackingPriority?: number;
  productId?: string | null;
}

export interface CommerceProductsEntitlementCheckResult {
  productId?: string;
  hasAccess?: boolean;
}

export interface CommerceProductsEntitlementInfo {
  productId?: string;
  productName?: string | null;
  status?: string | null;
  acquisitionType?: string | null;
  accessStartDate?: string | null;
  accessEndDate?: string | null;
  isSubscription?: boolean;
  subscriptionStatus?: string | null;
  pricePaid?: number;
  currency?: string | null;
}

export interface CommerceProductsGrantEntitlementInput {
  userId?: string;
  productId?: string;
  acquisitionType?: CommerceProductsProductAcquisitionType;
  pricePaid?: number;
  currency?: string | null;
  expiresAt?: string | null;
}

export interface CommerceProductsPatchProductInput {
  name?: string | null;
  description?: string | null;
  shortDescription?: string | null;
  imageUrl?: string | null;
  type?: CommerceProductsProductType;
  isBundle?: boolean | null;
  bundleItems?: Array<string> | null;
  referralCommissionPercentage?: number | null;
  maxAffiliateDiscount?: number | null;
  affiliateCommissionPercentage?: number | null;
  expectedVersion?: number | null;
}

export interface CommerceProductsPatchPromoCodeInput {
  name?: string | null;
  description?: string | null;
  type?: CommerceProductsPromoCodeType;
  discountPercentage?: number | null;
  discountAmount?: number | null;
  currency?: string | null;
  minimumOrderAmount?: number | null;
  maxUses?: number | null;
  maxUsesPerUser?: number | null;
  validFrom?: string | null;
  validUntil?: string | null;
  isActive?: boolean | null;
  isExclusive?: boolean | null;
  stackingPriority?: number | null;
  productId?: string | null;
}

export type CommerceProductsProductAcquisitionType = 'Purchase' | 'Subscription' | 'Grant' | 'PromoCode' | 'Bundle' | 'Trial' | 'Referral' | 'Free' | 'Gift';

export interface CommerceProductsProduct {
  id?: string;
  name?: string | null;
  description?: string | null;
  shortDescription?: string | null;
  imageUrl?: string | null;
  type?: CommerceProductsProductType;
  isBundle?: boolean;
  isPublished?: boolean;
  creatorId?: string | null;
  bundleItems?: Array<string> | null;
  referralCommissionPercentage?: number;
  maxAffiliateDiscount?: number;
  affiliateCommissionPercentage?: number;
  createdAt?: string;
  updatedAt?: string;
  pricing?: Array<CommerceProductsProductPricing> | null;
}

export interface CommerceProductsProductPricing {
  id?: string;
  productId?: string;
  name?: string | null;
  basePrice?: number;
  salePrice?: number | null;
  currency?: string | null;
  saleStartDate?: string | null;
  saleEndDate?: string | null;
  isDefault?: boolean;
  currentPrice?: number;
  isSaleActive?: boolean;
}

export type CommerceProductsProductType =
  | 'Program'
  | 'Course'
  | 'Bundle'
  | 'Subscription'
  | 'Workshop'
  | 'Mentorship'
  | 'Ebook'
  | 'ResourcePack'
  | 'Community'
  | 'Certification'
  | 'Physical'
  | 'Service'
  | 'LearningPathway'
  | 'Other';

export interface CommerceProductsPromoCodeApplicationResult {
  originalAmount?: number;
  finalAmount?: number;
  totalDiscount?: number;
  appliedCodes?: Array<CommerceProductsAppliedPromoCode> | null;
  rejectedCodes?: Array<CommerceProductsRejectedPromoCode> | null;
}

export interface CommerceProductsPromoCode {
  id?: string;
  code?: string | null;
  name?: string | null;
  description?: string | null;
  type?: CommerceProductsPromoCodeType;
  discountPercentage?: number | null;
  discountAmount?: number | null;
  currency?: string | null;
  minimumOrderAmount?: number | null;
  maxUses?: number | null;
  maxUsesPerUser?: number | null;
  validFrom?: string | null;
  validUntil?: string | null;
  isActive?: boolean;
  isExclusive?: boolean;
  stackingPriority?: number;
  productId?: string | null;
  usageCount?: number;
  createdAt?: string;
  updatedAt?: string;
}

export type CommerceProductsPromoCodeType = 'PercentageOff' | 'FixedAmountOff' | 'FreeTrial' | 'BuyOneGetOne' | 'FreeShipping';

export interface CommerceProductsPromoCodeUsage {
  promoCodeId?: string;
  code?: string | null;
  totalUses?: number;
  uniqueUsers?: number;
  totalDiscountGiven?: number;
  averageDiscountPerUse?: number;
  maxUses?: number | null;
  remainingUses?: number | null;
  firstUsedAt?: string | null;
  lastUsedAt?: string | null;
}

export interface CommerceProductsPromoCodeValidationResult {
  isValid?: boolean;
  code?: string | null;
  errorMessage?: string | null;
  discountAmount?: number;
  discountPercentage?: number | null;
}

export interface CommerceProductsRejectedPromoCode {
  code?: string | null;
  reason?: string | null;
}

export interface CommerceProductsRevokeEntitlementInput {
  userId?: string;
  productId?: string;
  reason?: string | null;
}

export interface CommerceProductsUpdateProductInput {
  name?: string | null;
  description?: string | null;
  shortDescription?: string | null;
  imageUrl?: string | null;
  type?: CommerceProductsProductType;
  isBundle?: boolean | null;
  bundleItems?: Array<string> | null;
  referralCommissionPercentage?: number | null;
  maxAffiliateDiscount?: number | null;
  affiliateCommissionPercentage?: number | null;
  expectedVersion?: number | null;
}

export interface CommerceProductsUpdatePromoCodeInput {
  name?: string | null;
  description?: string | null;
  type?: CommerceProductsPromoCodeType;
  discountPercentage?: number | null;
  discountAmount?: number | null;
  currency?: string | null;
  minimumOrderAmount?: number | null;
  maxUses?: number | null;
  maxUsesPerUser?: number | null;
  validFrom?: string | null;
  validUntil?: string | null;
  isActive?: boolean | null;
  isExclusive?: boolean | null;
  stackingPriority?: number | null;
  productId?: string | null;
}

export interface CommerceProductsValidatePromoCodeInput {
  code?: string | null;
  orderAmount?: number;
  productId?: string | null;
}

export interface CommerceSubscriptionsBillingHistory {
  id?: string;
  subscriptionId?: string;
  billingDate?: string;
  amount?: number;
  currency?: string | null;
  status?: string | null;
  externalPaymentId?: string | null;
  description?: string | null;
  createdAt?: string;
}

export type CommerceSubscriptionsCancellationReason =
  | 'UserRequested'
  | 'PaymentFailed'
  | 'PlanDiscontinued'
  | 'PolicyViolation'
  | 'Downgrade'
  | 'TrialEnded'
  | 'Custom'
  | 'ExternalRequest';

export interface CommerceSubscriptionsSubscription {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  createdByUserId: string;
  fulfilledOrderId?: string | null;
  lastModifyingOrderId?: string | null;
  lastRenewalIdempotencyKey?: string | null;
  lastPaymentIdempotencyKey?: string | null;
  lockedPriceVersionId?: string | null;
  lastProcessedBillingCycle?: number;
  trialEndDate?: string | null;
  cancellationReason?: CommerceSubscriptionsCancellationReason;
  cancellationNote?: string | null;
  cancelledAt?: string | null;
  externalId?: string | null;
  externalCustomerId?: string | null;
  autoRenew?: boolean;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  billingCycleCount?: number;
  lastPaymentAt?: string | null;
  metadata?: string | null;
  rowVersion?: string | null;
  plan?: CommerceSubscriptionsSubscriptionPlan;
  status?: CommerceSubscriptionsSubscriptionStatus;
  planId: string;
  billingCycle?: BillingCycle;
  amount?: Money;
  startDate?: string;
  endDate?: string | null;
  nextBillingDate?: string;
  isActive?: boolean;
  isTrialing?: boolean;
  isCancelled?: boolean;
}

export interface CommerceSubscriptionsSubscriptionDowngradeResult {
  success?: boolean;
  updatedSubscription?: CommerceSubscriptionsSubscription;
  effectiveDate?: string | null;
  creditIssued?: Money;
  failureReason?: string | null;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerAutoRenewInput {
  autoRenew?: boolean;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerCancelInput {
  reason?: string | null;
  note?: string | null;
  effectiveDate?: string | null;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerDowngradeInput {
  newPlanId?: string;
  effectiveDate?: string | null;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerEndTrialInput {
  convertToPaid?: boolean;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerExternalIdsInput {
  externalSubscriptionId?: string | null;
  externalCustomerId?: string | null;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerPauseSubscriptionInput {
  pauseUntil?: string | null;
  reason?: string | null;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerStartTrialInput {
  trialDays?: number;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerSuspendInput {
  reason?: string | null;
}

export interface CommerceSubscriptionsSubscriptionLifecycleControllerUpgradeInput {
  newPlanId?: string;
  effectiveDate?: string | null;
}

export interface CommerceSubscriptionsSubscriptionPlan {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  externalId?: string | null;
  isFeatured?: boolean;
  sortOrder?: number;
  hasPrioritySupport?: boolean;
  hasAdvancedAnalytics?: boolean;
  hasCustomBranding?: boolean;
  features?: string | null;
  metadata?: string | null;
  trialPeriodDays?: number;
  subscriptions?: Array<CommerceSubscriptionsSubscription> | null;
  name: string;
  slug: string;
  description?: string | null;
  monthlyPriceInCents?: number;
  annualPriceInCents?: number | null;
  currency: string;
  isActive?: boolean;
  maxUsers?: number | null;
  maxStorageMb?: number | null;
  maxApiCallsPerMonth?: number | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerCloneSubscriptionPlanInput {
  newName?: string | null;
  newSlug?: string | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerSetExternalIdInput {
  externalId?: string | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerSetFeaturedInput {
  featured?: boolean;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateDetailsInput {
  planId?: string;
  name?: string | null;
  description?: string | null;
  sortOrder?: number | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateFeaturesInput {
  hasPrioritySupport?: boolean | null;
  hasAdvancedAnalytics?: boolean | null;
  hasCustomBranding?: boolean | null;
  features?: string | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateLimitsInput {
  maxUsers?: number | null;
  maxStorageMb?: number | null;
  maxApiCallsPerMonth?: number | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdatePricingInput {
  monthlyPriceInCents?: number;
  annualPriceInCents?: number | null;
}

export interface CommerceSubscriptionsSubscriptionPlanOperationsControllerValidateLimitsInput {
  users?: number;
  storageMb?: number;
  apiCalls?: number;
}

export interface CommerceSubscriptionsSubscriptionPlansCrudControllerComparePlansInput {
  basePlanId?: string;
  comparePlanIds?: Array<string> | null;
}

export interface CommerceSubscriptionsSubscriptionPlansCrudControllerCreatePlanInput {
  name?: string | null;
  slug?: string | null;
  monthlyPriceInCents?: number;
  currency?: string | null;
  description?: string | null;
}

export interface CommerceSubscriptionsSubscriptionPlansCrudControllerPutSubscriptionPlanInput {
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  monthlyPriceInCents?: number;
  annualPriceInCents?: number | null;
  maxUsers?: number | null;
  maxStorageMb?: number | null;
  maxApiCallsPerMonth?: number | null;
  hasPrioritySupport?: boolean | null;
  hasAdvancedAnalytics?: boolean | null;
  hasCustomBranding?: boolean | null;
  features?: string | null;
  sortOrder?: number | null;
}

export type CommerceSubscriptionsSubscriptionStatus = 'PendingActivation' | 'Active' | 'Trialing' | 'PastDue' | 'Suspended' | 'Cancelled' | 'Expired';

export interface CommerceSubscriptionsSubscriptionUpgradeResult {
  success?: boolean;
  updatedSubscription?: CommerceSubscriptionsSubscription;
  proratedAmount?: Money;
  creditApplied?: Money;
  failureReason?: string | null;
}

export interface CommerceSubscriptionsSubscriptionUsage {
  subscriptionId?: string;
  usersCount?: number;
  maxUsers?: number | null;
  storageUsedMb?: number;
  maxStorageMb?: number | null;
  apiCallsThisMonth?: number;
  maxApiCallsPerMonth?: number | null;
  isOverLimit?: boolean;
  limitWarnings?: Array<string> | null;
}

export interface CommerceSubscriptionsSubscriptionsControllerCreateSubscriptionInput {
  tenantId?: string;
  planId?: string;
  createdByUserId?: string;
  billingCycle?: BillingCycle;
  amount?: number;
  currency?: string | null;
  fulfilledOrderId?: string | null;
  startDate?: string | null;
  trialDays?: number | null;
}

export interface CommerceSubscriptionsSubscriptionsControllerPatchSubscriptionInput {
  billingCycle?: BillingCycle;
  autoRenew?: boolean | null;
  externalSubscriptionId?: string | null;
  externalCustomerId?: string | null;
  metadata?: string | null;
}

export interface CommerceSubscriptionsSubscriptionsControllerPutSubscriptionInput {
  planId?: string;
  billingCycle?: BillingCycle;
  amount?: number;
  autoRenew?: boolean;
  externalSubscriptionId?: string | null;
  externalCustomerId?: string | null;
}

export interface ComplianceFERPACompleteFerpaInspectionRequestBody {
  processedByUserId?: string;
  approved?: boolean;
  notes?: string | null;
}

export type ComplianceFERPAEducationRecordKind =
  | 'CourseEnrollment'
  | 'AssessmentSubmission'
  | 'Grade'
  | 'Certificate'
  | 'Attendance'
  | 'Communication'
  | 'SupportCase'
  | 'Custom';

export interface ComplianceFERPAFerpaDirectoryInformationPolicy {
  id?: string;
  tenantId?: string | null;
  allowedFieldsJson?: string | null;
  optOutEnabled?: boolean;
  annualNoticeSentAt?: string | null;
  noticeUrl?: string | null;
}

export type ComplianceFERPAFerpaDisclosureBasis =
  | 'StudentConsent'
  | 'GuardianConsent'
  | 'SchoolOfficial'
  | 'FinancialAid'
  | 'HealthOrSafetyEmergency'
  | 'AuditOrEvaluation'
  | 'CourtOrder'
  | 'DirectoryInformation'
  | 'Other';

export interface ComplianceFERPAFerpaDisclosureConsent {
  id?: string;
  studentUserId?: string;
  guardianUserId?: string | null;
  recipient?: string | null;
  purpose?: string | null;
  scope?: string | null;
  effectiveFrom?: string;
  expiresAt?: string | null;
  revokedAt?: string | null;
  isActive?: boolean;
}

export interface ComplianceFERPAFerpaDisclosureLog {
  id?: string;
  studentUserId?: string;
  disclosedByUserId?: string;
  recipient?: string | null;
  basis?: ComplianceFERPAFerpaDisclosureBasis;
  purpose?: string | null;
  recordIdsJson?: string | null;
  disclosedAt?: string;
}

export interface ComplianceFERPAFerpaEducationRecord {
  id?: string;
  studentUserId?: string;
  recordKind?: ComplianceFERPAEducationRecordKind;
  externalRecordId?: string | null;
  title?: string | null;
  protectionLevel?: ComplianceFERPAFerpaRecordProtectionLevel;
  isDirectoryInformation?: boolean;
  retentionUntil?: string | null;
  metadataJson?: string | null;
  createdAt?: string;
}

export interface ComplianceFERPAFerpaInspectionInput {
  id?: string;
  studentUserId?: string;
  requestedByUserId?: string;
  status?: ComplianceFERPAFerpaRequestStatus;
  deadline?: string;
  processedByUserId?: string | null;
  processedAt?: string | null;
  processingNotes?: string | null;
}

export type ComplianceFERPAFerpaRecordProtectionLevel = 'DirectoryInformation' | 'EducationRecord' | 'SensitiveEducationRecord' | 'Restricted';

export type ComplianceFERPAFerpaRequestStatus = 'Pending' | 'InReview' | 'Completed' | 'Denied' | 'Expired';

export interface ComplianceFERPAGrantFerpaDisclosureConsentCommand {
  studentUserId?: string;
  recipient?: string | null;
  purpose?: string | null;
  scope?: string | null;
  effectiveFrom?: string;
  guardianUserId?: string | null;
  expiresAt?: string | null;
}

export interface ComplianceFERPARecordFerpaDisclosureCommand {
  studentUserId?: string;
  disclosedByUserId?: string;
  recipient?: string | null;
  basis?: ComplianceFERPAFerpaDisclosureBasis;
  purpose?: string | null;
  scope?: string | null;
  recordIdsJson?: string | null;
  disclosedAt?: string;
}

export interface ComplianceFERPARegisterEducationRecordCommand {
  studentUserId?: string;
  recordKind?: ComplianceFERPAEducationRecordKind;
  externalRecordId?: string | null;
  title?: string | null;
  protectionLevel?: ComplianceFERPAFerpaRecordProtectionLevel;
  isDirectoryInformation?: boolean;
  tenantId?: string | null;
  retentionUntil?: string | null;
  metadataJson?: string | null;
}

export interface ComplianceFERPASubmitFerpaInspectionRequestCommand {
  studentUserId?: string;
  requestedByUserId?: string;
  deadline?: string;
  description?: string | null;
}

export interface ComplianceFERPAUpsertDirectoryInformationPolicyCommand {
  tenantId?: string | null;
  allowedFieldsJson?: string | null;
  optOutEnabled?: boolean;
  annualNoticeSentAt?: string | null;
  noticeUrl?: string | null;
}

export interface ContentPagesContentResource {
  id?: string;
  slug?: string | null;
  title?: string | null;
  summary?: string | null;
  body?: string | null;
  resourceType?: string | null;
  status?: string | null;
  locale?: string | null;
  categorySlug?: string | null;
  tags?: string | null;
  authorId?: string | null;
  authorName?: string | null;
  coverImageUrl?: string | null;
  videoUrl?: string | null;
  downloadUrl?: string | null;
  externalUrl?: string | null;
  linkedEntityId?: string | null;
  linkedEntityType?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  ogImageUrl?: string | null;
  structuredData?: string | null;
  readingTimeMinutes?: number | null;
  viewCount?: number;
  isFeatured?: boolean;
  sortOrder?: number;
  publishedAt?: string | null;
  scheduledPublishAt?: string | null;
  customData?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export type ContentPagesContentResourceStatus = 'Draft' | 'InReview' | 'Published' | 'Archived';

export type ContentPagesContentResourceType = 'Article' | 'Tutorial' | 'Documentation' | 'Video' | 'Download' | 'ExternalLink' | 'Course' | 'Custom';

export interface ContentPagesCreateContentResource {
  slug?: string | null;
  title?: string | null;
  summary?: string | null;
  body?: string | null;
  resourceType?: ContentPagesContentResourceType;
  locale?: string | null;
  categorySlug?: string | null;
  tags?: string | null;
  coverImageUrl?: string | null;
  videoUrl?: string | null;
  downloadUrl?: string | null;
  externalUrl?: string | null;
  linkedEntityId?: string | null;
  linkedEntityType?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  ogImageUrl?: string | null;
  structuredData?: string | null;
  readingTimeMinutes?: number | null;
  isFeatured?: boolean;
  sortOrder?: number;
  customData?: string | null;
}

export interface ContentPagesCreateMarketingLead {
  source: string;
  name?: string | null;
  email: string;
  company?: string | null;
  topic?: string | null;
  plan?: string | null;
  message?: string | null;
  locale?: string | null;
  pagePath?: string | null;
  referrer?: string | null;
  userAgent?: string | null;
}

export interface ContentPagesCreatePage {
  slug?: string | null;
  title?: string | null;
  description?: string | null;
  pageType?: ContentPagesPageType;
  locale?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  canonicalUrl?: string | null;
  robotsDirective?: string | null;
  ogTitle?: string | null;
  ogDescription?: string | null;
  ogImageUrl?: string | null;
  ogType?: string | null;
  twitterCard?: string | null;
  twitterSite?: string | null;
  structuredData?: string | null;
  body?: string | null;
  customData?: string | null;
  parentPageId?: string | null;
  sortOrder?: number;
}

export interface ContentPagesCreatePageSection {
  sectionType?: ContentPagesSectionType;
  heading?: string | null;
  subheading?: string | null;
  data?: string | null;
  sortOrder?: number;
  isVisible?: boolean;
  cssClasses?: string | null;
}

export interface ContentPagesMarketingLead {
  id?: string;
  source?: string | null;
  status?: string | null;
  name?: string | null;
  email?: string | null;
  company?: string | null;
  topic?: string | null;
  plan?: string | null;
  message?: string | null;
  locale?: string | null;
  pagePath?: string | null;
  referrer?: string | null;
  userAgent?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface ContentPagesOpenGraphMetadata {
  slug?: string | null;
  title?: string | null;
  description?: string | null;
  ogTitle?: string | null;
  ogDescription?: string | null;
  ogImageUrl?: string | null;
  ogType?: string | null;
  twitterCard?: string | null;
  twitterSite?: string | null;
  canonicalUrl?: string | null;
  robotsDirective?: string | null;
  structuredData?: string | null;
}

export interface ContentPagesPage {
  id?: string;
  slug?: string | null;
  title?: string | null;
  description?: string | null;
  pageType?: string | null;
  status?: string | null;
  locale?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  canonicalUrl?: string | null;
  robotsDirective?: string | null;
  ogTitle?: string | null;
  ogDescription?: string | null;
  ogImageUrl?: string | null;
  ogType?: string | null;
  twitterCard?: string | null;
  twitterSite?: string | null;
  structuredData?: string | null;
  body?: string | null;
  customData?: string | null;
  parentPageId?: string | null;
  sortOrder?: number;
  sections?: Array<ContentPagesPageSection> | null;
  publishedAt?: string | null;
  scheduledPublishAt?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface ContentPagesPageSection {
  id?: string;
  pageId?: string;
  sectionType?: string | null;
  heading?: string | null;
  subheading?: string | null;
  data?: string | null;
  sortOrder?: number;
  isVisible?: boolean;
  cssClasses?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export type ContentPagesPageStatus = 'Draft' | 'Published' | 'Archived';

export type ContentPagesPageType = 'Landing' | 'Legal' | 'ResourceIndex' | 'Resource' | 'Custom';

export type ContentPagesSectionType =
  | 'Hero'
  | 'Features'
  | 'Testimonials'
  | 'Pricing'
  | 'CallToAction'
  | 'Faq'
  | 'RichText'
  | 'Gallery'
  | 'Stats'
  | 'Team'
  | 'LogoCloud'
  | 'Newsletter'
  | 'Contact'
  | 'ResourceCards'
  | 'Custom';

export interface ContentPagesSitemapEntry {
  slug?: string | null;
  updatedAt?: string | null;
  locale?: string | null;
}

export interface ContentPagesUpdateContentResource {
  slug?: string | null;
  title?: string | null;
  summary?: string | null;
  body?: string | null;
  resourceType?: ContentPagesContentResourceType;
  status?: ContentPagesContentResourceStatus;
  locale?: string | null;
  categorySlug?: string | null;
  tags?: string | null;
  coverImageUrl?: string | null;
  videoUrl?: string | null;
  downloadUrl?: string | null;
  externalUrl?: string | null;
  linkedEntityId?: string | null;
  linkedEntityType?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  ogImageUrl?: string | null;
  structuredData?: string | null;
  readingTimeMinutes?: number | null;
  isFeatured?: boolean | null;
  sortOrder?: number | null;
  scheduledPublishAt?: string | null;
  customData?: string | null;
}

export interface ContentPagesUpdatePage {
  slug?: string | null;
  title?: string | null;
  description?: string | null;
  pageType?: ContentPagesPageType;
  status?: ContentPagesPageStatus;
  locale?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  canonicalUrl?: string | null;
  robotsDirective?: string | null;
  ogTitle?: string | null;
  ogDescription?: string | null;
  ogImageUrl?: string | null;
  ogType?: string | null;
  twitterCard?: string | null;
  twitterSite?: string | null;
  structuredData?: string | null;
  body?: string | null;
  customData?: string | null;
  parentPageId?: string | null;
  sortOrder?: number | null;
  scheduledPublishAt?: string | null;
}

export interface ContentPagesUpdatePageSection {
  sectionType?: ContentPagesSectionType;
  heading?: string | null;
  subheading?: string | null;
  data?: string | null;
  sortOrder?: number | null;
  isVisible?: boolean | null;
  cssClasses?: string | null;
}

export interface FeaturesBulkEvaluationInput {
  featureKeys?: Array<string> | null;
  context?: FeaturesFeatureContext;
}

export interface FeaturesCapabilityAuditLog {
  id?: string;
  tenantId?: string;
  capabilityKey?: string | null;
  oldValue?: boolean | null;
  newValue?: boolean;
  oldSource?: string | null;
  newSource?: string | null;
  changedByUserId?: string | null;
  changeReason?: string | null;
  changeType?: string | null;
  changedAt?: string;
}

export interface FeaturesCapabilityCheckOutput {
  capability?: string | null;
  isEnabled?: boolean;
}

export interface FeaturesCreateFeatureInput {
  key?: string | null;
  name?: string | null;
  description?: string | null;
  isEnabled?: boolean;
  tenantId?: string | null;
}

export interface FeaturesFeatureContext {
  tenantId?: string | null;
  userId?: string | null;
  subscriptionPlanId?: string | null;
  environment?: string | null;
  permissions?: Array<string> | null;
  customAttributes?: Record<string, Record<string, unknown>> | null;
  userAgent?: string | null;
  ipAddress?: string | null;
  country?: string | null;
  requestTime?: string;
}

export interface FeaturesFeatureEvaluationInput {
  featureKey?: string | null;
  defaultValue?: Record<string, unknown> | null;
  context?: FeaturesFeatureContext;
}

export interface FeaturesFeatureFlag {
  id: string;
  key: string | null;
  name: string | null;
  description?: string | null;
  isEnabled: boolean;
  type: FeaturesFeatureFlagType;
  environment?: string | null;
  tenantId?: string | null;
  defaultValue?: Record<string, unknown> | null;
  createdAt: string;
  updatedAt?: string | null;
  deletedAt?: string | null;
  targets?: Array<FeaturesFeatureFlagTarget> | null;
}

export interface FeaturesFeatureFlagTarget {
  id: string;
  featureFlagId: string;
  targetType: string | null;
  targetIdentifier: string | null;
  isEnabled: boolean;
  rolloutPercentage?: number;
  customValue?: string | null;
  metadata?: string | null;
  priority?: number;
  createdAt: string;
  updatedAt?: string | null;
  deletedAt?: string | null;
}

export type FeaturesFeatureFlagType = 'Toggle' | 'Numeric' | 'String' | 'Percentage' | 'UserSegment';

export interface FeaturesSetCapabilityOverrideInput {
  capability?: string | null;
  isEnabled?: boolean;
  source?: string | null;
  reason?: string | null;
  expiresAt?: string | null;
}

export interface FeaturesToggleFeatureInput {
  featureKey?: string | null;
  isEnabled?: boolean;
  reason?: string | null;
  tenantId?: string | null;
  environment?: string | null;
}

export interface FeaturesUpdateFeatureInput {
  name?: string | null;
  description?: string | null;
  isEnabled?: boolean | null;
  rolloutPercentage?: number | null;
  enabledValue?: string | null;
  defaultValue?: string | null;
}

export interface Fido2NetLibAssertionOptions {
  challenge?: string | null;
  timeout?: number;
  rpId?: string | null;
  allowCredentials?: Array<ObjectsPublicKeyCredentialDescriptor> | null;
  userVerification?: ObjectsUserVerificationRequirement;
  hints?: Array<ObjectsPublicKeyCredentialHint> | null;
  extensions?: ObjectsAuthenticationExtensionsClientInputs;
}

export interface Fido2NetLibAuthenticatorSelection {
  authenticatorAttachment?: ObjectsAuthenticatorAttachment;
  residentKey?: ObjectsResidentKeyRequirement;
  requireResidentKey?: boolean;
  userVerification?: ObjectsUserVerificationRequirement;
}

export interface Fido2NetLibCredentialCreateOptions {
  rp: Fido2NetLibPublicKeyCredentialRpEntity;
  user: Fido2NetLibFido2User;
  challenge: string | null;
  pubKeyCredParams: Array<Fido2NetLibPubKeyCredParam> | null;
  timeout?: number;
  attestation?: ObjectsAttestationConveyancePreference;
  attestationFormats?: Array<ObjectsAttestationStatementFormatIdentifier> | null;
  authenticatorSelection?: Fido2NetLibAuthenticatorSelection;
  hints?: Array<ObjectsPublicKeyCredentialHint> | null;
  excludeCredentials?: Array<ObjectsPublicKeyCredentialDescriptor> | null;
  extensions?: ObjectsAuthenticationExtensionsClientInputs;
}

export interface Fido2NetLibFido2User {
  name?: string | null;
  id?: string | null;
  displayName?: string | null;
}

export interface Fido2NetLibPubKeyCredParam {
  type?: ObjectsPublicKeyCredentialType;
  alg?: ObjectsCOSEAlgorithm;
}

export interface Fido2NetLibPublicKeyCredentialRpEntity {
  id?: string | null;
  name?: string | null;
  icon?: string | null;
}

export interface GameJamsAddJamCriteriaInput {
  name?: string | null;
  description?: string | null;
  weight?: number;
  maxScore?: number;
}

export interface GameJamsCreateJamInput {
  name?: string | null;
  slug?: string | null;
  startDate?: string;
  endDate?: string;
  createdBy?: string;
  theme?: string | null;
  description?: string | null;
  rules?: string | null;
  submissionCriteria?: string | null;
  votingEndDate?: string | null;
  maxParticipants?: number | null;
}

export interface GameJamsJamCriteria {
  id?: string;
  jamId?: string;
  name?: string | null;
  description?: string | null;
  weight?: number;
  maxScore?: number;
}

export interface GameJamsJam {
  id?: string;
  name?: string | null;
  slug?: string | null;
  theme?: string | null;
  description?: string | null;
  startDate?: string;
  endDate?: string;
  votingEndDate?: string | null;
  maxParticipants?: number | null;
  participantCount?: number;
  status?: GameJamsJamStatus;
  createdBy?: string;
}

export interface GameJamsJamScore {
  id?: string;
  submissionId?: string;
  criteriaId?: string;
  judgeUserId?: string;
  score?: number;
  feedback?: string | null;
}

export type GameJamsJamStatus = 'Upcoming' | 'Active' | 'Voting' | 'Completed' | 'Cancelled';

export interface GameJamsJamSubmission {
  id?: string;
  jamId?: string;
  projectVersionId?: string;
  userId?: string;
  submissionNotes?: string | null;
}

export interface GameJamsScoreJamSubmissionInput {
  criteriaId?: string;
  judgeUserId?: string;
  score?: number;
  feedback?: string | null;
}

export interface GameJamsSubmitJamEntryInput {
  projectVersionId?: string;
  userId?: string;
  notes?: string | null;
}

export interface IdentityAuthenticationApiKey {
  id?: string;
  name?: string | null;
  keyPrefix?: string | null;
  scopes?: Array<string> | null;
  isActive?: boolean;
  expiresAt?: string | null;
  lastUsedAt?: string | null;
  usageCount?: number;
  createdAt?: string;
}

export interface IdentityAuthenticationBackupCodesOutput {
  codes?: Array<string> | null;
  generatedAt?: string;
}

export interface IdentityAuthenticationBackupCodesStatusOutput {
  totalCount: number;
  remainingCount: number;
  usedCount: number;
  hasBackupCodes: boolean;
}

export interface IdentityAuthenticationBeginWebAuthnAuthenticationInput {
  email?: string | null;
}

export interface IdentityAuthenticationBeginWebAuthnRegistrationInput {
  email?: string | null;
  displayName?: string | null;
  preferredAuthenticatorType?: IdentityAuthenticationWebAuthnAuthenticatorType;
}

export interface IdentityAuthenticationCleanupKeysInput {
  retentionDays?: number | null;
}

export interface IdentityAuthenticationCleanupResult {
  deletedCount?: number;
}

export interface IdentityAuthenticationClientCredentialsTokenOutput {
  accessToken?: string | null;
  tokenType?: string | null;
  expiresIn?: number;
  scope?: string | null;
}

export interface IdentityAuthenticationCompleteMfaSetupInput {
  code: string;
  secretKey: string;
}

export interface IdentityAuthenticationCompletePasswordResetInput {
  token: string;
  newPassword: string;
  confirmPassword: string;
  tenantId?: string | null;
}

export interface IdentityAuthenticationCompleteWebAuthnAuthenticationInput {
  assertionResponse?: string | null;
}

export interface IdentityAuthenticationCompleteWebAuthnRegistrationInput {
  attestationResponse?: string | null;
  friendlyName?: string | null;
  isPasswordless?: boolean;
}

export interface IdentityAuthenticationConsumeMagicLinkInput {
  token: string;
  tenantId?: string | null;
  deviceFingerprint?: string | null;
}

export interface IdentityAuthenticationCreateApiKeyCommand {
  name: string | null;
  scopes: Array<string> | null;
  expiresAt?: string | null;
  ipWhitelist?: string | null;
}

export interface IdentityAuthenticationCreateApiKeyOutput {
  id?: string;
  name?: string | null;
  apiKey?: string | null;
  keyPrefix?: string | null;
  scopes?: Array<string> | null;
  expiresAt?: string | null;
  createdAt?: string;
}

export interface IdentityAuthenticationCreateServiceAccountInput {
  name?: string | null;
  description?: string | null;
  tenantId?: string | null;
  scopes?: string | null;
  allowedIpAddresses?: string | null;
  expiresAt?: string | null;
}

export interface IdentityAuthenticationDeviceInfo {
  fingerprint?: string | null;
  deviceId?: string | null;
  ipAddress?: string | null;
  deviceName?: string | null;
  deviceType?: string | null;
  operatingSystem?: string | null;
  osVersion?: string | null;
  browser?: string | null;
  browserVersion?: string | null;
  screenResolution?: string | null;
  timezone?: string | null;
  language?: string | null;
  userAgent?: string | null;
  isMobile?: boolean;
  isBot?: boolean;
}

export interface IdentityAuthenticationDisableMfaInput {
  password: string;
}

export interface IdentityAuthenticationEmailVerificationOutput {
  message: string | null;
}

export interface IdentityAuthenticationEmailVerificationResult {
  success?: boolean;
  message?: string | null;
  email?: string | null;
  userId?: string | null;
  verifiedAt?: string | null;
}

export interface IdentityAuthenticationGitHubSignInOutput {
  authUrl: string | null;
}

export interface IdentityAuthenticationGoogleIdTokenInput {
  idToken: string;
  tenantId?: string | null;
}

export interface IdentityAuthenticationJwtKeyInfo {
  keyId?: string | null;
  algorithm?: string | null;
  isActive?: boolean;
  validFrom?: string;
  expiresAt?: string;
  rotatedAt?: string | null;
  rotationReason?: string | null;
  keyVersion?: number;
}

export interface IdentityAuthenticationLocalSignInInput {
  username?: string | null;
  email: string;
  password: string;
  tenantId?: string | null;
  deviceFingerprint?: string | null;
  emailOrUsername?: string | null;
}

export interface IdentityAuthenticationLocalSignUpInput {
  username: string;
  email: string;
  password: string;
  tenantId?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
}

export interface IdentityAuthenticationLocationInfo {
  ipAddress?: string | null;
  country?: string | null;
  countryCode?: string | null;
  region?: string | null;
  city?: string | null;
  postalCode?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone?: string | null;
  isp?: string | null;
  organization?: string | null;
  isProxy?: boolean | null;
  isHosting?: boolean | null;
  displayLocation?: string | null;
}

export interface IdentityAuthenticationLockServiceAccountInput {
  reason?: string | null;
}

export interface IdentityAuthenticationMagicLinkRequestResult {
  success?: boolean;
  message?: string | null;
  expiresInMinutes?: number;
  developmentPreviewToken?: string | null;
}

export interface IdentityAuthenticationMfaConfigurationOutput {
  isEnabled?: boolean;
  enabledMethods?: Array<string> | null;
  enabledAt?: string | null;
  backupCodesRemaining?: number;
}

export type IdentityAuthenticationMfaMethod = 'Totp' | 'BackupCode' | 'Sms' | 'Email' | 'WebAuthn';

export interface IdentityAuthenticationMfaMethodInfo {
  method: IdentityAuthenticationMfaMethod;
  name: string | null;
  description: string | null;
  isEnabled: boolean;
  isAvailable: boolean;
  priority: number;
}

export interface IdentityAuthenticationMfaMethodsOutput {
  methods: Array<IdentityAuthenticationMfaMethodInfo> | null;
  defaultMethod?: IdentityAuthenticationMfaMethod;
}

export interface IdentityAuthenticationMfaSetupOutput {
  isSuccess?: boolean;
  errorMessage?: string | null;
  secretKey?: string | null;
  qrCodeData?: string | null;
  qrCodeUri?: string | null;
  backupCodes?: Array<string> | null;
}

export interface IdentityAuthenticationMfaSuccessOutput {
  message: string | null;
}

export interface IdentityAuthenticationMfaVerificationOutput {
  isValid?: boolean;
  accessToken?: string | null;
  refreshToken?: string | null;
}

export interface IdentityAuthenticationOAuth2ErrorOutput {
  error?: string | null;
  errorDescription?: string | null;
}

export interface IdentityAuthenticationPasswordChangeInput {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  revokeOtherSessions?: boolean;
}

export interface IdentityAuthenticationPasswordChangeResult {
  success?: boolean;
  message?: string | null;
  sessionsRevoked?: number;
}

export interface IdentityAuthenticationPasswordResetRequestResult {
  success?: boolean;
  message?: string | null;
  expiresInMinutes?: number;
}

export interface IdentityAuthenticationPasswordResetResult {
  success?: boolean;
  message?: string | null;
  userId?: string | null;
}

export interface IdentityAuthenticationPatchServiceAccountInput {
  name?: string | null;
  description?: string | null;
  scopes?: string | null;
  expiresAt?: string | null;
}

export interface IdentityAuthenticationRefreshTokenInput {
  refreshToken: string;
  tenantId?: string | null;
}

export interface IdentityAuthenticationRequestMagicLinkInput {
  email: string;
  tenantId?: string | null;
}

export interface IdentityAuthenticationRequestPasswordResetInput {
  email: string;
  tenantId?: string | null;
}

export interface IdentityAuthenticationRevokeApiKeyInput {
  reason?: string | null;
}

export interface IdentityAuthenticationRevokeRefreshTokenInput {
  token: string;
  ipAddress?: string | null;
  reason?: string | null;
}

export type IdentityAuthenticationRiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export interface IdentityAuthenticationRotateKeyInput {
  reason?: string | null;
  validityDays?: number | null;
}

export interface IdentityAuthenticationSecretRotationOutput {
  clientSecret?: string | null;
  warning?: string | null;
}

export interface IdentityAuthenticationSendEmailVerificationInput {
  email: string;
}

export interface IdentityAuthenticationServiceAccountAuditEntry {
  id?: string;
  timestamp?: string;
  action?: string | null;
  performedBy?: string | null;
  ipAddress?: string | null;
  details?: string | null;
}

export interface IdentityAuthenticationServiceAccountAuditLogOutput {
  serviceAccountId?: string;
  entries?: Array<IdentityAuthenticationServiceAccountAuditEntry> | null;
  totalCount?: number;
  page?: number;
  pageSize?: number;
}

export interface IdentityAuthenticationServiceAccountCreatedOutput {
  id?: string;
  clientId?: string | null;
  clientSecret?: string | null;
  name?: string | null;
  description?: string | null;
  tenantId?: string | null;
  scopes?: string | null;
  createdAt?: string;
  expiresAt?: string | null;
  warning?: string | null;
}

export interface IdentityAuthenticationServiceAccountOutput {
  id?: string;
  clientId?: string | null;
  name?: string | null;
  description?: string | null;
  tenantId?: string | null;
  scopes?: string | null;
  isActive?: boolean;
  isLocked?: boolean;
  expiresAt?: string | null;
  createdAt?: string;
  createdBy?: string | null;
  lastAuthenticatedAt?: string | null;
  authenticationCount?: number;
  secretRotationCount?: number;
}

export interface IdentityAuthenticationSessionOutput {
  id?: string;
  deviceInfo?: IdentityAuthenticationDeviceInfo;
  location?: IdentityAuthenticationLocationInfo;
  ipAddress?: string | null;
  createdAt?: string;
  lastUsedAt?: string;
  expiresAt?: string;
  isTrustedDevice?: boolean;
  isCurrent?: boolean;
}

export interface IdentityAuthenticationSessionSecurityAnalysis {
  sessionId?: string;
  userId?: string;
  isSuspicious?: boolean;
  unusualActivityDetected?: boolean;
  riskScore?: number;
  activeSessionCount?: number;
  totalDeviceCount?: number;
  riskLevel?: IdentityAuthenticationRiskLevel;
  securityFlags?: Array<string> | null;
  riskFactors?: Array<string> | null;
  metadata?: Record<string, string> | null;
  analyzedAt?: string;
}

export interface IdentityAuthenticationSessionSuccessOutput {
  message: string | null;
}

export interface IdentityAuthenticationSessionTerminationOutput {
  message: string | null;
  terminatedCount: number;
}

export interface IdentityAuthenticationSignInOutput {
  success?: boolean;
  message?: string | null;
  accessToken?: string | null;
  refreshToken?: string | null;
  expiresAt?: string;
  accessTokenExpiresAt?: string;
  refreshTokenExpiresAt?: string;
  expiresIn?: number;
  userId?: string;
  email?: string | null;
  sessionId?: string;
  tempToken?: string | null;
  mfaToken?: string | null;
  user?: IdentityAuthenticationUser;
  tenantId?: string | null;
  availableTenants?: Array<TenantInfo> | null;
  requiresMfa?: boolean;
  mfaSessionId?: string | null;
  requiresStepUp?: boolean;
  stepUpToken?: string | null;
  stepUpExpiresAt?: string | null;
  riskLevel?: IdentityAuthenticationRiskLevel;
  riskFactors?: Array<string> | null;
  availableMethods?: Array<string> | null;
}

export interface IdentityAuthenticationSmsMfaSetupInput {
  phoneNumber: string | null;
}

export interface IdentityAuthenticationSmsMfaSetupOutput {
  message: string | null;
  phoneNumberMasked: string | null;
  expiresInSeconds: number;
}

export interface IdentityAuthenticationTrustDeviceInput {
  deviceName?: string | null;
}

export interface IdentityAuthenticationTrustedDeviceOutput {
  id?: string;
  deviceName?: string | null;
  deviceInfo?: IdentityAuthenticationDeviceInfo;
  trustedAt?: string;
  lastUsedAt?: string;
  expiresAt?: string | null;
}

export interface IdentityAuthenticationUpdateCredentialNameInput {
  friendlyName?: string | null;
}

export interface IdentityAuthenticationUpdateScopesInput {
  scopes?: string | null;
}

export interface IdentityAuthenticationUser {
  id?: string;
  email?: string | null;
  username?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  emailVerified?: boolean;
  phoneNumberVerified?: boolean;
  createdAt?: string;
  lastLoginAt?: string | null;
}

export interface IdentityAuthenticationVerifyEmailInput {
  token: string;
  tenantId?: string | null;
}

export interface IdentityAuthenticationVerifyMfaInput {
  userId?: string;
  code: string;
  method?: IdentityAuthenticationMfaMethod;
}

export interface IdentityAuthenticationWeb3ChallengeInput {
  walletAddress: string;
  chainId?: string | null;
}

export interface IdentityAuthenticationWeb3ChallengeOutput {
  challenge?: string | null;
  nonce?: string | null;
  expiresAt?: string;
}

export interface IdentityAuthenticationWeb3VerifyInput {
  walletAddress: string;
  signature: string;
  nonce: string;
  chainId: string;
  tenantId?: string | null;
  deviceFingerprint?: string | null;
}

export interface IdentityAuthenticationWebAuthnAuthenticationOptionsResult {
  success?: boolean;
  error?: string | null;
  sessionId?: string | null;
  optionsJson?: string | null;
  options?: Fido2NetLibAssertionOptions;
}

export interface IdentityAuthenticationWebAuthnAuthenticationResult {
  success?: boolean;
  error?: string | null;
  userId?: string | null;
  credentialId?: string | null;
  isPasswordless?: boolean;
  email?: string | null;
  accessToken?: string | null;
  refreshToken?: string | null;
  accessTokenExpiresAt?: string | null;
  refreshTokenExpiresAt?: string | null;
  expiresIn?: number;
}

export type IdentityAuthenticationWebAuthnAuthenticatorType = 'Platform' | 'CrossPlatform';

export interface IdentityAuthenticationWebAuthnCredentialInfo {
  id?: string;
  friendlyName?: string | null;
  authenticatorType?: IdentityAuthenticationWebAuthnAuthenticatorType;
  createdAt?: string;
  lastUsedAt?: string | null;
  isPasswordless?: boolean;
  isDefault?: boolean;
  backedUp?: boolean;
}

export interface IdentityAuthenticationWebAuthnCredentialVerifyResult {
  success?: boolean;
  error?: string | null;
  isValid?: boolean;
  isExpired?: boolean;
  isRevoked?: boolean;
  lastUsedAt?: string | null;
  signatureCount?: number;
}

export interface IdentityAuthenticationWebAuthnRegistrationOptionsResult {
  success?: boolean;
  error?: string | null;
  sessionId?: string | null;
  optionsJson?: string | null;
  options?: Fido2NetLibCredentialCreateOptions;
}

export interface IdentityAuthenticationWebAuthnRegistrationResult {
  success?: boolean;
  error?: string | null;
  credentialId?: string | null;
  friendlyName?: string | null;
}

export interface IdentityAuthenticationWebAuthnStatusOutput {
  isEnabled?: boolean;
  credentialCount?: number;
  hasPasswordlessCredential?: boolean;
  hasPlatformAuthenticator?: boolean;
  hasSecurityKey?: boolean;
}

export interface IdentityTenantsAddTenantMemberOutput {
  success?: boolean;
  message?: string | null;
  memberId?: string | null;
}

export interface IdentityTenantsAddUserMembershipInput {
  tenantId?: string;
  role?: string | null;
  invitedByEmail?: string | null;
}

export interface IdentityTenantsArchiveInput {
  reason?: string | null;
}

export interface IdentityTenantsBulkActivateTenantsCommand {
  tenantIds?: Array<string> | null;
}

export interface IdentityTenantsBulkArchiveTenantsCommand {
  tenantIds?: Array<string> | null;
}

export interface IdentityTenantsBulkCreateTenantItem {
  name?: string | null;
  slug?: string | null;
  adminEmail?: string | null;
  description?: string | null;
}

export interface IdentityTenantsBulkCreateTenantsCommand {
  tenants?: Array<IdentityTenantsBulkCreateTenantItem> | null;
}

export interface IdentityTenantsBulkDeactivateTenantsCommand {
  tenantIds?: Array<string> | null;
}

export interface IdentityTenantsBulkDeleteTenantsCommand {
  tenantIds?: Array<string> | null;
  hardDelete?: boolean;
}

export interface IdentityTenantsBulkPurgeTenantsCommand {
  tenantIds?: Array<string> | null;
}

export interface IdentityTenantsBulkUndeleteTenantsCommand {
  tenantIds?: Array<string> | null;
}

export interface IdentityTenantsBulkUpdateTenantItem {
  tenantId?: string;
  name?: string | null;
  description?: string | null;
}

export interface IdentityTenantsBulkUpdateTenantsCommand {
  updates?: Array<IdentityTenantsBulkUpdateTenantItem> | null;
}

export interface IdentityTenantsCreateTenantInput {
  name?: string | null;
  slug?: string | null;
  adminEmail?: string | null;
  description?: string | null;
}

export interface IdentityTenantsGetUserMembershipsOutput {
  memberships?: Array<IdentityTenantsUserMembership> | null;
  totalCount?: number;
}

export interface IdentityTenantsMembershipCountOutput {
  count?: number;
}

export interface IdentityTenantsRecoverInput {
  reason?: string | null;
}

export interface IdentityTenantsReplaceTenantMetadataInput {
  customFields?: Record<string, Record<string, unknown> | null> | null;
  tags?: Array<string> | null;
  externalReferences?: Record<string, string> | null;
  businessInfo?: IdentityTenantsUpdateTenantBusinessInfoInput;
  contactInfo?: IdentityTenantsUpdateTenantContactInfoInput;
  adminNotes?: string | null;
}

export interface IdentityTenantsReplaceTenantSettingsInput {
  systemConfiguration?: IdentityTenantsUpdateTenantSystemConfigurationInput;
  featureFlags?: Record<string, boolean> | null;
  businessRules?: IdentityTenantsUpdateTenantBusinessRulesInput;
  userInterfaceSettings?: IdentityTenantsUpdateTenantUiSettingsInput;
  securitySettings?: IdentityTenantsUpdateTenantSecuritySettingsInput;
  integrationSettings?: IdentityTenantsUpdateTenantIntegrationSettingsInput;
  systemLimits?: IdentityTenantsUpdateTenantSystemLimitsInput;
}

export interface IdentityTenantsSlugValidation {
  isAvailable?: boolean;
  isValid?: boolean;
  suggestedAlternatives?: Array<string> | null;
}

export interface IdentityTenantsTenant {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  isDefault?: boolean;
  isArchived?: boolean;
  archivedAt?: string | null;
  tenantMembers?: Array<IdentityTenantsTenantMember> | null;
  tenantDomains?: Array<IdentityTenantsTenantDomain> | null;
  tenantSettings?: IdentityTenantsTenantSettings;
  tenantStatistics?: IdentityTenantsTenantStatistics;
  usageTrackingRecords?: Array<IdentityTenantsUsageTracking> | null;
  name: string;
  description?: string | null;
  isActive?: boolean;
  slug: string;
  adminEmail?: string | null;
  canAcceptMembers?: boolean;
  activeMemberCount?: number;
  hasActiveMembers?: boolean;
}

export interface IdentityTenantsTenantAddress {
  street?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface IdentityTenantsTenantAuditLogEntry {
  id?: string;
  tenantId?: string;
  timestamp?: string;
  action?: string | null;
  actorId?: string | null;
  actorName?: string | null;
  actorEmail?: string | null;
  beforeValues?: Record<string, Record<string, unknown> | null> | null;
  afterValues?: Record<string, Record<string, unknown> | null> | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  correlationId?: string | null;
  metadata?: Record<string, string> | null;
}

export interface IdentityTenantsTenantBranding {
  logoUrl?: string | null;
  faviconUrl?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
  companyName?: string | null;
}

export interface IdentityTenantsTenantBusinessInfo {
  industry?: string | null;
  organizationSize?: string | null;
  tenantType?: string | null;
  geographicRegion?: string | null;
  complianceRequirements?: Array<string> | null;
}

export interface IdentityTenantsTenantBusinessRules {
  workflowRules?: Record<string, Record<string, unknown> | null> | null;
  validationRules?: Record<string, Record<string, unknown> | null> | null;
  approvalRules?: Record<string, Record<string, unknown> | null> | null;
  notificationRules?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsTenantContactInfo {
  primaryContactName?: string | null;
  primaryContactEmail?: string | null;
  primaryContactPhone?: string | null;
  organizationName?: string | null;
  address?: IdentityTenantsTenantAddress;
  website?: string | null;
}

export interface IdentityTenantsTenantCurrencySettings {
  defaultCurrency?: string | null;
  displayFormat?: string | null;
  decimalPlaces?: number;
}

export interface IdentityTenantsTenantDomain {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId: string;
  topLevelDomain: string;
  subdomain?: string | null;
  isMainDomain?: boolean;
  isSecondaryDomain?: boolean;
  userGroupId?: string | null;
  fullDomain?: string | null;
  tenant?: IdentityTenantsTenant;
}

export interface IdentityTenantsTenantIntegrationSettings {
  externalServices?: Record<string, Record<string, unknown> | null> | null;
  webhookSettings?: Record<string, Record<string, unknown> | null> | null;
  apiKeys?: Record<string, string> | null;
  ssoConfiguration?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsTenantMember {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId: string;
  userId: string;
  parentMemberId?: string | null;
  parentMember?: IdentityTenantsTenantMember;
  childMembers?: Array<IdentityTenantsTenantMember> | null;
  role: string;
  isActive?: boolean;
  joinedAt?: string;
  leftAt?: string | null;
  leaveReason?: string | null;
  metadata?: string | null;
  tenant?: IdentityTenantsTenant;
}

export interface IdentityTenantsTenantMetadata {
  id?: string;
  customFields?: Record<string, Record<string, unknown> | null> | null;
  tags?: Array<string> | null;
  externalReferences?: Record<string, string> | null;
  businessInfo?: IdentityTenantsTenantBusinessInfo;
  contactInfo?: IdentityTenantsTenantContactInfo;
  adminNotes?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface IdentityTenantsTenantSecuritySettings {
  passwordPolicy?: Record<string, Record<string, unknown> | null> | null;
  sessionTimeout?: number;
  twoFactorRequired?: boolean;
  ipWhitelist?: Array<string> | null;
  apiRateLimits?: Record<string, number> | null;
}

export interface IdentityTenantsTenantSettings {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId: string;
  defaultLanguage?: string | null;
  defaultTimezone?: string | null;
  defaultCurrency?: string | null;
  allowUserRegistration?: boolean;
  requireRegistrationApproval?: boolean;
  requireTwoFactorAuth?: boolean;
  maxUsers?: number | null;
  storageQuota?: number | null;
  enableAuditLogging?: boolean;
  enableApiAccess?: boolean;
  brandingSettings?: string | null;
  notificationSettings?: string | null;
  securitySettings?: string | null;
  integrationSettingsJson?: string | null;
  tenant?: IdentityTenantsTenant;
}

export interface IdentityTenantsTenantStatistics {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId: string;
  statisticDate?: string;
  totalMembers?: number;
  activeMembers?: number;
  inactiveMembers?: number;
  storageUsed?: number;
  apiCalls?: number;
  newMembers?: number;
  membersLeft?: number;
  customMetrics?: string | null;
  tenant?: IdentityTenantsTenant;
}

export interface IdentityTenantsTenantSystemConfiguration {
  timeZone?: string | null;
  locale?: string | null;
  dateFormat?: string | null;
  numberFormat?: string | null;
  currencySettings?: IdentityTenantsTenantCurrencySettings;
  customConfiguration?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsTenantSystemLimits {
  maxUsers?: number;
  maxStorage?: number;
  maxApiCalls?: number;
  maxProjects?: number;
  customLimits?: Record<string, number> | null;
}

export interface IdentityTenantsTenantUiSettings {
  theme?: string | null;
  layout?: Record<string, Record<string, unknown> | null> | null;
  branding?: IdentityTenantsTenantBranding;
  customCss?: string | null;
  componentSettings?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsTenantValidationError {
  field?: string | null;
  code?: string | null;
  message?: string | null;
}

export interface IdentityTenantsTenantValidationOutput {
  isValid?: boolean;
  errors?: Array<IdentityTenantsTenantValidationError> | null;
  warnings?: Array<IdentityTenantsTenantValidationWarning> | null;
  suggestions?: Array<string> | null;
  slugValidation?: IdentityTenantsSlugValidation;
}

export interface IdentityTenantsTenantValidationWarning {
  field?: string | null;
  code?: string | null;
  message?: string | null;
}

export interface IdentityTenantsUpdateTenantAddressInput {
  street?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface IdentityTenantsUpdateTenantBrandingInput {
  logoUrl?: string | null;
  faviconUrl?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
  companyName?: string | null;
}

export interface IdentityTenantsUpdateTenantBusinessInfoInput {
  industry?: string | null;
  organizationSize?: string | null;
  tenantType?: string | null;
  geographicRegion?: string | null;
  complianceRequirements?: Array<string> | null;
}

export interface IdentityTenantsUpdateTenantBusinessRulesInput {
  workflowRules?: Record<string, Record<string, unknown> | null> | null;
  validationRules?: Record<string, Record<string, unknown> | null> | null;
  approvalRules?: Record<string, Record<string, unknown> | null> | null;
  notificationRules?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsUpdateTenantContactInfoInput {
  primaryContactName?: string | null;
  primaryContactEmail?: string | null;
  primaryContactPhone?: string | null;
  organizationName?: string | null;
  address?: IdentityTenantsUpdateTenantAddressInput;
  website?: string | null;
}

export interface IdentityTenantsUpdateTenantCurrencySettingsInput {
  defaultCurrency?: string | null;
  displayFormat?: string | null;
  decimalPlaces?: number | null;
}

export interface IdentityTenantsUpdateTenantIntegrationSettingsInput {
  externalServices?: Record<string, Record<string, unknown> | null> | null;
  webhookSettings?: Record<string, Record<string, unknown> | null> | null;
  apiKeys?: Record<string, string> | null;
  ssoConfiguration?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsUpdateTenantMemberRoleOutput {
  success?: boolean;
  message?: string | null;
  memberId?: string;
  newRole?: string | null;
}

export interface IdentityTenantsUpdateTenantMetadataInput {
  customFields?: Record<string, Record<string, unknown> | null> | null;
  tags?: Array<string> | null;
  externalReferences?: Record<string, string> | null;
  businessInfo?: IdentityTenantsUpdateTenantBusinessInfoInput;
  contactInfo?: IdentityTenantsUpdateTenantContactInfoInput;
  adminNotes?: string | null;
}

export interface IdentityTenantsUpdateTenantInput {
  name?: string | null;
  description?: string | null;
}

export interface IdentityTenantsUpdateTenantSecuritySettingsInput {
  passwordPolicy?: Record<string, Record<string, unknown> | null> | null;
  sessionTimeout?: number | null;
  twoFactorRequired?: boolean | null;
  ipWhitelist?: Array<string> | null;
  apiRateLimits?: Record<string, number> | null;
}

export interface IdentityTenantsUpdateTenantSettingsInput {
  systemConfiguration?: IdentityTenantsUpdateTenantSystemConfigurationInput;
  featureFlags?: Record<string, boolean> | null;
  businessRules?: IdentityTenantsUpdateTenantBusinessRulesInput;
  userInterfaceSettings?: IdentityTenantsUpdateTenantUiSettingsInput;
  securitySettings?: IdentityTenantsUpdateTenantSecuritySettingsInput;
  integrationSettings?: IdentityTenantsUpdateTenantIntegrationSettingsInput;
  systemLimits?: IdentityTenantsUpdateTenantSystemLimitsInput;
}

export interface IdentityTenantsUpdateTenantSystemConfigurationInput {
  timeZone?: string | null;
  locale?: string | null;
  dateFormat?: string | null;
  numberFormat?: string | null;
  currencySettings?: IdentityTenantsUpdateTenantCurrencySettingsInput;
  customConfiguration?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsUpdateTenantSystemLimitsInput {
  maxUsers?: number | null;
  maxStorage?: number | null;
  maxApiCalls?: number | null;
  maxProjects?: number | null;
  customLimits?: Record<string, number> | null;
}

export interface IdentityTenantsUpdateTenantTagsInput {
  tags?: Array<string> | null;
}

export interface IdentityTenantsUpdateTenantUiSettingsInput {
  theme?: string | null;
  layout?: Record<string, Record<string, unknown> | null> | null;
  branding?: IdentityTenantsUpdateTenantBrandingInput;
  customCss?: string | null;
  componentSettings?: Record<string, Record<string, unknown> | null> | null;
}

export interface IdentityTenantsUpdateUserMembershipRoleInput {
  role?: string | null;
}

export interface IdentityTenantsUsageTracking {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId: string;
  date?: string;
  resourceType: string;
  usageAmount?: number;
  unit?: string | null;
  cost?: number;
  metadata?: string | null;
  tenant?: IdentityTenantsTenant;
}

export interface IdentityTenantsUserMembership {
  membershipId?: string;
  tenantId?: string;
  tenantName?: string | null;
  tenantSlug?: string | null;
  tenantIsActive?: boolean;
  tenantDescription?: string | null;
  role?: string | null;
  isActive?: boolean;
  joinedAt?: string;
  leftAt?: string | null;
}

export interface IdentityTenantsValidateTenantInput {
  name?: string | null;
  slug?: string | null;
  adminEmail?: string | null;
}

export interface IdentityUsersBulkActivateUsersInput {
  userIds?: Array<string> | null;
}

export interface IdentityUsersBulkActivateUsersOutput {
  activatedUsers?: Array<IdentityUsersUser> | null;
  failedUserIds?: Array<string> | null;
}

export interface IdentityUsersBulkCreateUsersInput {
  users?: Array<IdentityUsersCreateUserRequestItem> | null;
}

export interface IdentityUsersBulkCreateUsersOutput {
  createdUserIds?: Array<string> | null;
  failedEmails?: Array<string> | null;
}

export interface IdentityUsersBulkDeactivateUsersInput {
  userIds?: Array<string> | null;
}

export interface IdentityUsersBulkDeactivateUsersOutput {
  deactivatedUsers?: Array<IdentityUsersUser> | null;
  failedUserIds?: Array<string> | null;
}

export interface IdentityUsersBulkDeleteUsersInput {
  userIds?: Array<string> | null;
}

export interface IdentityUsersBulkNotificationInput {
  notificationIds?: Array<string> | null;
  operation?: string | null;
  filterCriteria?: IdentityUsersNotificationFilterCriteria;
}

export interface IdentityUsersBulkPurgeUsersInput {
  userIds?: Array<string> | null;
  strategy?: IdentityUsersPurgeStrategy;
}

export interface IdentityUsersBulkRestoreUsersInput {
  userIds?: Array<string> | null;
}

export interface IdentityUsersBulkRestoreUsersOutput {
  restoredUsers?: Array<IdentityUsersUser> | null;
  failedUserIds?: Array<string> | null;
}

export interface IdentityUsersBulkSuspendUsersInput {
  userIds?: Array<string> | null;
}

export interface IdentityUsersBulkSuspendUsersOutput {
  suspendedUsers?: Array<IdentityUsersUser> | null;
  failedUserIds?: Array<string> | null;
}

export interface IdentityUsersBulkUnsuspendUsersInput {
  userIds?: Array<string> | null;
}

export interface IdentityUsersBulkUnsuspendUsersOutput {
  unsuspendedUsers?: Array<IdentityUsersUser> | null;
  failedUserIds?: Array<string> | null;
}

export interface IdentityUsersBulkUpdateUsersInput {
  updates?: Array<IdentityUsersUpdateUserRequestItem> | null;
}

export interface IdentityUsersCreateUserInput {
  email?: string | null;
  name?: string | null;
  phoneNumber?: string | null;
}

export interface IdentityUsersCreateUserRequestItem {
  email?: string | null;
  name?: string | null;
  phoneNumber?: string | null;
}

export interface IdentityUsersNotificationAction {
  id?: string | null;
  text?: string | null;
  url?: string | null;
  type?: string | null;
  isPrimary?: boolean;
}

export interface IdentityUsersNotificationFilterCriteria {
  categories?: Array<string> | null;
  priorities?: Array<string> | null;
  types?: Array<string> | null;
  isRead?: boolean | null;
  isArchived?: boolean | null;
  dateFrom?: string | null;
  dateTo?: string | null;
}

export type IdentityUsersPurgeStrategy = 'Immediate' | 'Scheduled' | 'GracePeriod';

export interface IdentityUsersReplaceUserAccessibilityPreferencesInput {
  accessibilityPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersReplaceUserLocalizationPreferencesInput {
  localizationPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersReplaceUserMetadataInput {
  customFields?: Record<string, Record<string, unknown>> | null;
  tags?: Array<string> | null;
  externalReferences?: Record<string, string> | null;
}

export interface IdentityUsersReplaceUserNotificationPreferencesInput {
  notificationPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersReplaceUserPreferencesInput {
  generalPreferences?: Record<string, Record<string, unknown>> | null;
  notificationPreferences?: Record<string, Record<string, unknown>> | null;
  accessibilityPreferences?: Record<string, Record<string, unknown>> | null;
  privacyPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersReplaceUserPrivacyPreferencesInput {
  privacyPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersReplaceUserProfileInput {
  displayName?: string | null;
  bio?: string | null;
  location?: string | null;
  website?: string | null;
  jobTitle?: string | null;
  company?: string | null;
  timeZone?: string | null;
  language?: string | null;
  profileVisibility?: string | null;
  showEmail?: boolean;
  showLocation?: boolean;
}

export interface IdentityUsersUpdateUserAccessibilityPreferencesInput {
  accessibilityPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUpdateUserLocalizationPreferencesInput {
  localizationPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUpdateUserMetadataInput {
  customFields?: Record<string, Record<string, unknown>> | null;
  tagsToAdd?: Array<string> | null;
  tagsToRemove?: Array<string> | null;
  externalReferences?: Record<string, string> | null;
}

export interface IdentityUsersUpdateUserNotificationPreferencesInput {
  notificationPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUpdateUserPreferencesInput {
  generalPreferences?: Record<string, Record<string, unknown>> | null;
  notificationPreferences?: Record<string, Record<string, unknown>> | null;
  accessibilityPreferences?: Record<string, Record<string, unknown>> | null;
  privacyPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUpdateUserPrivacyPreferencesInput {
  privacyPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUpdateUserProfileInput {
  displayName?: string | null;
  bio?: string | null;
  location?: string | null;
  website?: string | null;
  jobTitle?: string | null;
  company?: string | null;
  timeZone?: string | null;
  language?: string | null;
  profileVisibility?: string | null;
  showEmail?: boolean | null;
  showLocation?: boolean | null;
}

export interface IdentityUsersUpdateUserInput {
  name?: string | null;
  phoneNumber?: string | null;
}

export interface IdentityUsersUpdateUserRequestItem {
  userId?: string;
  name?: string | null;
  phoneNumber?: string | null;
}

export interface IdentityUsersUserAccessibilityPreferences {
  highContrast?: boolean;
  largeText?: boolean;
  screenReader?: boolean;
  reducedMotion?: boolean;
  keyboardNavigation?: boolean;
  fontSize?: number;
  colorScheme?: string | null;
  customSettings?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUser {
  id?: string;
  email?: string | null;
  name?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
  isActive?: boolean;
  phoneNumber?: string | null;
  lastSeenAt?: string | null;
}

export interface IdentityUsersUserLocalizationPreferences {
  language?: string | null;
  timezone?: string | null;
  dateFormat?: string | null;
  timeFormat?: string | null;
  currency?: string | null;
  numberFormat?: Record<string, Record<string, unknown>> | null;
  customSettings?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUserMetadata {
  id?: string;
  userId?: string;
  customFields?: Record<string, Record<string, unknown>> | null;
  tags?: Array<string> | null;
  externalReferences?: Record<string, string> | null;
  createdAt?: string;
  updatedAt?: string | null;
  version?: string | null;
}

export interface IdentityUsersUserNotificationDetail {
  notification?: IdentityUsersUserNotification;
  relatedNotifications?: Array<IdentityUsersUserNotification> | null;
  actions?: Array<IdentityUsersNotificationAction> | null;
}

export interface IdentityUsersUserNotification {
  id?: string;
  userId?: string;
  type?: string | null;
  title?: string | null;
  message?: string | null;
  priority?: string | null;
  category?: string | null;
  isRead?: boolean;
  isArchived?: boolean;
  readAt?: string | null;
  archivedAt?: string | null;
  expiresAt?: string | null;
  actionUrl?: string | null;
  actionText?: string | null;
  imageUrl?: string | null;
  metadata?: Record<string, Record<string, unknown>> | null;
  createdAt?: string;
  updatedAt?: string | null;
  version?: string | null;
}

export interface IdentityUsersUserNotificationPreferences {
  emailEnabled?: boolean;
  pushEnabled?: boolean;
  smsEnabled?: boolean;
  inAppEnabled?: boolean;
  frequency?: string | null;
  quietHours?: Record<string, Record<string, unknown>> | null;
  categoryPreferences?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUserPreferences {
  id?: string;
  userId?: string;
  generalPreferences?: Record<string, Record<string, unknown>> | null;
  notificationPreferences?: Record<string, Record<string, unknown>> | null;
  accessibilityPreferences?: Record<string, Record<string, unknown>> | null;
  privacyPreferences?: Record<string, Record<string, unknown>> | null;
  localizationPreferences?: Record<string, Record<string, unknown>> | null;
  createdAt?: string;
  updatedAt?: string | null;
  version?: string | null;
}

export interface IdentityUsersUserPrivacyPreferences {
  profileVisibility?: string | null;
  activityTracking?: boolean;
  dataCollection?: Record<string, Record<string, unknown>> | null;
  thirdPartySharing?: Record<string, Record<string, unknown>> | null;
  marketingEmails?: boolean;
  analyticsCookies?: boolean;
  personalizedContent?: boolean;
  customSettings?: Record<string, Record<string, unknown>> | null;
}

export interface IdentityUsersUserProfile {
  id?: string;
  userId?: string;
  displayName?: string | null;
  bio?: string | null;
  location?: string | null;
  website?: string | null;
  jobTitle?: string | null;
  company?: string | null;
  avatarUrl?: string | null;
  bannerUrl?: string | null;
  timeZone?: string | null;
  language?: string | null;
  profileVisibility?: string | null;
  showEmail?: boolean;
  showLocation?: boolean;
  createdAt?: string;
  updatedAt?: string | null;
  version?: string | null;
}

export interface LearningAssessmentsAssessment {
  id?: string;
  courseId?: string;
  contentId?: string | null;
  title?: string | null;
  description?: string | null;
  type?: LearningAssessmentsAssessmentType;
  maxScore?: number;
  passingScore?: number;
  timeLimitMinutes?: number | null;
  maxAttempts?: number | null;
  isRequired?: boolean;
  order?: number;
  availableFrom?: string | null;
  availableUntil?: string | null;
  isAvailable?: boolean;
}

export interface LearningAssessmentsAssessmentSubmission {
  id?: string;
  assessmentId?: string;
  enrollmentId?: string;
  userId?: string;
  attemptNumber?: number;
  score?: number | null;
  passed?: boolean | null;
  startedAt?: string;
  submittedAt?: string | null;
  gradedAt?: string | null;
  gradedBy?: string | null;
  feedback?: string | null;
  status?: LearningAssessmentsSubmissionStatus;
}

export type LearningAssessmentsAssessmentType = 'Quiz' | 'Exam' | 'Assignment' | 'Project' | 'PeerReview' | 'SelfAssessment';

export interface LearningAssessmentsCanAttemptOutput {
  canAttempt?: boolean;
  currentAttemptCount?: number;
}

export interface LearningAssessmentsCreateAssessmentInput {
  courseId?: string;
  title?: string | null;
  description?: string | null;
  type?: LearningAssessmentsAssessmentType;
  maxScore?: number;
  passingScore?: number;
  timeLimitMinutes?: number | null;
  maxAttempts?: number | null;
  isRequired?: boolean;
  availableFrom?: string | null;
  availableUntil?: string | null;
}

export interface LearningAssessmentsGradeSubmissionInput {
  score?: number;
  gradedBy?: string | null;
  feedback?: string | null;
}

export interface LearningAssessmentsStartSubmissionInput {
  enrollmentId?: string;
}

export type LearningAssessmentsSubmissionStatus = 'InProgress' | 'Submitted' | 'Graded' | 'Returned' | 'Late';

export interface LearningAssessmentsUpdateAssessmentInput {
  title?: string | null;
  description?: string | null;
  maxScore?: number | null;
  passingScore?: number | null;
  timeLimitMinutes?: number | null;
  maxAttempts?: number | null;
  isRequired?: boolean | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  contentId?: string | null;
  clearContentId?: boolean;
}

export interface LearningCoursesActivityGrade {
  id?: string;
  contentInteractionId?: string;
  graderProgramUserId?: string | null;
  grade?: number;
  feedback?: string | null;
  gradingDetails?: string | null;
  gradedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  contentInteraction?: LearningCoursesContentInteractionSummary;
  grader?: LearningCoursesGraderSummary;
  isPassingGrade?: boolean;
  gradePercentage?: string | null;
  hasFeedback?: boolean;
  hasGradingDetails?: boolean;
}

export interface LearningCoursesCircularDependencyCheckResult {
  wouldCreateCycle?: boolean;
}

export interface LearningCoursesCloneProgram {
  newTitle?: string | null;
  newDescription?: string | null;
}

export interface LearningCoursesCompleteContentInput {
  programUserId?: string;
  contentId?: string;
}

export interface LearningCoursesCompletionRates {
  programId?: string;
  overallCompletionRate?: number;
  contentCompletionRates?: Record<string, number> | null;
  completionTrends?: Array<LearningCoursesCompletionTrend> | null;
}

export interface LearningCoursesCompletionTrend {
  date?: string;
  completedCount?: number;
  totalCount?: number;
  rate?: number;
}

export interface LearningCoursesContentInteraction {
  id?: string;
  programUserId?: string;
  contentId?: string;
  status?: LearningCoursesProgressStatus;
  submissionData?: string | null;
  completionPercentage?: number;
  timeSpentMinutes?: number | null;
  firstAccessedAt?: string | null;
  lastAccessedAt?: string | null;
  completedAt?: string | null;
  submittedAt?: string | null;
  createdAt?: string;
  updatedAt?: string;
  content?: LearningCoursesContentSummary;
  programUser?: LearningCoursesProgramUserSummary;
  isSubmitted?: boolean;
  isCompleted?: boolean;
  canModify?: boolean;
  durationInMinutes?: number;
}

export interface LearningCoursesContentInteractionSummary {
  id?: string;
  programUserId?: string;
  contentId?: string;
  status?: string | null;
  submittedAt?: string | null;
  content?: LearningCoursesContentSummary;
  student?: LearningCoursesStudentSummary;
}

export interface LearningCoursesContentProgress {
  contentId?: string;
  title?: string | null;
  status?: LearningCoursesProgressStatus;
  completionPercentage?: number;
  firstAccessedAt?: string | null;
  lastAccessedAt?: string | null;
  completedAt?: string | null;
}

export interface LearningCoursesContentStats {
  programId?: string;
  totalContent?: number;
  requiredContent?: number;
  optionalContent?: number;
  contentByType?: {
    Lesson?: number;
    Page?: number;
    Assignment?: number;
    Questionnaire?: number;
    Discussion?: number;
    Code?: number;
    Challenge?: number;
    Reflection?: number;
    Survey?: number;
  } | null;
  contentByVisibility?: { Public?: number; Internal?: number; Private?: number; Restricted?: number } | null;
  topLevelContent?: number;
  nestedContent?: number;
}

export interface LearningCoursesContentSummary {
  id?: string;
  title?: string | null;
  contentType?: string | null;
  estimatedMinutes?: number | null;
}

export interface LearningCoursesCreateActivityGrade {
  contentInteractionId?: string;
  graderProgramUserId?: string;
  grade?: number;
  feedback?: string | null;
  gradingDetails?: string | null;
}

export interface LearningCoursesCreatePrerequisiteApiInput {
  courseId?: string;
  prerequisiteCourseId?: string;
  type?: LearningCoursesPrerequisiteType;
  minimumGrade?: number | null;
  description?: string | null;
  displayOrder?: number;
  prerequisiteGroup?: string | null;
}

export interface LearningCoursesCreateProductFromProgram {
  name?: string | null;
  description?: string | null;
  basePrice?: number;
  currency?: string | null;
}

export interface LearningCoursesCreateProgramContent {
  programId: string;
  parentId?: string | null;
  title: string;
  description?: string | null;
  type: LearningCoursesProgramContentType;
  body?: string | null;
  sortOrder?: number;
  isRequired?: boolean;
  gradingMethod?: LearningCoursesGradingMethod;
  maxPoints?: number | null;
  estimatedMinutes?: number | null;
  visibility?: LearningCoursesVisibility;
}

export interface LearningCoursesCreateProgram {
  title?: string | null;
  description?: string | null;
  slug?: string | null;
  thumbnail?: string | null;
  creatorId?: string | null;
}

export interface LearningCoursesEngagementMetrics {
  programId?: string;
  dailyActiveUsers?: number;
  weeklyActiveUsers?: number;
  monthlyActiveUsers?: number;
  averageSessionDuration?: string;
  totalSessions?: number;
  retentionRate?: number;
  contentEngagement?: Record<string, number> | null;
}

export type LearningCoursesEnrollmentStatus = 'Open' | 'Active' | 'Paused' | 'Cancelled' | 'Expired' | 'Completed' | 'Closed' | 'InviteOnly' | 'Waitlist';

export interface LearningCoursesGradeStatistics {
  totalGrades?: number;
  averageGrade?: number;
  minGrade?: number;
  maxGrade?: number;
  passingRate?: number;
  averageGradeFormatted?: string | null;
  passingRateFormatted?: string | null;
  hasGrades?: boolean;
}

export interface LearningCoursesGraderSummary {
  id?: string;
  userDisplayName?: string | null;
  userEmail?: string | null;
  role?: string | null;
}

export type LearningCoursesGradingMethod = 'None' | 'Instructor' | 'Peer' | 'Ai' | 'AutomatedTests';

export interface LearningCoursesMonetization {
  price?: number;
  currency?: string | null;
  isSubscription?: boolean;
  subscriptionDurationDays?: number | null;
}

export interface LearningCoursesMoveContent {
  contentId: string;
  newParentId?: string | null;
  newSortOrder: number;
}

export interface LearningCoursesPrerequisiteCheckResult {
  isSatisfied?: boolean;
  prerequisites?: Array<LearningCoursesPrerequisiteStatus> | null;
}

export interface LearningCoursesPrerequisite {
  id?: string;
  courseId?: string;
  prerequisiteCourseId?: string;
  prerequisiteCourseName?: string | null;
  tenantId?: string | null;
  type?: LearningCoursesPrerequisiteType;
  minimumGrade?: number | null;
  description?: string | null;
  displayOrder?: number;
  prerequisiteGroup?: string | null;
  createdAt?: string;
}

export interface LearningCoursesPrerequisiteStatus {
  prerequisiteId?: string;
  prerequisiteCourseId?: string;
  courseName?: string | null;
  type?: LearningCoursesPrerequisiteType;
  isSatisfied?: boolean;
  requiredGrade?: number | null;
  achievedGrade?: number | null;
  reason?: string | null;
}

export type LearningCoursesPrerequisiteType = 'Required' | 'Recommended' | 'Corequisite';

export interface LearningCoursesPricing {
  price?: number;
  currency?: string | null;
  isSubscription?: boolean;
  subscriptionDurationDays?: number | null;
  isMonetizationEnabled?: boolean;
}

export interface LearningCoursesProgramAnalytics {
  programId?: string;
  title?: string | null;
  totalUsers?: number;
  activeUsers?: number;
  completedUsers?: number;
  completionRate?: number;
  averageCompletionTime?: string;
  totalViews?: number;
  lastActivity?: string | null;
  additionalMetrics?: Record<string, Record<string, unknown>> | null;
}

export interface LearningCoursesProgramContent {
  id?: string;
  programId?: string;
  parentId?: string | null;
  title?: string | null;
  description?: string | null;
  type?: LearningCoursesProgramContentType;
  body?: Record<string, unknown> | null;
  sortOrder?: number;
  isRequired?: boolean;
  gradingMethod?: LearningCoursesGradingMethod;
  maxPoints?: number | null;
  estimatedMinutes?: number | null;
  visibility?: LearningCoursesVisibility;
  createdAt?: string;
  updatedAt?: string | null;
  programTitle?: string | null;
  parentTitle?: string | null;
  childrenCount?: number;
  children?: Array<LearningCoursesProgramContent> | null;
}

export type LearningCoursesProgramContentType =
  | 'Lesson'
  | 'Page'
  | 'Assignment'
  | 'Questionnaire'
  | 'Discussion'
  | 'Code'
  | 'Challenge'
  | 'Reflection'
  | 'Survey';

export type LearningCoursesProgramDifficulty = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';

export interface LearningCoursesProgram {
  id?: string;
  creatorId?: string | null;
  title?: string | null;
  description?: string | null;
  visibility?: ContentVisibility;
  slug?: string | null;
  status?: ContentStatus;
  thumbnail?: string | null;
  videoShowcaseUrl?: string | null;
  estimatedHours?: number | null;
  enrollmentStatus?: LearningCoursesEnrollmentStatus;
  maxEnrollments?: number | null;
  enrollmentDeadline?: string | null;
  category?: ProgramCategory;
  difficulty?: LearningCoursesProgramDifficulty;
  skillsRequired?: string | null;
  skillsProvided?: string | null;
  currentEnrollments?: number;
  averageRating?: number;
  totalRatings?: number;
  isEnrollmentOpen?: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface LearningCoursesProgramUserSummary {
  id?: string;
  userDisplayName?: string | null;
  userEmail?: string | null;
}

export type LearningCoursesProgressStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Submitted';

export interface LearningCoursesRejectProgram {
  reason?: string | null;
}

export interface LearningCoursesReorderContent {
  contentIds?: Array<string> | null;
}

export interface LearningCoursesReorderPrerequisitesInput {
  prerequisiteIds?: Array<string> | null;
}

export interface LearningCoursesRevenueAnalytics {
  programId?: string;
  totalRevenue?: number;
  monthlyRevenue?: number;
  totalPurchases?: number;
  monthlyPurchases?: number;
  averageRevenuePerUser?: number;
  conversionRate?: number;
  revenueChart?: Array<LearningCoursesRevenueChart> | null;
}

export interface LearningCoursesRevenueChart {
  date?: string;
  revenue?: number;
  purchases?: number;
}

export interface LearningCoursesScheduleProgram {
  publishAt?: string;
}

export interface LearningCoursesSearchContent {
  programId: string;
  searchTerm: string;
  type?: LearningCoursesProgramContentType;
  visibility?: LearningCoursesVisibility;
  isRequired?: boolean | null;
  parentId?: string | null;
}

export interface LearningCoursesStartContentInput {
  programUserId?: string;
  contentId?: string;
}

export interface LearningCoursesStudentSummary {
  id?: string;
  userDisplayName?: string | null;
  userEmail?: string | null;
}

export interface LearningCoursesSubmitContentInput {
  programUserId?: string;
  contentId?: string;
  submissionData?: string | null;
}

export interface LearningCoursesSubmitUserContent {
  submissionData: string;
}

export interface LearningCoursesUpdateActivityGrade {
  grade?: number | null;
  feedback?: string | null;
  gradingDetails?: string | null;
}

export interface LearningCoursesUpdatePrerequisiteApiInput {
  type?: LearningCoursesPrerequisiteType;
  minimumGrade?: number | null;
  description?: string | null;
  displayOrder?: number | null;
  prerequisiteGroup?: string | null;
}

export interface LearningCoursesUpdatePricing {
  price?: number | null;
  currency?: string | null;
  isSubscription?: boolean | null;
  subscriptionDurationDays?: number | null;
}

export interface LearningCoursesUpdateProgramContent {
  id: string;
  title?: string | null;
  description?: string | null;
  type?: LearningCoursesProgramContentType;
  body?: string | null;
  sortOrder?: number | null;
  isRequired?: boolean | null;
  gradingMethod?: LearningCoursesGradingMethod;
  maxPoints?: number | null;
  estimatedMinutes?: number | null;
  visibility?: LearningCoursesVisibility;
}

export interface LearningCoursesUpdateProgram {
  title?: string | null;
  description?: string | null;
  slug?: string | null;
  thumbnail?: string | null;
  videoShowcaseUrl?: string | null;
  estimatedHours?: number | null;
  visibility?: ContentVisibility;
  category?: ProgramCategory;
  difficulty?: LearningCoursesProgramDifficulty;
  skillsRequired?: string | null;
  skillsProvided?: string | null;
  enrollmentStatus?: LearningCoursesEnrollmentStatus;
  maxEnrollments?: number | null;
  enrollmentDeadline?: string | null;
}

export interface LearningCoursesUpdateProgress {
  status?: LearningCoursesProgressStatus;
  lastAccessedAt?: string | null;
  additionalData?: Record<string, Record<string, unknown>> | null;
}

export interface LearningCoursesUpdateProgressInput {
  programUserId?: string;
  contentId?: string;
  completionPercentage?: number;
}

export interface LearningCoursesUpdateTimeSpentInput {
  programUserId?: string;
  contentId?: string;
  additionalMinutes?: number;
}

export interface LearningCoursesUserProgress {
  completionPercentage?: number;
  lastAccessedAt?: string | null;
  startedAt?: string | null;
  completedAt?: string | null;
  contentProgress?: Array<LearningCoursesContentProgress> | null;
}

export type LearningCoursesVisibility = 'Public' | 'Internal' | 'Private' | 'Restricted';

export interface LearningEnrollmentsEnrollUserInput {
  courseId?: string;
  userId?: string;
  cohortId?: string | null;
}

export interface LearningEnrollmentsEnrollment {
  id?: string;
  courseId?: string;
  userId?: string;
  cohortId?: string | null;
  status?: LearningEnrollmentsEnrollmentStatus;
  enrolledAt?: string;
  completedAt?: string | null;
  droppedAt?: string | null;
  progress?: number;
  lastActivityAt?: string | null;
}

export type LearningEnrollmentsEnrollmentStatus = 'Active' | 'Paused' | 'Completed' | 'Dropped' | 'Expired';

export interface LearningEnrollmentsUpdateEnrollmentProgressInput {
  progress?: number;
}

export interface MvcProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

export type ObjectsAttestationConveyancePreference = 'None' | 'Indirect' | 'Direct' | 'Enterprise';

export type ObjectsAttestationStatementFormatIdentifier = 'Packed' | 'Tpm' | 'AndroidKey' | 'AndroidSafetyNet' | 'FidoU2f' | 'Apple' | 'None';

export interface ObjectsAuthenticationExtensionsClientInputs {
  'example.extension.bool'?: boolean | null;
  exts?: boolean | null;
  uvm?: boolean | null;
  credProps?: boolean | null;
  prf?: ObjectsAuthenticationExtensionsPRFInputs;
  largeBlob?: ObjectsAuthenticationExtensionsLargeBlobInputs;
  credentialProtectionPolicy?: ObjectsCredentialProtectionPolicy;
  enforceCredentialProtectionPolicy?: boolean | null;
}

export interface ObjectsAuthenticationExtensionsLargeBlobInputs {
  support?: ObjectsLargeBlobSupport;
  read?: boolean;
  write?: string | null;
}

export interface ObjectsAuthenticationExtensionsPRFInputs {
  eval?: ObjectsAuthenticationExtensionsPRFValues;
  evalByCredential?: KeyValuePairStringAuthenticationExtensionsPRFValues;
}

export interface ObjectsAuthenticationExtensionsPRFValues {
  first: string | null;
  second?: string | null;
}

export type ObjectsAuthenticatorAttachment = 'Platform' | 'CrossPlatform';

export type ObjectsAuthenticatorTransport = 'Usb' | 'Nfc' | 'Ble' | 'SmartCard' | 'Hybrid' | 'Internal';

export type ObjectsCOSEAlgorithm = 'RS1' | 'RS512' | 'RS384' | 'RS256' | 'ES256K' | 'PS512' | 'PS384' | 'PS256' | 'ES512' | 'ES384' | 'EdDSA' | 'ES256';

export type ObjectsCredentialProtectionPolicy = 'UserVerificationOptional' | 'UserVerificationOptionalWithCredentialIdList' | 'UserVerificationRequired';

export type ObjectsLargeBlobSupport = 'Required' | 'Preferred';

export interface ObjectsPublicKeyCredentialDescriptor {
  type?: ObjectsPublicKeyCredentialType;
  id?: string | null;
  transports?: Array<ObjectsAuthenticatorTransport> | null;
}

export type ObjectsPublicKeyCredentialHint = 'SecurityKey' | 'ClientDevice' | 'Hybrid';

export type ObjectsPublicKeyCredentialType = 'PublicKey' | 'Invalid';

export type ObjectsResidentKeyRequirement = 'Required' | 'Preferred' | 'Discouraged';

export type ObjectsUserVerificationRequirement = 'Required' | 'Preferred' | 'Discouraged';

export interface PagedResult {
  items?: Array<CommerceProductsProduct> | null;
  totalCount?: number;
  skip?: number;
  take?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface ResourcesArchiveResourceUsageRecordsInput {
  olderThan?: string;
}

export interface ResourcesCheckResourceQuotaInput {
  amount?: number;
}

export interface ResourcesCleanupOrphanedResourcesInput {
  dryRun?: boolean;
  resourceTypes?: Array<ResourcesResourceUsageType> | null;
}

export interface ResourcesEffectiveSettingOutput {
  key?: string | null;
  value?: string | null;
  isUserOverride?: boolean;
}

export interface ResourcesRecordTenantResourceUsageInput {
  resourceUsageType?: ResourcesResourceUsageType;
  count?: number;
  periodStart?: string;
  periodEnd?: string;
  metadata?: Record<string, string> | null;
}

export interface ResourcesRecordUserResourceUsageInput {
  resourceUsageType?: ResourcesResourceUsageType;
  count?: number;
  periodStart?: string;
  periodEnd?: string;
  metadata?: Record<string, string> | null;
}

export interface ResourcesResourceMetadata {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  key: string;
  value?: string | null;
  dataType?: string | null;
  description?: string | null;
  category?: string | null;
  isSystemManaged?: boolean;
  isActive?: boolean;
  displayOrder?: number;
  userId?: string | null;
  resourceId?: string | null;
  rowVersion?: string | null;
}

export interface ResourcesResourceQuotaEnforcementResult {
  isAllowed?: boolean;
  isSoftLimitExceeded?: boolean;
  isHardLimitExceeded?: boolean;
  currentUsage?: number;
  softLimit?: number | null;
  hardLimit?: number | null;
  usagePercentage?: number;
  excessAmount?: number;
  message?: string | null;
  type?: ResourcesResourceUsageType;
  nextReset?: string | null;
  remainingQuota?: number | null;
}

export type ResourcesResourceQuotaPeriod = 'Daily' | 'Weekly' | 'Monthly' | 'Quarterly' | 'Yearly' | 'Unlimited';

export interface ResourcesResourceQuotaOutput {
  id?: string;
  tenantId?: string;
  type?: ResourcesResourceUsageType;
  limit?: number;
  currentUsage?: number;
  remainingQuota?: number;
  usagePercentage?: number;
  softLimitPercentage?: number;
  isActive?: boolean;
  period?: ResourcesResourceQuotaPeriod;
  lastResetDate?: string;
  nextResetDate?: string;
  description?: string | null;
  isSoftLimitExceeded?: boolean;
  isHardLimitExceeded?: boolean;
  shouldReset?: boolean;
  softLimit?: number | null;
  hardLimit?: number | null;
}

export interface ResourcesResourceSettings {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  key: string;
  value?: string | null;
  defaultValue?: string | null;
  dataType?: string | null;
  description?: string | null;
  category?: string | null;
  isSystemManaged?: boolean;
  isActive?: boolean;
  allowUserOverride?: boolean;
  displayOrder?: number;
  userId?: string | null;
  validationRules?: string | null;
  rowVersion?: string | null;
}

export type ResourcesResourceUsageType =
  | 'Users'
  | 'Projects'
  | 'Storage'
  | 'ApiCalls'
  | 'Programs'
  | 'Courses'
  | 'FeatureFlags'
  | 'SubscriptionPlans'
  | 'Products'
  | 'TestingSessions'
  | 'Roles'
  | 'Tenants'
  | 'Subscriptions'
  | 'SLOs'
  | 'AccessReviewCampaigns'
  | 'SoDRules'
  | 'AbacPolicies'
  | 'ConditionalPolicies'
  | 'Wallets'
  | 'Disputes'
  | 'PromoCodes'
  | 'Orders'
  | 'AuditEntries'
  | 'Assets'
  | 'AssetStorage'
  | 'AssetDownloads'
  | 'AssetTransformations'
  | 'AiRequests'
  | 'AiTokens';

export interface ResourcesSetQuotaInput {
  softLimit?: number | null;
  hardLimit?: number | null;
  period?: ResourcesResourceQuotaPeriod;
  isActive?: boolean;
  resetTime?: string | null;
}

export interface ResourcesSetResourceMetadataInput {
  value?: string | null;
  dataType?: string | null;
  description?: string | null;
  category?: string | null;
  displayOrder?: number | null;
}

export interface ResourcesSetResourceSettingsInput {
  value?: string | null;
  defaultValue?: string | null;
  dataType?: string | null;
  description?: string | null;
  category?: string | null;
  allowUserOverride?: boolean | null;
  displayOrder?: number | null;
  validationRules?: string | null;
}

export interface ResourcesSetUserResourceSettingsInput {
  value?: string | null;
}

export interface ResourcesToggleResourceQuotaInput {
  isActive?: boolean;
}

export type ResourcesTrendGranularity = 'Daily' | 'Weekly' | 'Monthly';

export interface ResourcesUsageRecord {
  isGlobal?: boolean;
  isNew?: boolean;
  isDeleted?: boolean;
  domainEvents?: Array<CQRSIDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string | null;
  tenantId?: string | null;
  type?: ResourcesResourceUsageType;
  count?: number;
  usageAmount?: number;
  periodStart?: string;
  periodEnd?: string;
  averagePerDay?: number | null;
  peakUsage?: number | null;
  peakUsageDate?: string | null;
  metadata?: string | null;
  source?: string | null;
  userId?: string | null;
  resourceId?: string | null;
  resourceQuotaId?: string | null;
}

export interface ResourcesUsageTrendDataPoint {
  period?: string;
  totalUsage?: number;
  tenantCount?: number;
}

export interface ResourcesUsageTrendsResult {
  type?: ResourcesResourceUsageType;
  startDate?: string;
  endDate?: string;
  granularity?: ResourcesTrendGranularity;
  dataPoints?: Array<ResourcesUsageTrendDataPoint> | null;
}

export interface SocialBlogBlogPost {
  id?: string;
  authorId?: string;
  tenantId?: string | null;
  title?: string | null;
  slug?: string | null;
  excerpt?: string | null;
  content?: string | null;
  coverImageUrl?: string | null;
  status?: SocialBlogBlogPostStatus;
  publishedAt?: string | null;
  isFeatured?: boolean;
  allowComments?: boolean;
  viewsCount?: number;
  likesCount?: number;
  commentsCount?: number;
  readTimeMinutes?: number;
  createdAt?: string;
  updatedAt?: string;
}

export type SocialBlogBlogPostStatus = 'Draft' | 'Published' | 'Archived';

export interface SocialBlogCreateBlogPostInput {
  authorId?: string;
  title?: string | null;
  slug?: string | null;
  content?: string | null;
  tenantId?: string | null;
}

export interface SocialFeedAddFeedItemInput {
  userId?: string;
  contentId?: string;
  contentType?: SocialFeedFeedContentType;
  authorId?: string;
  reason?: SocialFeedFeedItemReason;
  contentCreatedAt?: string | null;
  relevanceScore?: number;
}

export type SocialFeedFeedContentType = 'Post' | 'BlogPost' | 'CourseReview' | 'ProjectUpdate' | 'Achievement' | 'CourseCompletion';

export interface SocialFeedFeedItem {
  id?: string;
  userId?: string;
  contentId?: string;
  contentType?: SocialFeedFeedContentType;
  authorId?: string;
  relevanceScore?: number;
  reason?: SocialFeedFeedItemReason;
  isRead?: boolean;
  isHidden?: boolean;
  contentCreatedAt?: string;
  createdAt?: string;
}

export type SocialFeedFeedItemReason = 'Following' | 'Trending' | 'Recommended' | 'Mentioned' | 'Replied' | 'Liked' | 'InNetwork';

export interface SocialGroupsApproveSocialGroupMemberInput {
  approvedByUserId?: string;
}

export interface SocialGroupsChangeSocialGroupMemberRoleInput {
  role?: SocialGroupsSocialGroupMemberRole;
}

export interface SocialGroupsCreateSocialGroupInput {
  ownerId?: string;
  name?: string | null;
  slug?: string | null;
  type?: SocialGroupsSocialGroupType;
  visibility?: SocialGroupsSocialGroupVisibility;
  description?: string | null;
  tenantId?: string | null;
}

export interface SocialGroupsJoinSocialGroupInput {
  userId?: string;
  requestedRole?: SocialGroupsSocialGroupMemberRole;
}

export interface SocialGroupsSocialGroup {
  id?: string;
  tenantId?: string | null;
  ownerId?: string;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  type?: SocialGroupsSocialGroupType;
  visibility?: SocialGroupsSocialGroupVisibility;
  status?: SocialGroupsSocialGroupStatus;
  memberCount?: number;
  pendingMemberCount?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface SocialGroupsSocialGroupMember {
  id?: string;
  groupId?: string;
  userId?: string;
  role?: SocialGroupsSocialGroupMemberRole;
  status?: SocialGroupsSocialGroupMembershipStatus;
  requestedAt?: string;
  joinedAt?: string | null;
  approvedByUserId?: string | null;
  removedAt?: string | null;
}

export type SocialGroupsSocialGroupMemberRole = 'Owner' | 'Admin' | 'Moderator' | 'Member';

export type SocialGroupsSocialGroupMembershipStatus = 'Pending' | 'Active' | 'Rejected' | 'Removed';

export type SocialGroupsSocialGroupStatus = 'Active' | 'Archived' | 'Suspended';

export type SocialGroupsSocialGroupType = 'StudyGroup' | 'ProjectTeam' | 'InterestCommunity' | 'CourseCohort' | 'Institution' | 'GameJamTeam';

export type SocialGroupsSocialGroupVisibility = 'Public' | 'Private' | 'InviteOnly';

export interface SocialGroupsUpdateSocialGroupInput {
  name?: string | null;
  slug?: string | null;
  type?: SocialGroupsSocialGroupType;
  visibility?: SocialGroupsSocialGroupVisibility;
  description?: string | null;
}

export interface SocialProfilesAddProfilePortfolioItemBody {
  title?: string | null;
  projectId?: string | null;
  description?: string | null;
  url?: string | null;
  imageUrl?: string | null;
  isPinned?: boolean;
  displayOrder?: number;
}

export interface SocialProfilesAddProfileSkillBody {
  name?: string | null;
  proficiency?: SocialProfilesProfileSkillProficiency;
  displayOrder?: number;
}

export type SocialProfilesProfileAvailabilityStatus = 'NotSet' | 'OpenToWork' | 'OpenToCollaborate' | 'Busy' | 'Hidden';

export interface SocialProfilesProfilePortfolioItem {
  id?: string;
  profileId?: string;
  projectId?: string | null;
  title?: string | null;
  description?: string | null;
  url?: string | null;
  imageUrl?: string | null;
  isPinned?: boolean;
  displayOrder?: number;
}

export interface SocialProfilesProfileSkill {
  id?: string;
  profileId?: string;
  name?: string | null;
  proficiency?: SocialProfilesProfileSkillProficiency;
  displayOrder?: number;
}

export type SocialProfilesProfileSkillProficiency = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';

export type SocialProfilesProfileVisibility = 'Private' | 'Connections' | 'Public';

export interface SocialProfilesSocialProfile {
  id?: string;
  userId?: string;
  handle?: string | null;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  bannerUrl?: string | null;
  headline?: string | null;
  location?: string | null;
  timeZone?: string | null;
  websiteUrl?: string | null;
  socialLinksJson?: string | null;
  visibility?: SocialProfilesProfileVisibility;
  availabilityStatus?: SocialProfilesProfileAvailabilityStatus;
  showActivity?: boolean;
  showPortfolio?: boolean;
  showSkills?: boolean;
  verifiedAt?: string | null;
  completenessScore?: number;
  followerCount?: number;
  followingCount?: number;
  postCount?: number;
  projectCount?: number;
  skills?: Array<SocialProfilesProfileSkill> | null;
  portfolioItems?: Array<SocialProfilesProfilePortfolioItem> | null;
}

export interface SocialProfilesUpdateProfilePortfolioItemBody {
  title?: string | null;
  description?: string | null;
  url?: string | null;
  imageUrl?: string | null;
  isPinned?: boolean;
  displayOrder?: number;
}

export interface SocialProfilesUpdateProfilePrivacyBody {
  visibility?: SocialProfilesProfileVisibility;
  showActivity?: boolean;
  showPortfolio?: boolean;
  showSkills?: boolean;
}

export interface SocialProfilesUpdateProfileStatsBody {
  followerCount?: number;
  followingCount?: number;
  postCount?: number;
  projectCount?: number;
}

export interface SocialProfilesUpdateSocialProfileBody {
  handle?: string | null;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  bannerUrl?: string | null;
  headline?: string | null;
  location?: string | null;
  timeZone?: string | null;
  websiteUrl?: string | null;
  socialLinksJson?: string | null;
  availabilityStatus?: SocialProfilesProfileAvailabilityStatus;
}

export interface SocialReactionsReaction {
  id?: string;
  userId?: string;
  targetId?: string;
  targetType?: SocialReactionsReactionTargetType;
  type?: SocialReactionsReactionType;
  createdAt?: string;
  updatedAt?: string;
}

export type SocialReactionsReactionTargetType = 'Post' | 'Comment' | 'BlogPost' | 'CourseReview' | 'Discussion' | 'Reply';

export type SocialReactionsReactionType = 'Like' | 'Love' | 'Insightful' | 'Celebrate' | 'Support' | 'Curious';

export interface SocialReactionsRemoveReactionInput {
  userId?: string;
  targetId?: string;
  targetType?: SocialReactionsReactionTargetType;
}

export interface SocialReactionsSetReactionInput {
  userId?: string;
  targetId?: string;
  targetType?: SocialReactionsReactionTargetType;
  type?: SocialReactionsReactionType;
}

export interface SocialReactionsTargetReactionSummary {
  targetId?: string;
  targetType?: SocialReactionsReactionTargetType;
  counts?: { Like?: number; Love?: number; Insightful?: number; Celebrate?: number; Support?: number; Curious?: number } | null;
  total?: number;
}

export interface TagsCreateTagProficiencyInput {
  name?: string | null;
  type?: TagsTagType;
  proficiencyLevel?: TagsSkillProficiencyLevel;
  description?: string | null;
  color?: string | null;
  icon?: string | null;
}

export interface TagsCreateTagRelationshipInput {
  sourceId?: string;
  targetId?: string;
  type?: TagsTagRelationshipType;
  weight?: number | null;
  metadata?: string | null;
}

export interface TagsCreateTagInput {
  name?: string | null;
  type?: TagsTagType;
  description?: string | null;
  color?: string | null;
  icon?: string | null;
  tenantId?: string | null;
}

export type TagsSkillProficiencyLevel = 'Beginner' | 'Elementary' | 'Intermediate' | 'Advanced' | 'Expert' | 'Master';

export interface TagsTag {
  id?: string;
  name?: string | null;
  description?: string | null;
  type?: TagsTagType;
  color?: string | null;
  icon?: string | null;
  isActive?: boolean;
  tenantId?: string | null;
}

export interface TagsTagProficiency {
  id?: string;
  name?: string | null;
  description?: string | null;
  type?: TagsTagType;
  proficiencyLevel?: TagsSkillProficiencyLevel;
  color?: string | null;
  icon?: string | null;
  isActive?: boolean;
}

export interface TagsTagRelationship {
  id?: string;
  sourceId?: string;
  targetId?: string;
  type?: TagsTagRelationshipType;
  weight?: number | null;
  metadata?: string | null;
}

export type TagsTagRelationshipType = 'Related' | 'Parent' | 'Child' | 'Requires' | 'Suggested';

export type TagsTagType = 'Skill' | 'Topic' | 'Technology' | 'Difficulty' | 'Category' | 'Industry' | 'Certification';

export interface TagsUpdateTagInput {
  name?: string | null;
  description?: string | null;
  color?: string | null;
  icon?: string | null;
  isActive?: boolean | null;
}

// Zod Schema Declarations (to handle circular references)
export let BillingCycleSchema: z.ZodType<BillingCycle>;
export let BulkOperationErrorSchema: z.ZodType<BulkOperationError>;
export let CommercePaymentsTaxJurisdictionDtoSchema: z.ZodType<CommercePaymentsTaxJurisdictionDto>;
export let CommercePaymentsTaxRuleDtoSchema: z.ZodType<CommercePaymentsTaxRuleDto>;
export let ContentStatusSchema: z.ZodType<ContentStatus>;
export let ContentVisibilitySchema: z.ZodType<ContentVisibility>;
export let IdentityTenantsTenantSettingsDtoSchema: z.ZodType<IdentityTenantsTenantSettingsDto>;
export let KeyValuePairStringAuthenticationExtensionsPRFValuesSchema: z.ZodType<KeyValuePairStringAuthenticationExtensionsPRFValues>;
export let MoneySchema: z.ZodType<Money>;
export let PagedResult1GameGuildCommerceProductsPromoCodeDtoGameGuildCommerceProductsVersion100PagedResultPromoCodeDtoSchema: z.ZodType<PagedResult1GameGuildCommerceProductsPromoCodeDtoGameGuildCommerceProductsVersion100PagedResultPromoCodeDto>;
export let PagedResult1GameGuildIdentityTenantsTenantGameGuildIdentityTenantsVersion100PagedResultTenantSchema: z.ZodType<PagedResult1GameGuildIdentityTenantsTenantGameGuildIdentityTenantsVersion100PagedResultTenant>;
export let PagedResult1GameGuildIdentityTenantsTenantAuditLogEntryGameGuildIdentityTenantsVersion100PagedResultTenantAuditLogEntrySchema: z.ZodType<PagedResult1GameGuildIdentityTenantsTenantAuditLogEntryGameGuildIdentityTenantsVersion100PagedResultTenantAuditLogEntry>;
export let PagedResult1GameGuildIdentityUsersUserDtoGameGuildIdentityUsersVersion100PagedResultUserDtoSchema: z.ZodType<PagedResult1GameGuildIdentityUsersUserDtoGameGuildIdentityUsersVersion100PagedResultUserDto>;
export let PagedResult1GameGuildIdentityUsersUserNotificationDtoGameGuildIdentityUsersVersion100PagedResultUserNotificationDtoSchema: z.ZodType<PagedResult1GameGuildIdentityUsersUserNotificationDtoGameGuildIdentityUsersVersion100PagedResultUserNotificationDto>;
export let PagedResult1GameGuildIdentityUsersUserProfileDtoGameGuildIdentityUsersVersion100PagedResultUserProfileDtoSchema: z.ZodType<PagedResult1GameGuildIdentityUsersUserProfileDtoGameGuildIdentityUsersVersion100PagedResultUserProfileDto>;
export let ProgramCategorySchema: z.ZodType<ProgramCategory>;
export let TenantInfoSchema: z.ZodType<TenantInfo>;
export let AIAiChatMessageSchema: z.ZodType<AIAiChatMessage>;
export let AIAiChatInputSchema: z.ZodType<AIAiChatInput>;
export let AIAiCompletionOutputSchema: z.ZodType<AIAiCompletionOutput>;
export let AIAiConversationHistoryEntrySchema: z.ZodType<AIAiConversationHistoryEntry>;
export let AIAiGenerateInputSchema: z.ZodType<AIAiGenerateInput>;
export let AIAiGeneratedContentDraftInputSchema: z.ZodType<AIAiGeneratedContentDraftInput>;
export let AIAiGeneratedContentKindSchema: z.ZodType<AIAiGeneratedContentKind>;
export let AIAiGeneratedContentInputSchema: z.ZodType<AIAiGeneratedContentInput>;
export let AIAiPromptTemplateSchema: z.ZodType<AIAiPromptTemplate>;
export let AIAiPromptTemplateGenerateInputSchema: z.ZodType<AIAiPromptTemplateGenerateInput>;
export let AIAiPromptTemplateRenderInputSchema: z.ZodType<AIAiPromptTemplateRenderInput>;
export let AIAiPromptTemplateRenderOutputSchema: z.ZodType<AIAiPromptTemplateRenderOutput>;
export let AIAiProviderStatusSchema: z.ZodType<AIAiProviderStatus>;
export let AIAiQuotaStatusSchema: z.ZodType<AIAiQuotaStatus>;
export let AIAiQuotaStatusOutputSchema: z.ZodType<AIAiQuotaStatusOutput>;
export let AIAiStatusOutputSchema: z.ZodType<AIAiStatusOutput>;
export let AIAiUsageSchema: z.ZodType<AIAiUsage>;
export let AICreateAiPromptTemplateInputSchema: z.ZodType<AICreateAiPromptTemplateInput>;
export let AIUpdateAiPromptTemplateInputSchema: z.ZodType<AIUpdateAiPromptTemplateInput>;
export let APIControllersApplicationDetailsSchema: z.ZodType<APIControllersApplicationDetails>;
export let APIControllersApplicationInfoOutputSchema: z.ZodType<APIControllersApplicationInfoOutput>;
export let APIControllersBuildDetailsSchema: z.ZodType<APIControllersBuildDetails>;
export let APIControllersDependencyHealthItemSchema: z.ZodType<APIControllersDependencyHealthItem>;
export let APIControllersDependencyHealthOutputSchema: z.ZodType<APIControllersDependencyHealthOutput>;
export let APIControllersHealthinessOutputSchema: z.ZodType<APIControllersHealthinessOutput>;
export let APIControllersHealthinessResponseItemSchema: z.ZodType<APIControllersHealthinessResponseItem>;
export let APIControllersLivenessOutputSchema: z.ZodType<APIControllersLivenessOutput>;
export let APIControllersProcessDetailsSchema: z.ZodType<APIControllersProcessDetails>;
export let APIControllersReadinessOutputSchema: z.ZodType<APIControllersReadinessOutput>;
export let APIControllersRuntimeDetailsSchema: z.ZodType<APIControllersRuntimeDetails>;
export let BulkOperationOutputSchema: z.ZodType<BulkOperationOutput>;
export let CQRSIDomainEventSchema: z.ZodType<CQRSIDomainEvent>;
export let CommerceBillingInvoicePaymentRetryResultSchema: z.ZodType<CommerceBillingInvoicePaymentRetryResult>;
export let CommerceBillingInvoiceStatusSchema: z.ZodType<CommerceBillingInvoiceStatus>;
export let CommercePaymentsAddFundsInputSchema: z.ZodType<CommercePaymentsAddFundsInput>;
export let CommercePaymentsCalculateTaxInputSchema: z.ZodType<CommercePaymentsCalculateTaxInput>;
export let CommercePaymentsCreateTaxJurisdictionInputSchema: z.ZodType<CommercePaymentsCreateTaxJurisdictionInput>;
export let CommercePaymentsCreateTaxRuleInputSchema: z.ZodType<CommercePaymentsCreateTaxRuleInput>;
export let CommercePaymentsCreateWalletInputSchema: z.ZodType<CommercePaymentsCreateWalletInput>;
export let CommercePaymentsCustomerTypeSchema: z.ZodType<CommercePaymentsCustomerType>;
export let CommercePaymentsDeductFundsInputSchema: z.ZodType<CommercePaymentsDeductFundsInput>;
export let CommercePaymentsLockWalletInputSchema: z.ZodType<CommercePaymentsLockWalletInput>;
export let CommercePaymentsModelsFreezeWalletInputSchema: z.ZodType<CommercePaymentsModelsFreezeWalletInput>;
export let CommercePaymentsModelsPatchWalletInputSchema: z.ZodType<CommercePaymentsModelsPatchWalletInput>;
export let CommercePaymentsPatchTaxJurisdictionInputSchema: z.ZodType<CommercePaymentsPatchTaxJurisdictionInput>;
export let CommercePaymentsPatchTaxRuleInputSchema: z.ZodType<CommercePaymentsPatchTaxRuleInput>;
export let CommercePaymentsPaymentCancellationResultSchema: z.ZodType<CommercePaymentsPaymentCancellationResult>;
export let CommercePaymentsPaymentResultSchema: z.ZodType<CommercePaymentsPaymentResult>;
export let CommercePaymentsPaymentRetryResultSchema: z.ZodType<CommercePaymentsPaymentRetryResult>;
export let CommercePaymentsPaymentStatusSchema: z.ZodType<CommercePaymentsPaymentStatus>;
export let CommercePaymentsPaymentsControllerCancelPaymentInputSchema: z.ZodType<CommercePaymentsPaymentsControllerCancelPaymentInput>;
export let CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInputSchema: z.ZodType<CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInput>;
export let CommercePaymentsPaymentsControllerCreateSetupIntentInputSchema: z.ZodType<CommercePaymentsPaymentsControllerCreateSetupIntentInput>;
export let CommercePaymentsPaymentsControllerCreateSetupIntentOutputSchema: z.ZodType<CommercePaymentsPaymentsControllerCreateSetupIntentOutput>;
export let CommercePaymentsPaymentsControllerProcessPaymentInputSchema: z.ZodType<CommercePaymentsPaymentsControllerProcessPaymentInput>;
export let CommercePaymentsPaymentsControllerRefundInputSchema: z.ZodType<CommercePaymentsPaymentsControllerRefundInput>;
export let CommercePaymentsProcessRefundResultSchema: z.ZodType<CommercePaymentsProcessRefundResult>;
export let CommercePaymentsTaxBreakdownSchema: z.ZodType<CommercePaymentsTaxBreakdown>;
export let CommercePaymentsTaxCalculationResultSchema: z.ZodType<CommercePaymentsTaxCalculationResult>;
export let CommercePaymentsTaxExemptionValidationResultSchema: z.ZodType<CommercePaymentsTaxExemptionValidationResult>;
export let CommercePaymentsTaxJurisdictionSchema: z.ZodType<CommercePaymentsTaxJurisdiction>;
export let CommercePaymentsTaxJurisdictionTypeSchema: z.ZodType<CommercePaymentsTaxJurisdictionType>;
export let CommercePaymentsTaxRateSchema: z.ZodType<CommercePaymentsTaxRate>;
export let CommercePaymentsTaxRuleSchema: z.ZodType<CommercePaymentsTaxRule>;
export let CommercePaymentsTaxRuleTypeSchema: z.ZodType<CommercePaymentsTaxRuleType>;
export let CommercePaymentsTaxTypeSchema: z.ZodType<CommercePaymentsTaxType>;
export let CommercePaymentsTransactionStatusSchema: z.ZodType<CommercePaymentsTransactionStatus>;
export let CommercePaymentsTransferFundsInputSchema: z.ZodType<CommercePaymentsTransferFundsInput>;
export let CommercePaymentsTransferResultSchema: z.ZodType<CommercePaymentsTransferResult>;
export let CommercePaymentsUserWalletSchema: z.ZodType<CommercePaymentsUserWallet>;
export let CommercePaymentsValidateTaxExemptionInputSchema: z.ZodType<CommercePaymentsValidateTaxExemptionInput>;
export let CommercePaymentsWalletTransactionSchema: z.ZodType<CommercePaymentsWalletTransaction>;
export let CommercePaymentsWalletTransactionTypeSchema: z.ZodType<CommercePaymentsWalletTransactionType>;
export let CommerceProductsAppliedPromoCodeSchema: z.ZodType<CommerceProductsAppliedPromoCode>;
export let CommerceProductsApplyPromoCodesInputSchema: z.ZodType<CommerceProductsApplyPromoCodesInput>;
export let CommerceProductsBatchCreateProductsInputSchema: z.ZodType<CommerceProductsBatchCreateProductsInput>;
export let CommerceProductsBatchProductCreateItemSchema: z.ZodType<CommerceProductsBatchProductCreateItem>;
export let CommerceProductsCheckMultipleAccessInputSchema: z.ZodType<CommerceProductsCheckMultipleAccessInput>;
export let CommerceProductsCreateProductInputSchema: z.ZodType<CommerceProductsCreateProductInput>;
export let CommerceProductsCreatePromoCodeInputSchema: z.ZodType<CommerceProductsCreatePromoCodeInput>;
export let CommerceProductsEntitlementCheckResultSchema: z.ZodType<CommerceProductsEntitlementCheckResult>;
export let CommerceProductsEntitlementInfoSchema: z.ZodType<CommerceProductsEntitlementInfo>;
export let CommerceProductsGrantEntitlementInputSchema: z.ZodType<CommerceProductsGrantEntitlementInput>;
export let CommerceProductsPatchProductInputSchema: z.ZodType<CommerceProductsPatchProductInput>;
export let CommerceProductsPatchPromoCodeInputSchema: z.ZodType<CommerceProductsPatchPromoCodeInput>;
export let CommerceProductsProductAcquisitionTypeSchema: z.ZodType<CommerceProductsProductAcquisitionType>;
export let CommerceProductsProductSchema: z.ZodType<CommerceProductsProduct>;
export let CommerceProductsProductPricingSchema: z.ZodType<CommerceProductsProductPricing>;
export let CommerceProductsProductTypeSchema: z.ZodType<CommerceProductsProductType>;
export let CommerceProductsPromoCodeApplicationResultSchema: z.ZodType<CommerceProductsPromoCodeApplicationResult>;
export let CommerceProductsPromoCodeSchema: z.ZodType<CommerceProductsPromoCode>;
export let CommerceProductsPromoCodeTypeSchema: z.ZodType<CommerceProductsPromoCodeType>;
export let CommerceProductsPromoCodeUsageSchema: z.ZodType<CommerceProductsPromoCodeUsage>;
export let CommerceProductsPromoCodeValidationResultSchema: z.ZodType<CommerceProductsPromoCodeValidationResult>;
export let CommerceProductsRejectedPromoCodeSchema: z.ZodType<CommerceProductsRejectedPromoCode>;
export let CommerceProductsRevokeEntitlementInputSchema: z.ZodType<CommerceProductsRevokeEntitlementInput>;
export let CommerceProductsUpdateProductInputSchema: z.ZodType<CommerceProductsUpdateProductInput>;
export let CommerceProductsUpdatePromoCodeInputSchema: z.ZodType<CommerceProductsUpdatePromoCodeInput>;
export let CommerceProductsValidatePromoCodeInputSchema: z.ZodType<CommerceProductsValidatePromoCodeInput>;
export let CommerceSubscriptionsBillingHistorySchema: z.ZodType<CommerceSubscriptionsBillingHistory>;
export let CommerceSubscriptionsCancellationReasonSchema: z.ZodType<CommerceSubscriptionsCancellationReason>;
export let CommerceSubscriptionsSubscriptionSchema: z.ZodType<CommerceSubscriptionsSubscription>;
export let CommerceSubscriptionsSubscriptionDowngradeResultSchema: z.ZodType<CommerceSubscriptionsSubscriptionDowngradeResult>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerAutoRenewInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerAutoRenewInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerCancelInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerCancelInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerDowngradeInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerDowngradeInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerEndTrialInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerEndTrialInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerExternalIdsInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerExternalIdsInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerPauseSubscriptionInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerPauseSubscriptionInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerStartTrialInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerStartTrialInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerSuspendInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerSuspendInput>;
export let CommerceSubscriptionsSubscriptionLifecycleControllerUpgradeInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionLifecycleControllerUpgradeInput>;
export let CommerceSubscriptionsSubscriptionPlanSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlan>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerCloneSubscriptionPlanInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerCloneSubscriptionPlanInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerSetExternalIdInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerSetExternalIdInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerSetFeaturedInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerSetFeaturedInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateDetailsInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateDetailsInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateFeaturesInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateFeaturesInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateLimitsInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateLimitsInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdatePricingInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdatePricingInput>;
export let CommerceSubscriptionsSubscriptionPlanOperationsControllerValidateLimitsInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlanOperationsControllerValidateLimitsInput>;
export let CommerceSubscriptionsSubscriptionPlansCrudControllerComparePlansInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlansCrudControllerComparePlansInput>;
export let CommerceSubscriptionsSubscriptionPlansCrudControllerCreatePlanInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlansCrudControllerCreatePlanInput>;
export let CommerceSubscriptionsSubscriptionPlansCrudControllerPutSubscriptionPlanInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionPlansCrudControllerPutSubscriptionPlanInput>;
export let CommerceSubscriptionsSubscriptionStatusSchema: z.ZodType<CommerceSubscriptionsSubscriptionStatus>;
export let CommerceSubscriptionsSubscriptionUpgradeResultSchema: z.ZodType<CommerceSubscriptionsSubscriptionUpgradeResult>;
export let CommerceSubscriptionsSubscriptionUsageSchema: z.ZodType<CommerceSubscriptionsSubscriptionUsage>;
export let CommerceSubscriptionsSubscriptionsControllerCreateSubscriptionInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionsControllerCreateSubscriptionInput>;
export let CommerceSubscriptionsSubscriptionsControllerPatchSubscriptionInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionsControllerPatchSubscriptionInput>;
export let CommerceSubscriptionsSubscriptionsControllerPutSubscriptionInputSchema: z.ZodType<CommerceSubscriptionsSubscriptionsControllerPutSubscriptionInput>;
export let ComplianceFERPACompleteFerpaInspectionRequestBodySchema: z.ZodType<ComplianceFERPACompleteFerpaInspectionRequestBody>;
export let ComplianceFERPAEducationRecordKindSchema: z.ZodType<ComplianceFERPAEducationRecordKind>;
export let ComplianceFERPAFerpaDirectoryInformationPolicySchema: z.ZodType<ComplianceFERPAFerpaDirectoryInformationPolicy>;
export let ComplianceFERPAFerpaDisclosureBasisSchema: z.ZodType<ComplianceFERPAFerpaDisclosureBasis>;
export let ComplianceFERPAFerpaDisclosureConsentSchema: z.ZodType<ComplianceFERPAFerpaDisclosureConsent>;
export let ComplianceFERPAFerpaDisclosureLogSchema: z.ZodType<ComplianceFERPAFerpaDisclosureLog>;
export let ComplianceFERPAFerpaEducationRecordSchema: z.ZodType<ComplianceFERPAFerpaEducationRecord>;
export let ComplianceFERPAFerpaInspectionInputSchema: z.ZodType<ComplianceFERPAFerpaInspectionInput>;
export let ComplianceFERPAFerpaRecordProtectionLevelSchema: z.ZodType<ComplianceFERPAFerpaRecordProtectionLevel>;
export let ComplianceFERPAFerpaRequestStatusSchema: z.ZodType<ComplianceFERPAFerpaRequestStatus>;
export let ComplianceFERPAGrantFerpaDisclosureConsentCommandSchema: z.ZodType<ComplianceFERPAGrantFerpaDisclosureConsentCommand>;
export let ComplianceFERPARecordFerpaDisclosureCommandSchema: z.ZodType<ComplianceFERPARecordFerpaDisclosureCommand>;
export let ComplianceFERPARegisterEducationRecordCommandSchema: z.ZodType<ComplianceFERPARegisterEducationRecordCommand>;
export let ComplianceFERPASubmitFerpaInspectionRequestCommandSchema: z.ZodType<ComplianceFERPASubmitFerpaInspectionRequestCommand>;
export let ComplianceFERPAUpsertDirectoryInformationPolicyCommandSchema: z.ZodType<ComplianceFERPAUpsertDirectoryInformationPolicyCommand>;
export let ContentPagesContentResourceSchema: z.ZodType<ContentPagesContentResource>;
export let ContentPagesContentResourceStatusSchema: z.ZodType<ContentPagesContentResourceStatus>;
export let ContentPagesContentResourceTypeSchema: z.ZodType<ContentPagesContentResourceType>;
export let ContentPagesCreateContentResourceSchema: z.ZodType<ContentPagesCreateContentResource>;
export let ContentPagesCreateMarketingLeadSchema: z.ZodType<ContentPagesCreateMarketingLead>;
export let ContentPagesCreatePageSchema: z.ZodType<ContentPagesCreatePage>;
export let ContentPagesCreatePageSectionSchema: z.ZodType<ContentPagesCreatePageSection>;
export let ContentPagesMarketingLeadSchema: z.ZodType<ContentPagesMarketingLead>;
export let ContentPagesOpenGraphMetadataSchema: z.ZodType<ContentPagesOpenGraphMetadata>;
export let ContentPagesPageSchema: z.ZodType<ContentPagesPage>;
export let ContentPagesPageSectionSchema: z.ZodType<ContentPagesPageSection>;
export let ContentPagesPageStatusSchema: z.ZodType<ContentPagesPageStatus>;
export let ContentPagesPageTypeSchema: z.ZodType<ContentPagesPageType>;
export let ContentPagesSectionTypeSchema: z.ZodType<ContentPagesSectionType>;
export let ContentPagesSitemapEntrySchema: z.ZodType<ContentPagesSitemapEntry>;
export let ContentPagesUpdateContentResourceSchema: z.ZodType<ContentPagesUpdateContentResource>;
export let ContentPagesUpdatePageSchema: z.ZodType<ContentPagesUpdatePage>;
export let ContentPagesUpdatePageSectionSchema: z.ZodType<ContentPagesUpdatePageSection>;
export let FeaturesBulkEvaluationInputSchema: z.ZodType<FeaturesBulkEvaluationInput>;
export let FeaturesCapabilityAuditLogSchema: z.ZodType<FeaturesCapabilityAuditLog>;
export let FeaturesCapabilityCheckOutputSchema: z.ZodType<FeaturesCapabilityCheckOutput>;
export let FeaturesCreateFeatureInputSchema: z.ZodType<FeaturesCreateFeatureInput>;
export let FeaturesFeatureContextSchema: z.ZodType<FeaturesFeatureContext>;
export let FeaturesFeatureEvaluationInputSchema: z.ZodType<FeaturesFeatureEvaluationInput>;
export let FeaturesFeatureFlagSchema: z.ZodType<FeaturesFeatureFlag>;
export let FeaturesFeatureFlagTargetSchema: z.ZodType<FeaturesFeatureFlagTarget>;
export let FeaturesFeatureFlagTypeSchema: z.ZodType<FeaturesFeatureFlagType>;
export let FeaturesSetCapabilityOverrideInputSchema: z.ZodType<FeaturesSetCapabilityOverrideInput>;
export let FeaturesToggleFeatureInputSchema: z.ZodType<FeaturesToggleFeatureInput>;
export let FeaturesUpdateFeatureInputSchema: z.ZodType<FeaturesUpdateFeatureInput>;
export let Fido2NetLibAssertionOptionsSchema: z.ZodType<Fido2NetLibAssertionOptions>;
export let Fido2NetLibAuthenticatorSelectionSchema: z.ZodType<Fido2NetLibAuthenticatorSelection>;
export let Fido2NetLibCredentialCreateOptionsSchema: z.ZodType<Fido2NetLibCredentialCreateOptions>;
export let Fido2NetLibFido2UserSchema: z.ZodType<Fido2NetLibFido2User>;
export let Fido2NetLibPubKeyCredParamSchema: z.ZodType<Fido2NetLibPubKeyCredParam>;
export let Fido2NetLibPublicKeyCredentialRpEntitySchema: z.ZodType<Fido2NetLibPublicKeyCredentialRpEntity>;
export let GameJamsAddJamCriteriaInputSchema: z.ZodType<GameJamsAddJamCriteriaInput>;
export let GameJamsCreateJamInputSchema: z.ZodType<GameJamsCreateJamInput>;
export let GameJamsJamCriteriaSchema: z.ZodType<GameJamsJamCriteria>;
export let GameJamsJamSchema: z.ZodType<GameJamsJam>;
export let GameJamsJamScoreSchema: z.ZodType<GameJamsJamScore>;
export let GameJamsJamStatusSchema: z.ZodType<GameJamsJamStatus>;
export let GameJamsJamSubmissionSchema: z.ZodType<GameJamsJamSubmission>;
export let GameJamsScoreJamSubmissionInputSchema: z.ZodType<GameJamsScoreJamSubmissionInput>;
export let GameJamsSubmitJamEntryInputSchema: z.ZodType<GameJamsSubmitJamEntryInput>;
export let IdentityAuthenticationApiKeySchema: z.ZodType<IdentityAuthenticationApiKey>;
export let IdentityAuthenticationBackupCodesOutputSchema: z.ZodType<IdentityAuthenticationBackupCodesOutput>;
export let IdentityAuthenticationBackupCodesStatusOutputSchema: z.ZodType<IdentityAuthenticationBackupCodesStatusOutput>;
export let IdentityAuthenticationBeginWebAuthnAuthenticationInputSchema: z.ZodType<IdentityAuthenticationBeginWebAuthnAuthenticationInput>;
export let IdentityAuthenticationBeginWebAuthnRegistrationInputSchema: z.ZodType<IdentityAuthenticationBeginWebAuthnRegistrationInput>;
export let IdentityAuthenticationCleanupKeysInputSchema: z.ZodType<IdentityAuthenticationCleanupKeysInput>;
export let IdentityAuthenticationCleanupResultSchema: z.ZodType<IdentityAuthenticationCleanupResult>;
export let IdentityAuthenticationClientCredentialsTokenOutputSchema: z.ZodType<IdentityAuthenticationClientCredentialsTokenOutput>;
export let IdentityAuthenticationCompleteMfaSetupInputSchema: z.ZodType<IdentityAuthenticationCompleteMfaSetupInput>;
export let IdentityAuthenticationCompletePasswordResetInputSchema: z.ZodType<IdentityAuthenticationCompletePasswordResetInput>;
export let IdentityAuthenticationCompleteWebAuthnAuthenticationInputSchema: z.ZodType<IdentityAuthenticationCompleteWebAuthnAuthenticationInput>;
export let IdentityAuthenticationCompleteWebAuthnRegistrationInputSchema: z.ZodType<IdentityAuthenticationCompleteWebAuthnRegistrationInput>;
export let IdentityAuthenticationConsumeMagicLinkInputSchema: z.ZodType<IdentityAuthenticationConsumeMagicLinkInput>;
export let IdentityAuthenticationCreateApiKeyCommandSchema: z.ZodType<IdentityAuthenticationCreateApiKeyCommand>;
export let IdentityAuthenticationCreateApiKeyOutputSchema: z.ZodType<IdentityAuthenticationCreateApiKeyOutput>;
export let IdentityAuthenticationCreateServiceAccountInputSchema: z.ZodType<IdentityAuthenticationCreateServiceAccountInput>;
export let IdentityAuthenticationDeviceInfoSchema: z.ZodType<IdentityAuthenticationDeviceInfo>;
export let IdentityAuthenticationDisableMfaInputSchema: z.ZodType<IdentityAuthenticationDisableMfaInput>;
export let IdentityAuthenticationEmailVerificationOutputSchema: z.ZodType<IdentityAuthenticationEmailVerificationOutput>;
export let IdentityAuthenticationEmailVerificationResultSchema: z.ZodType<IdentityAuthenticationEmailVerificationResult>;
export let IdentityAuthenticationGitHubSignInOutputSchema: z.ZodType<IdentityAuthenticationGitHubSignInOutput>;
export let IdentityAuthenticationGoogleIdTokenInputSchema: z.ZodType<IdentityAuthenticationGoogleIdTokenInput>;
export let IdentityAuthenticationJwtKeyInfoSchema: z.ZodType<IdentityAuthenticationJwtKeyInfo>;
export let IdentityAuthenticationLocalSignInInputSchema: z.ZodType<IdentityAuthenticationLocalSignInInput>;
export let IdentityAuthenticationLocalSignUpInputSchema: z.ZodType<IdentityAuthenticationLocalSignUpInput>;
export let IdentityAuthenticationLocationInfoSchema: z.ZodType<IdentityAuthenticationLocationInfo>;
export let IdentityAuthenticationLockServiceAccountInputSchema: z.ZodType<IdentityAuthenticationLockServiceAccountInput>;
export let IdentityAuthenticationMagicLinkRequestResultSchema: z.ZodType<IdentityAuthenticationMagicLinkRequestResult>;
export let IdentityAuthenticationMfaConfigurationOutputSchema: z.ZodType<IdentityAuthenticationMfaConfigurationOutput>;
export let IdentityAuthenticationMfaMethodSchema: z.ZodType<IdentityAuthenticationMfaMethod>;
export let IdentityAuthenticationMfaMethodInfoSchema: z.ZodType<IdentityAuthenticationMfaMethodInfo>;
export let IdentityAuthenticationMfaMethodsOutputSchema: z.ZodType<IdentityAuthenticationMfaMethodsOutput>;
export let IdentityAuthenticationMfaSetupOutputSchema: z.ZodType<IdentityAuthenticationMfaSetupOutput>;
export let IdentityAuthenticationMfaSuccessOutputSchema: z.ZodType<IdentityAuthenticationMfaSuccessOutput>;
export let IdentityAuthenticationMfaVerificationOutputSchema: z.ZodType<IdentityAuthenticationMfaVerificationOutput>;
export let IdentityAuthenticationOAuth2ErrorOutputSchema: z.ZodType<IdentityAuthenticationOAuth2ErrorOutput>;
export let IdentityAuthenticationPasswordChangeInputSchema: z.ZodType<IdentityAuthenticationPasswordChangeInput>;
export let IdentityAuthenticationPasswordChangeResultSchema: z.ZodType<IdentityAuthenticationPasswordChangeResult>;
export let IdentityAuthenticationPasswordResetRequestResultSchema: z.ZodType<IdentityAuthenticationPasswordResetRequestResult>;
export let IdentityAuthenticationPasswordResetResultSchema: z.ZodType<IdentityAuthenticationPasswordResetResult>;
export let IdentityAuthenticationPatchServiceAccountInputSchema: z.ZodType<IdentityAuthenticationPatchServiceAccountInput>;
export let IdentityAuthenticationRefreshTokenInputSchema: z.ZodType<IdentityAuthenticationRefreshTokenInput>;
export let IdentityAuthenticationRequestMagicLinkInputSchema: z.ZodType<IdentityAuthenticationRequestMagicLinkInput>;
export let IdentityAuthenticationRequestPasswordResetInputSchema: z.ZodType<IdentityAuthenticationRequestPasswordResetInput>;
export let IdentityAuthenticationRevokeApiKeyInputSchema: z.ZodType<IdentityAuthenticationRevokeApiKeyInput>;
export let IdentityAuthenticationRevokeRefreshTokenInputSchema: z.ZodType<IdentityAuthenticationRevokeRefreshTokenInput>;
export let IdentityAuthenticationRiskLevelSchema: z.ZodType<IdentityAuthenticationRiskLevel>;
export let IdentityAuthenticationRotateKeyInputSchema: z.ZodType<IdentityAuthenticationRotateKeyInput>;
export let IdentityAuthenticationSecretRotationOutputSchema: z.ZodType<IdentityAuthenticationSecretRotationOutput>;
export let IdentityAuthenticationSendEmailVerificationInputSchema: z.ZodType<IdentityAuthenticationSendEmailVerificationInput>;
export let IdentityAuthenticationServiceAccountAuditEntrySchema: z.ZodType<IdentityAuthenticationServiceAccountAuditEntry>;
export let IdentityAuthenticationServiceAccountAuditLogOutputSchema: z.ZodType<IdentityAuthenticationServiceAccountAuditLogOutput>;
export let IdentityAuthenticationServiceAccountCreatedOutputSchema: z.ZodType<IdentityAuthenticationServiceAccountCreatedOutput>;
export let IdentityAuthenticationServiceAccountOutputSchema: z.ZodType<IdentityAuthenticationServiceAccountOutput>;
export let IdentityAuthenticationSessionOutputSchema: z.ZodType<IdentityAuthenticationSessionOutput>;
export let IdentityAuthenticationSessionSecurityAnalysisSchema: z.ZodType<IdentityAuthenticationSessionSecurityAnalysis>;
export let IdentityAuthenticationSessionSuccessOutputSchema: z.ZodType<IdentityAuthenticationSessionSuccessOutput>;
export let IdentityAuthenticationSessionTerminationOutputSchema: z.ZodType<IdentityAuthenticationSessionTerminationOutput>;
export let IdentityAuthenticationSignInOutputSchema: z.ZodType<IdentityAuthenticationSignInOutput>;
export let IdentityAuthenticationSmsMfaSetupInputSchema: z.ZodType<IdentityAuthenticationSmsMfaSetupInput>;
export let IdentityAuthenticationSmsMfaSetupOutputSchema: z.ZodType<IdentityAuthenticationSmsMfaSetupOutput>;
export let IdentityAuthenticationTrustDeviceInputSchema: z.ZodType<IdentityAuthenticationTrustDeviceInput>;
export let IdentityAuthenticationTrustedDeviceOutputSchema: z.ZodType<IdentityAuthenticationTrustedDeviceOutput>;
export let IdentityAuthenticationUpdateCredentialNameInputSchema: z.ZodType<IdentityAuthenticationUpdateCredentialNameInput>;
export let IdentityAuthenticationUpdateScopesInputSchema: z.ZodType<IdentityAuthenticationUpdateScopesInput>;
export let IdentityAuthenticationUserSchema: z.ZodType<IdentityAuthenticationUser>;
export let IdentityAuthenticationVerifyEmailInputSchema: z.ZodType<IdentityAuthenticationVerifyEmailInput>;
export let IdentityAuthenticationVerifyMfaInputSchema: z.ZodType<IdentityAuthenticationVerifyMfaInput>;
export let IdentityAuthenticationWeb3ChallengeInputSchema: z.ZodType<IdentityAuthenticationWeb3ChallengeInput>;
export let IdentityAuthenticationWeb3ChallengeOutputSchema: z.ZodType<IdentityAuthenticationWeb3ChallengeOutput>;
export let IdentityAuthenticationWeb3VerifyInputSchema: z.ZodType<IdentityAuthenticationWeb3VerifyInput>;
export let IdentityAuthenticationWebAuthnAuthenticationOptionsResultSchema: z.ZodType<IdentityAuthenticationWebAuthnAuthenticationOptionsResult>;
export let IdentityAuthenticationWebAuthnAuthenticationResultSchema: z.ZodType<IdentityAuthenticationWebAuthnAuthenticationResult>;
export let IdentityAuthenticationWebAuthnAuthenticatorTypeSchema: z.ZodType<IdentityAuthenticationWebAuthnAuthenticatorType>;
export let IdentityAuthenticationWebAuthnCredentialInfoSchema: z.ZodType<IdentityAuthenticationWebAuthnCredentialInfo>;
export let IdentityAuthenticationWebAuthnCredentialVerifyResultSchema: z.ZodType<IdentityAuthenticationWebAuthnCredentialVerifyResult>;
export let IdentityAuthenticationWebAuthnRegistrationOptionsResultSchema: z.ZodType<IdentityAuthenticationWebAuthnRegistrationOptionsResult>;
export let IdentityAuthenticationWebAuthnRegistrationResultSchema: z.ZodType<IdentityAuthenticationWebAuthnRegistrationResult>;
export let IdentityAuthenticationWebAuthnStatusOutputSchema: z.ZodType<IdentityAuthenticationWebAuthnStatusOutput>;
export let IdentityTenantsAddTenantMemberOutputSchema: z.ZodType<IdentityTenantsAddTenantMemberOutput>;
export let IdentityTenantsAddUserMembershipInputSchema: z.ZodType<IdentityTenantsAddUserMembershipInput>;
export let IdentityTenantsArchiveInputSchema: z.ZodType<IdentityTenantsArchiveInput>;
export let IdentityTenantsBulkActivateTenantsCommandSchema: z.ZodType<IdentityTenantsBulkActivateTenantsCommand>;
export let IdentityTenantsBulkArchiveTenantsCommandSchema: z.ZodType<IdentityTenantsBulkArchiveTenantsCommand>;
export let IdentityTenantsBulkCreateTenantItemSchema: z.ZodType<IdentityTenantsBulkCreateTenantItem>;
export let IdentityTenantsBulkCreateTenantsCommandSchema: z.ZodType<IdentityTenantsBulkCreateTenantsCommand>;
export let IdentityTenantsBulkDeactivateTenantsCommandSchema: z.ZodType<IdentityTenantsBulkDeactivateTenantsCommand>;
export let IdentityTenantsBulkDeleteTenantsCommandSchema: z.ZodType<IdentityTenantsBulkDeleteTenantsCommand>;
export let IdentityTenantsBulkPurgeTenantsCommandSchema: z.ZodType<IdentityTenantsBulkPurgeTenantsCommand>;
export let IdentityTenantsBulkUndeleteTenantsCommandSchema: z.ZodType<IdentityTenantsBulkUndeleteTenantsCommand>;
export let IdentityTenantsBulkUpdateTenantItemSchema: z.ZodType<IdentityTenantsBulkUpdateTenantItem>;
export let IdentityTenantsBulkUpdateTenantsCommandSchema: z.ZodType<IdentityTenantsBulkUpdateTenantsCommand>;
export let IdentityTenantsCreateTenantInputSchema: z.ZodType<IdentityTenantsCreateTenantInput>;
export let IdentityTenantsGetUserMembershipsOutputSchema: z.ZodType<IdentityTenantsGetUserMembershipsOutput>;
export let IdentityTenantsMembershipCountOutputSchema: z.ZodType<IdentityTenantsMembershipCountOutput>;
export let IdentityTenantsRecoverInputSchema: z.ZodType<IdentityTenantsRecoverInput>;
export let IdentityTenantsReplaceTenantMetadataInputSchema: z.ZodType<IdentityTenantsReplaceTenantMetadataInput>;
export let IdentityTenantsReplaceTenantSettingsInputSchema: z.ZodType<IdentityTenantsReplaceTenantSettingsInput>;
export let IdentityTenantsSlugValidationSchema: z.ZodType<IdentityTenantsSlugValidation>;
export let IdentityTenantsTenantSchema: z.ZodType<IdentityTenantsTenant>;
export let IdentityTenantsTenantAddressSchema: z.ZodType<IdentityTenantsTenantAddress>;
export let IdentityTenantsTenantAuditLogEntrySchema: z.ZodType<IdentityTenantsTenantAuditLogEntry>;
export let IdentityTenantsTenantBrandingSchema: z.ZodType<IdentityTenantsTenantBranding>;
export let IdentityTenantsTenantBusinessInfoSchema: z.ZodType<IdentityTenantsTenantBusinessInfo>;
export let IdentityTenantsTenantBusinessRulesSchema: z.ZodType<IdentityTenantsTenantBusinessRules>;
export let IdentityTenantsTenantContactInfoSchema: z.ZodType<IdentityTenantsTenantContactInfo>;
export let IdentityTenantsTenantCurrencySettingsSchema: z.ZodType<IdentityTenantsTenantCurrencySettings>;
export let IdentityTenantsTenantDomainSchema: z.ZodType<IdentityTenantsTenantDomain>;
export let IdentityTenantsTenantIntegrationSettingsSchema: z.ZodType<IdentityTenantsTenantIntegrationSettings>;
export let IdentityTenantsTenantMemberSchema: z.ZodType<IdentityTenantsTenantMember>;
export let IdentityTenantsTenantMetadataSchema: z.ZodType<IdentityTenantsTenantMetadata>;
export let IdentityTenantsTenantSecuritySettingsSchema: z.ZodType<IdentityTenantsTenantSecuritySettings>;
export let IdentityTenantsTenantSettingsSchema: z.ZodType<IdentityTenantsTenantSettings>;
export let IdentityTenantsTenantStatisticsSchema: z.ZodType<IdentityTenantsTenantStatistics>;
export let IdentityTenantsTenantSystemConfigurationSchema: z.ZodType<IdentityTenantsTenantSystemConfiguration>;
export let IdentityTenantsTenantSystemLimitsSchema: z.ZodType<IdentityTenantsTenantSystemLimits>;
export let IdentityTenantsTenantUiSettingsSchema: z.ZodType<IdentityTenantsTenantUiSettings>;
export let IdentityTenantsTenantValidationErrorSchema: z.ZodType<IdentityTenantsTenantValidationError>;
export let IdentityTenantsTenantValidationOutputSchema: z.ZodType<IdentityTenantsTenantValidationOutput>;
export let IdentityTenantsTenantValidationWarningSchema: z.ZodType<IdentityTenantsTenantValidationWarning>;
export let IdentityTenantsUpdateTenantAddressInputSchema: z.ZodType<IdentityTenantsUpdateTenantAddressInput>;
export let IdentityTenantsUpdateTenantBrandingInputSchema: z.ZodType<IdentityTenantsUpdateTenantBrandingInput>;
export let IdentityTenantsUpdateTenantBusinessInfoInputSchema: z.ZodType<IdentityTenantsUpdateTenantBusinessInfoInput>;
export let IdentityTenantsUpdateTenantBusinessRulesInputSchema: z.ZodType<IdentityTenantsUpdateTenantBusinessRulesInput>;
export let IdentityTenantsUpdateTenantContactInfoInputSchema: z.ZodType<IdentityTenantsUpdateTenantContactInfoInput>;
export let IdentityTenantsUpdateTenantCurrencySettingsInputSchema: z.ZodType<IdentityTenantsUpdateTenantCurrencySettingsInput>;
export let IdentityTenantsUpdateTenantIntegrationSettingsInputSchema: z.ZodType<IdentityTenantsUpdateTenantIntegrationSettingsInput>;
export let IdentityTenantsUpdateTenantMemberRoleOutputSchema: z.ZodType<IdentityTenantsUpdateTenantMemberRoleOutput>;
export let IdentityTenantsUpdateTenantMetadataInputSchema: z.ZodType<IdentityTenantsUpdateTenantMetadataInput>;
export let IdentityTenantsUpdateTenantInputSchema: z.ZodType<IdentityTenantsUpdateTenantInput>;
export let IdentityTenantsUpdateTenantSecuritySettingsInputSchema: z.ZodType<IdentityTenantsUpdateTenantSecuritySettingsInput>;
export let IdentityTenantsUpdateTenantSettingsInputSchema: z.ZodType<IdentityTenantsUpdateTenantSettingsInput>;
export let IdentityTenantsUpdateTenantSystemConfigurationInputSchema: z.ZodType<IdentityTenantsUpdateTenantSystemConfigurationInput>;
export let IdentityTenantsUpdateTenantSystemLimitsInputSchema: z.ZodType<IdentityTenantsUpdateTenantSystemLimitsInput>;
export let IdentityTenantsUpdateTenantTagsInputSchema: z.ZodType<IdentityTenantsUpdateTenantTagsInput>;
export let IdentityTenantsUpdateTenantUiSettingsInputSchema: z.ZodType<IdentityTenantsUpdateTenantUiSettingsInput>;
export let IdentityTenantsUpdateUserMembershipRoleInputSchema: z.ZodType<IdentityTenantsUpdateUserMembershipRoleInput>;
export let IdentityTenantsUsageTrackingSchema: z.ZodType<IdentityTenantsUsageTracking>;
export let IdentityTenantsUserMembershipSchema: z.ZodType<IdentityTenantsUserMembership>;
export let IdentityTenantsValidateTenantInputSchema: z.ZodType<IdentityTenantsValidateTenantInput>;
export let IdentityUsersBulkActivateUsersInputSchema: z.ZodType<IdentityUsersBulkActivateUsersInput>;
export let IdentityUsersBulkActivateUsersOutputSchema: z.ZodType<IdentityUsersBulkActivateUsersOutput>;
export let IdentityUsersBulkCreateUsersInputSchema: z.ZodType<IdentityUsersBulkCreateUsersInput>;
export let IdentityUsersBulkCreateUsersOutputSchema: z.ZodType<IdentityUsersBulkCreateUsersOutput>;
export let IdentityUsersBulkDeactivateUsersInputSchema: z.ZodType<IdentityUsersBulkDeactivateUsersInput>;
export let IdentityUsersBulkDeactivateUsersOutputSchema: z.ZodType<IdentityUsersBulkDeactivateUsersOutput>;
export let IdentityUsersBulkDeleteUsersInputSchema: z.ZodType<IdentityUsersBulkDeleteUsersInput>;
export let IdentityUsersBulkNotificationInputSchema: z.ZodType<IdentityUsersBulkNotificationInput>;
export let IdentityUsersBulkPurgeUsersInputSchema: z.ZodType<IdentityUsersBulkPurgeUsersInput>;
export let IdentityUsersBulkRestoreUsersInputSchema: z.ZodType<IdentityUsersBulkRestoreUsersInput>;
export let IdentityUsersBulkRestoreUsersOutputSchema: z.ZodType<IdentityUsersBulkRestoreUsersOutput>;
export let IdentityUsersBulkSuspendUsersInputSchema: z.ZodType<IdentityUsersBulkSuspendUsersInput>;
export let IdentityUsersBulkSuspendUsersOutputSchema: z.ZodType<IdentityUsersBulkSuspendUsersOutput>;
export let IdentityUsersBulkUnsuspendUsersInputSchema: z.ZodType<IdentityUsersBulkUnsuspendUsersInput>;
export let IdentityUsersBulkUnsuspendUsersOutputSchema: z.ZodType<IdentityUsersBulkUnsuspendUsersOutput>;
export let IdentityUsersBulkUpdateUsersInputSchema: z.ZodType<IdentityUsersBulkUpdateUsersInput>;
export let IdentityUsersCreateUserInputSchema: z.ZodType<IdentityUsersCreateUserInput>;
export let IdentityUsersCreateUserRequestItemSchema: z.ZodType<IdentityUsersCreateUserRequestItem>;
export let IdentityUsersNotificationActionSchema: z.ZodType<IdentityUsersNotificationAction>;
export let IdentityUsersNotificationFilterCriteriaSchema: z.ZodType<IdentityUsersNotificationFilterCriteria>;
export let IdentityUsersPurgeStrategySchema: z.ZodType<IdentityUsersPurgeStrategy>;
export let IdentityUsersReplaceUserAccessibilityPreferencesInputSchema: z.ZodType<IdentityUsersReplaceUserAccessibilityPreferencesInput>;
export let IdentityUsersReplaceUserLocalizationPreferencesInputSchema: z.ZodType<IdentityUsersReplaceUserLocalizationPreferencesInput>;
export let IdentityUsersReplaceUserMetadataInputSchema: z.ZodType<IdentityUsersReplaceUserMetadataInput>;
export let IdentityUsersReplaceUserNotificationPreferencesInputSchema: z.ZodType<IdentityUsersReplaceUserNotificationPreferencesInput>;
export let IdentityUsersReplaceUserPreferencesInputSchema: z.ZodType<IdentityUsersReplaceUserPreferencesInput>;
export let IdentityUsersReplaceUserPrivacyPreferencesInputSchema: z.ZodType<IdentityUsersReplaceUserPrivacyPreferencesInput>;
export let IdentityUsersReplaceUserProfileInputSchema: z.ZodType<IdentityUsersReplaceUserProfileInput>;
export let IdentityUsersUpdateUserAccessibilityPreferencesInputSchema: z.ZodType<IdentityUsersUpdateUserAccessibilityPreferencesInput>;
export let IdentityUsersUpdateUserLocalizationPreferencesInputSchema: z.ZodType<IdentityUsersUpdateUserLocalizationPreferencesInput>;
export let IdentityUsersUpdateUserMetadataInputSchema: z.ZodType<IdentityUsersUpdateUserMetadataInput>;
export let IdentityUsersUpdateUserNotificationPreferencesInputSchema: z.ZodType<IdentityUsersUpdateUserNotificationPreferencesInput>;
export let IdentityUsersUpdateUserPreferencesInputSchema: z.ZodType<IdentityUsersUpdateUserPreferencesInput>;
export let IdentityUsersUpdateUserPrivacyPreferencesInputSchema: z.ZodType<IdentityUsersUpdateUserPrivacyPreferencesInput>;
export let IdentityUsersUpdateUserProfileInputSchema: z.ZodType<IdentityUsersUpdateUserProfileInput>;
export let IdentityUsersUpdateUserInputSchema: z.ZodType<IdentityUsersUpdateUserInput>;
export let IdentityUsersUpdateUserRequestItemSchema: z.ZodType<IdentityUsersUpdateUserRequestItem>;
export let IdentityUsersUserAccessibilityPreferencesSchema: z.ZodType<IdentityUsersUserAccessibilityPreferences>;
export let IdentityUsersUserSchema: z.ZodType<IdentityUsersUser>;
export let IdentityUsersUserLocalizationPreferencesSchema: z.ZodType<IdentityUsersUserLocalizationPreferences>;
export let IdentityUsersUserMetadataSchema: z.ZodType<IdentityUsersUserMetadata>;
export let IdentityUsersUserNotificationDetailSchema: z.ZodType<IdentityUsersUserNotificationDetail>;
export let IdentityUsersUserNotificationSchema: z.ZodType<IdentityUsersUserNotification>;
export let IdentityUsersUserNotificationPreferencesSchema: z.ZodType<IdentityUsersUserNotificationPreferences>;
export let IdentityUsersUserPreferencesSchema: z.ZodType<IdentityUsersUserPreferences>;
export let IdentityUsersUserPrivacyPreferencesSchema: z.ZodType<IdentityUsersUserPrivacyPreferences>;
export let IdentityUsersUserProfileSchema: z.ZodType<IdentityUsersUserProfile>;
export let LearningAssessmentsAssessmentSchema: z.ZodType<LearningAssessmentsAssessment>;
export let LearningAssessmentsAssessmentSubmissionSchema: z.ZodType<LearningAssessmentsAssessmentSubmission>;
export let LearningAssessmentsAssessmentTypeSchema: z.ZodType<LearningAssessmentsAssessmentType>;
export let LearningAssessmentsCanAttemptOutputSchema: z.ZodType<LearningAssessmentsCanAttemptOutput>;
export let LearningAssessmentsCreateAssessmentInputSchema: z.ZodType<LearningAssessmentsCreateAssessmentInput>;
export let LearningAssessmentsGradeSubmissionInputSchema: z.ZodType<LearningAssessmentsGradeSubmissionInput>;
export let LearningAssessmentsStartSubmissionInputSchema: z.ZodType<LearningAssessmentsStartSubmissionInput>;
export let LearningAssessmentsSubmissionStatusSchema: z.ZodType<LearningAssessmentsSubmissionStatus>;
export let LearningAssessmentsUpdateAssessmentInputSchema: z.ZodType<LearningAssessmentsUpdateAssessmentInput>;
export let LearningCoursesActivityGradeSchema: z.ZodType<LearningCoursesActivityGrade>;
export let LearningCoursesCircularDependencyCheckResultSchema: z.ZodType<LearningCoursesCircularDependencyCheckResult>;
export let LearningCoursesCloneProgramSchema: z.ZodType<LearningCoursesCloneProgram>;
export let LearningCoursesCompleteContentInputSchema: z.ZodType<LearningCoursesCompleteContentInput>;
export let LearningCoursesCompletionRatesSchema: z.ZodType<LearningCoursesCompletionRates>;
export let LearningCoursesCompletionTrendSchema: z.ZodType<LearningCoursesCompletionTrend>;
export let LearningCoursesContentInteractionSchema: z.ZodType<LearningCoursesContentInteraction>;
export let LearningCoursesContentInteractionSummarySchema: z.ZodType<LearningCoursesContentInteractionSummary>;
export let LearningCoursesContentProgressSchema: z.ZodType<LearningCoursesContentProgress>;
export let LearningCoursesContentStatsSchema: z.ZodType<LearningCoursesContentStats>;
export let LearningCoursesContentSummarySchema: z.ZodType<LearningCoursesContentSummary>;
export let LearningCoursesCreateActivityGradeSchema: z.ZodType<LearningCoursesCreateActivityGrade>;
export let LearningCoursesCreatePrerequisiteApiInputSchema: z.ZodType<LearningCoursesCreatePrerequisiteApiInput>;
export let LearningCoursesCreateProductFromProgramSchema: z.ZodType<LearningCoursesCreateProductFromProgram>;
export let LearningCoursesCreateProgramContentSchema: z.ZodType<LearningCoursesCreateProgramContent>;
export let LearningCoursesCreateProgramSchema: z.ZodType<LearningCoursesCreateProgram>;
export let LearningCoursesEngagementMetricsSchema: z.ZodType<LearningCoursesEngagementMetrics>;
export let LearningCoursesEnrollmentStatusSchema: z.ZodType<LearningCoursesEnrollmentStatus>;
export let LearningCoursesGradeStatisticsSchema: z.ZodType<LearningCoursesGradeStatistics>;
export let LearningCoursesGraderSummarySchema: z.ZodType<LearningCoursesGraderSummary>;
export let LearningCoursesGradingMethodSchema: z.ZodType<LearningCoursesGradingMethod>;
export let LearningCoursesMonetizationSchema: z.ZodType<LearningCoursesMonetization>;
export let LearningCoursesMoveContentSchema: z.ZodType<LearningCoursesMoveContent>;
export let LearningCoursesPrerequisiteCheckResultSchema: z.ZodType<LearningCoursesPrerequisiteCheckResult>;
export let LearningCoursesPrerequisiteSchema: z.ZodType<LearningCoursesPrerequisite>;
export let LearningCoursesPrerequisiteStatusSchema: z.ZodType<LearningCoursesPrerequisiteStatus>;
export let LearningCoursesPrerequisiteTypeSchema: z.ZodType<LearningCoursesPrerequisiteType>;
export let LearningCoursesPricingSchema: z.ZodType<LearningCoursesPricing>;
export let LearningCoursesProgramAnalyticsSchema: z.ZodType<LearningCoursesProgramAnalytics>;
export let LearningCoursesProgramContentSchema: z.ZodType<LearningCoursesProgramContent>;
export let LearningCoursesProgramContentTypeSchema: z.ZodType<LearningCoursesProgramContentType>;
export let LearningCoursesProgramDifficultySchema: z.ZodType<LearningCoursesProgramDifficulty>;
export let LearningCoursesProgramSchema: z.ZodType<LearningCoursesProgram>;
export let LearningCoursesProgramUserSummarySchema: z.ZodType<LearningCoursesProgramUserSummary>;
export let LearningCoursesProgressStatusSchema: z.ZodType<LearningCoursesProgressStatus>;
export let LearningCoursesRejectProgramSchema: z.ZodType<LearningCoursesRejectProgram>;
export let LearningCoursesReorderContentSchema: z.ZodType<LearningCoursesReorderContent>;
export let LearningCoursesReorderPrerequisitesInputSchema: z.ZodType<LearningCoursesReorderPrerequisitesInput>;
export let LearningCoursesRevenueAnalyticsSchema: z.ZodType<LearningCoursesRevenueAnalytics>;
export let LearningCoursesRevenueChartSchema: z.ZodType<LearningCoursesRevenueChart>;
export let LearningCoursesScheduleProgramSchema: z.ZodType<LearningCoursesScheduleProgram>;
export let LearningCoursesSearchContentSchema: z.ZodType<LearningCoursesSearchContent>;
export let LearningCoursesStartContentInputSchema: z.ZodType<LearningCoursesStartContentInput>;
export let LearningCoursesStudentSummarySchema: z.ZodType<LearningCoursesStudentSummary>;
export let LearningCoursesSubmitContentInputSchema: z.ZodType<LearningCoursesSubmitContentInput>;
export let LearningCoursesSubmitUserContentSchema: z.ZodType<LearningCoursesSubmitUserContent>;
export let LearningCoursesUpdateActivityGradeSchema: z.ZodType<LearningCoursesUpdateActivityGrade>;
export let LearningCoursesUpdatePrerequisiteApiInputSchema: z.ZodType<LearningCoursesUpdatePrerequisiteApiInput>;
export let LearningCoursesUpdatePricingSchema: z.ZodType<LearningCoursesUpdatePricing>;
export let LearningCoursesUpdateProgramContentSchema: z.ZodType<LearningCoursesUpdateProgramContent>;
export let LearningCoursesUpdateProgramSchema: z.ZodType<LearningCoursesUpdateProgram>;
export let LearningCoursesUpdateProgressSchema: z.ZodType<LearningCoursesUpdateProgress>;
export let LearningCoursesUpdateProgressInputSchema: z.ZodType<LearningCoursesUpdateProgressInput>;
export let LearningCoursesUpdateTimeSpentInputSchema: z.ZodType<LearningCoursesUpdateTimeSpentInput>;
export let LearningCoursesUserProgressSchema: z.ZodType<LearningCoursesUserProgress>;
export let LearningCoursesVisibilitySchema: z.ZodType<LearningCoursesVisibility>;
export let LearningEnrollmentsEnrollUserInputSchema: z.ZodType<LearningEnrollmentsEnrollUserInput>;
export let LearningEnrollmentsEnrollmentSchema: z.ZodType<LearningEnrollmentsEnrollment>;
export let LearningEnrollmentsEnrollmentStatusSchema: z.ZodType<LearningEnrollmentsEnrollmentStatus>;
export let LearningEnrollmentsUpdateEnrollmentProgressInputSchema: z.ZodType<LearningEnrollmentsUpdateEnrollmentProgressInput>;
export let MvcProblemDetailsSchema: z.ZodType<MvcProblemDetails>;
export let ObjectsAttestationConveyancePreferenceSchema: z.ZodType<ObjectsAttestationConveyancePreference>;
export let ObjectsAttestationStatementFormatIdentifierSchema: z.ZodType<ObjectsAttestationStatementFormatIdentifier>;
export let ObjectsAuthenticationExtensionsClientInputsSchema: z.ZodType<ObjectsAuthenticationExtensionsClientInputs>;
export let ObjectsAuthenticationExtensionsLargeBlobInputsSchema: z.ZodType<ObjectsAuthenticationExtensionsLargeBlobInputs>;
export let ObjectsAuthenticationExtensionsPRFInputsSchema: z.ZodType<ObjectsAuthenticationExtensionsPRFInputs>;
export let ObjectsAuthenticationExtensionsPRFValuesSchema: z.ZodType<ObjectsAuthenticationExtensionsPRFValues>;
export let ObjectsAuthenticatorAttachmentSchema: z.ZodType<ObjectsAuthenticatorAttachment>;
export let ObjectsAuthenticatorTransportSchema: z.ZodType<ObjectsAuthenticatorTransport>;
export let ObjectsCOSEAlgorithmSchema: z.ZodType<ObjectsCOSEAlgorithm>;
export let ObjectsCredentialProtectionPolicySchema: z.ZodType<ObjectsCredentialProtectionPolicy>;
export let ObjectsLargeBlobSupportSchema: z.ZodType<ObjectsLargeBlobSupport>;
export let ObjectsPublicKeyCredentialDescriptorSchema: z.ZodType<ObjectsPublicKeyCredentialDescriptor>;
export let ObjectsPublicKeyCredentialHintSchema: z.ZodType<ObjectsPublicKeyCredentialHint>;
export let ObjectsPublicKeyCredentialTypeSchema: z.ZodType<ObjectsPublicKeyCredentialType>;
export let ObjectsResidentKeyRequirementSchema: z.ZodType<ObjectsResidentKeyRequirement>;
export let ObjectsUserVerificationRequirementSchema: z.ZodType<ObjectsUserVerificationRequirement>;
export let PagedResultSchema: z.ZodType<PagedResult>;
export let ResourcesArchiveResourceUsageRecordsInputSchema: z.ZodType<ResourcesArchiveResourceUsageRecordsInput>;
export let ResourcesCheckResourceQuotaInputSchema: z.ZodType<ResourcesCheckResourceQuotaInput>;
export let ResourcesCleanupOrphanedResourcesInputSchema: z.ZodType<ResourcesCleanupOrphanedResourcesInput>;
export let ResourcesEffectiveSettingOutputSchema: z.ZodType<ResourcesEffectiveSettingOutput>;
export let ResourcesRecordTenantResourceUsageInputSchema: z.ZodType<ResourcesRecordTenantResourceUsageInput>;
export let ResourcesRecordUserResourceUsageInputSchema: z.ZodType<ResourcesRecordUserResourceUsageInput>;
export let ResourcesResourceMetadataSchema: z.ZodType<ResourcesResourceMetadata>;
export let ResourcesResourceQuotaEnforcementResultSchema: z.ZodType<ResourcesResourceQuotaEnforcementResult>;
export let ResourcesResourceQuotaPeriodSchema: z.ZodType<ResourcesResourceQuotaPeriod>;
export let ResourcesResourceQuotaOutputSchema: z.ZodType<ResourcesResourceQuotaOutput>;
export let ResourcesResourceSettingsSchema: z.ZodType<ResourcesResourceSettings>;
export let ResourcesResourceUsageTypeSchema: z.ZodType<ResourcesResourceUsageType>;
export let ResourcesSetQuotaInputSchema: z.ZodType<ResourcesSetQuotaInput>;
export let ResourcesSetResourceMetadataInputSchema: z.ZodType<ResourcesSetResourceMetadataInput>;
export let ResourcesSetResourceSettingsInputSchema: z.ZodType<ResourcesSetResourceSettingsInput>;
export let ResourcesSetUserResourceSettingsInputSchema: z.ZodType<ResourcesSetUserResourceSettingsInput>;
export let ResourcesToggleResourceQuotaInputSchema: z.ZodType<ResourcesToggleResourceQuotaInput>;
export let ResourcesTrendGranularitySchema: z.ZodType<ResourcesTrendGranularity>;
export let ResourcesUsageRecordSchema: z.ZodType<ResourcesUsageRecord>;
export let ResourcesUsageTrendDataPointSchema: z.ZodType<ResourcesUsageTrendDataPoint>;
export let ResourcesUsageTrendsResultSchema: z.ZodType<ResourcesUsageTrendsResult>;
export let SocialBlogBlogPostSchema: z.ZodType<SocialBlogBlogPost>;
export let SocialBlogBlogPostStatusSchema: z.ZodType<SocialBlogBlogPostStatus>;
export let SocialBlogCreateBlogPostInputSchema: z.ZodType<SocialBlogCreateBlogPostInput>;
export let SocialFeedAddFeedItemInputSchema: z.ZodType<SocialFeedAddFeedItemInput>;
export let SocialFeedFeedContentTypeSchema: z.ZodType<SocialFeedFeedContentType>;
export let SocialFeedFeedItemSchema: z.ZodType<SocialFeedFeedItem>;
export let SocialFeedFeedItemReasonSchema: z.ZodType<SocialFeedFeedItemReason>;
export let SocialGroupsApproveSocialGroupMemberInputSchema: z.ZodType<SocialGroupsApproveSocialGroupMemberInput>;
export let SocialGroupsChangeSocialGroupMemberRoleInputSchema: z.ZodType<SocialGroupsChangeSocialGroupMemberRoleInput>;
export let SocialGroupsCreateSocialGroupInputSchema: z.ZodType<SocialGroupsCreateSocialGroupInput>;
export let SocialGroupsJoinSocialGroupInputSchema: z.ZodType<SocialGroupsJoinSocialGroupInput>;
export let SocialGroupsSocialGroupSchema: z.ZodType<SocialGroupsSocialGroup>;
export let SocialGroupsSocialGroupMemberSchema: z.ZodType<SocialGroupsSocialGroupMember>;
export let SocialGroupsSocialGroupMemberRoleSchema: z.ZodType<SocialGroupsSocialGroupMemberRole>;
export let SocialGroupsSocialGroupMembershipStatusSchema: z.ZodType<SocialGroupsSocialGroupMembershipStatus>;
export let SocialGroupsSocialGroupStatusSchema: z.ZodType<SocialGroupsSocialGroupStatus>;
export let SocialGroupsSocialGroupTypeSchema: z.ZodType<SocialGroupsSocialGroupType>;
export let SocialGroupsSocialGroupVisibilitySchema: z.ZodType<SocialGroupsSocialGroupVisibility>;
export let SocialGroupsUpdateSocialGroupInputSchema: z.ZodType<SocialGroupsUpdateSocialGroupInput>;
export let SocialProfilesAddProfilePortfolioItemBodySchema: z.ZodType<SocialProfilesAddProfilePortfolioItemBody>;
export let SocialProfilesAddProfileSkillBodySchema: z.ZodType<SocialProfilesAddProfileSkillBody>;
export let SocialProfilesProfileAvailabilityStatusSchema: z.ZodType<SocialProfilesProfileAvailabilityStatus>;
export let SocialProfilesProfilePortfolioItemSchema: z.ZodType<SocialProfilesProfilePortfolioItem>;
export let SocialProfilesProfileSkillSchema: z.ZodType<SocialProfilesProfileSkill>;
export let SocialProfilesProfileSkillProficiencySchema: z.ZodType<SocialProfilesProfileSkillProficiency>;
export let SocialProfilesProfileVisibilitySchema: z.ZodType<SocialProfilesProfileVisibility>;
export let SocialProfilesSocialProfileSchema: z.ZodType<SocialProfilesSocialProfile>;
export let SocialProfilesUpdateProfilePortfolioItemBodySchema: z.ZodType<SocialProfilesUpdateProfilePortfolioItemBody>;
export let SocialProfilesUpdateProfilePrivacyBodySchema: z.ZodType<SocialProfilesUpdateProfilePrivacyBody>;
export let SocialProfilesUpdateProfileStatsBodySchema: z.ZodType<SocialProfilesUpdateProfileStatsBody>;
export let SocialProfilesUpdateSocialProfileBodySchema: z.ZodType<SocialProfilesUpdateSocialProfileBody>;
export let SocialReactionsReactionSchema: z.ZodType<SocialReactionsReaction>;
export let SocialReactionsReactionTargetTypeSchema: z.ZodType<SocialReactionsReactionTargetType>;
export let SocialReactionsReactionTypeSchema: z.ZodType<SocialReactionsReactionType>;
export let SocialReactionsRemoveReactionInputSchema: z.ZodType<SocialReactionsRemoveReactionInput>;
export let SocialReactionsSetReactionInputSchema: z.ZodType<SocialReactionsSetReactionInput>;
export let SocialReactionsTargetReactionSummarySchema: z.ZodType<SocialReactionsTargetReactionSummary>;
export let TagsCreateTagProficiencyInputSchema: z.ZodType<TagsCreateTagProficiencyInput>;
export let TagsCreateTagRelationshipInputSchema: z.ZodType<TagsCreateTagRelationshipInput>;
export let TagsCreateTagInputSchema: z.ZodType<TagsCreateTagInput>;
export let TagsSkillProficiencyLevelSchema: z.ZodType<TagsSkillProficiencyLevel>;
export let TagsTagSchema: z.ZodType<TagsTag>;
export let TagsTagProficiencySchema: z.ZodType<TagsTagProficiency>;
export let TagsTagRelationshipSchema: z.ZodType<TagsTagRelationship>;
export let TagsTagRelationshipTypeSchema: z.ZodType<TagsTagRelationshipType>;
export let TagsTagTypeSchema: z.ZodType<TagsTagType>;
export let TagsUpdateTagInputSchema: z.ZodType<TagsUpdateTagInput>;

// Zod Schema Definitions
/** Zod schema for BillingCycle */
BillingCycleSchema = z.enum(['Weekly', 'Monthly', 'Quarterly', 'SemiAnnually', 'Annually', 'Biannually']);

/** Zod schema for BulkOperationError */
BulkOperationErrorSchema = z.object({
  tenantId: z.string().uuid().optional(),
  tenantName: z.string().nullable().optional(),
  errorMessage: z.string().nullable().optional(),
  errorCode: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsTaxJurisdictionDto */
CommercePaymentsTaxJurisdictionDtoSchema = z.object({
  id: z.string().uuid().optional(),
  code: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  state: z.string().nullable().optional(),
  taxType: z.string().nullable().optional(),
  defaultRate: z.number().optional(),
  isActive: z.boolean().optional(),
});

/** Zod schema for CommercePaymentsTaxRuleDto */
CommercePaymentsTaxRuleDtoSchema = z.object({
  id: z.string().uuid().optional(),
  jurisdictionCode: z.string().nullable().optional(),
  productCategory: z.string().nullable().optional(),
  customerType: z.string().nullable().optional(),
  rate: z.number().optional(),
  effectiveFrom: z.string().datetime().optional(),
  effectiveTo: z.string().datetime().nullable().optional(),
  description: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
});

/** Zod schema for ContentStatus */
ContentStatusSchema = z.enum(['Draft', 'Review', 'Published', 'Archived', 'Deleted']);

/** Zod schema for ContentVisibility */
ContentVisibilitySchema = z.enum(['Private', 'Internal', 'Friends', 'Protected', 'Public']);

/** Zod schema for IdentityTenantsTenantSettingsDto */
IdentityTenantsTenantSettingsDtoSchema = z.object({
  id: z.string().uuid().optional(),
  systemConfiguration: z.lazy(() => IdentityTenantsTenantSystemConfigurationSchema).optional(),
  featureFlags: z.record(z.string(), z.boolean()).nullable().optional(),
  businessRules: z.lazy(() => IdentityTenantsTenantBusinessRulesSchema).optional(),
  userInterfaceSettings: z.lazy(() => IdentityTenantsTenantUiSettingsSchema).optional(),
  securitySettings: z.lazy(() => IdentityTenantsTenantSecuritySettingsSchema).optional(),
  integrationSettings: z.lazy(() => IdentityTenantsTenantIntegrationSettingsSchema).optional(),
  systemLimits: z.lazy(() => IdentityTenantsTenantSystemLimitsSchema).optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for KeyValuePairStringAuthenticationExtensionsPRFValues */
KeyValuePairStringAuthenticationExtensionsPRFValuesSchema = z.object({
  key: z.string().nullable().optional(),
  value: z.lazy(() => ObjectsAuthenticationExtensionsPRFValuesSchema).optional(),
});

/** Zod schema for Money */
MoneySchema = z.object({
  amount: z.number().optional(),
  currency: z.string().nullable().optional(),
});

/** Zod schema for PagedResult1GameGuildCommerceProductsPromoCodeDtoGameGuildCommerceProductsVersion100PagedResultPromoCodeDto */
PagedResult1GameGuildCommerceProductsPromoCodeDtoGameGuildCommerceProductsVersion100PagedResultPromoCodeDtoSchema = z.object({
  items: z
    .array(z.lazy(() => CommerceProductsPromoCodeSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for PagedResult1GameGuildIdentityTenantsTenantGameGuildIdentityTenantsVersion100PagedResultTenant */
PagedResult1GameGuildIdentityTenantsTenantGameGuildIdentityTenantsVersion100PagedResultTenantSchema = z.object({
  items: z
    .array(z.lazy(() => IdentityTenantsTenantSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for PagedResult1GameGuildIdentityTenantsTenantAuditLogEntryGameGuildIdentityTenantsVersion100PagedResultTenantAuditLogEntry */
PagedResult1GameGuildIdentityTenantsTenantAuditLogEntryGameGuildIdentityTenantsVersion100PagedResultTenantAuditLogEntrySchema = z.object({
  items: z
    .array(z.lazy(() => IdentityTenantsTenantAuditLogEntrySchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for PagedResult1GameGuildIdentityUsersUserDtoGameGuildIdentityUsersVersion100PagedResultUserDto */
PagedResult1GameGuildIdentityUsersUserDtoGameGuildIdentityUsersVersion100PagedResultUserDtoSchema = z.object({
  items: z
    .array(z.lazy(() => IdentityUsersUserSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for PagedResult1GameGuildIdentityUsersUserNotificationDtoGameGuildIdentityUsersVersion100PagedResultUserNotificationDto */
PagedResult1GameGuildIdentityUsersUserNotificationDtoGameGuildIdentityUsersVersion100PagedResultUserNotificationDtoSchema = z.object({
  items: z
    .array(z.lazy(() => IdentityUsersUserNotificationSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for PagedResult1GameGuildIdentityUsersUserProfileDtoGameGuildIdentityUsersVersion100PagedResultUserProfileDto */
PagedResult1GameGuildIdentityUsersUserProfileDtoGameGuildIdentityUsersVersion100PagedResultUserProfileDtoSchema = z.object({
  items: z
    .array(z.lazy(() => IdentityUsersUserProfileSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for ProgramCategory */
ProgramCategorySchema = z.enum([
  'General',
  'Programming',
  'DataScience',
  'WebDevelopment',
  'MobileDevelopment',
  'GameDevelopment',
  'AI',
  'Cybersecurity',
  'DevOps',
  'Database',
  'Business',
  'Design',
  'Marketing',
  'ProjectManagement',
  'PersonalDevelopment',
  'CreativeArts',
  'Science',
  'Language',
  'Other',
]);

/** Zod schema for TenantInfo */
TenantInfoSchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
});

/** Zod schema for AIAiChatMessage */
AIAiChatMessageSchema = z.object({
  role: z.string().nullable().optional(),
  content: z.string().nullable().optional(),
});

/** Zod schema for AIAiChatInput */
AIAiChatInputSchema = z.object({
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  messages: z
    .array(z.lazy(() => AIAiChatMessageSchema))
    .nullable()
    .optional(),
  temperature: z.number().nullable().optional(),
  maxTokens: z.number().int().nullable().optional(),
});

/** Zod schema for AIAiCompletionOutput */
AIAiCompletionOutputSchema = z.object({
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  text: z.string().nullable().optional(),
  finishReason: z.string().nullable().optional(),
  usage: z.lazy(() => AIAiUsageSchema).optional(),
});

/** Zod schema for AIAiConversationHistoryEntry */
AIAiConversationHistoryEntrySchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().nullable().optional(),
  requestKind: z.string().nullable().optional(),
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  requestText: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  responseText: z.string().nullable().optional(),
  outcome: z.string().nullable().optional(),
  outcomeCode: z.string().nullable().optional(),
  outcomeReason: z.string().nullable().optional(),
  finishReason: z.string().nullable().optional(),
  usage: z.lazy(() => AIAiUsageSchema).optional(),
  occurredAt: z.string().datetime().optional(),
});

/** Zod schema for AIAiGenerateInput */
AIAiGenerateInputSchema = z.object({
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  prompt: z.string().nullable().optional(),
  temperature: z.number().nullable().optional(),
  maxTokens: z.number().int().nullable().optional(),
});

/** Zod schema for AIAiGeneratedContentDraftInput */
AIAiGeneratedContentDraftInputSchema = z.object({
  subject: z.string().nullable().optional(),
  context: z.string().nullable().optional(),
  audience: z.string().nullable().optional(),
  tone: z.string().nullable().optional(),
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  maxTokens: z.number().int().nullable().optional(),
});

/** Zod schema for AIAiGeneratedContentKind */
AIAiGeneratedContentKindSchema = z.enum(['Email', 'Report', 'ListingDescription']);

/** Zod schema for AIAiGeneratedContentInput */
AIAiGeneratedContentInputSchema = z.object({
  kind: z.lazy(() => AIAiGeneratedContentKindSchema).optional(),
  subject: z.string().nullable().optional(),
  context: z.string().nullable().optional(),
  audience: z.string().nullable().optional(),
  tone: z.string().nullable().optional(),
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  maxTokens: z.number().int().nullable().optional(),
});

/** Zod schema for AIAiPromptTemplate */
AIAiPromptTemplateSchema = z.object({
  id: z.string().uuid().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  key: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  category: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  prompt: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
  isSystemTemplate: z.boolean().optional(),
  createdByUserId: z.string().uuid().nullable().optional(),
  updatedByUserId: z.string().uuid().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for AIAiPromptTemplateGenerateInput */
AIAiPromptTemplateGenerateInputSchema = z.object({
  variables: z.record(z.string(), z.string().nullable()).nullable().optional(),
  provider: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  temperature: z.number().nullable().optional(),
  maxTokens: z.number().int().nullable().optional(),
});

/** Zod schema for AIAiPromptTemplateRenderInput */
AIAiPromptTemplateRenderInputSchema = z.object({
  variables: z.record(z.string(), z.string().nullable()).nullable().optional(),
});

/** Zod schema for AIAiPromptTemplateRenderOutput */
AIAiPromptTemplateRenderOutputSchema = z.object({
  templateId: z.string().uuid().optional(),
  key: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  prompt: z.string().nullable().optional(),
  variables: z.record(z.string(), z.string().nullable()).nullable().optional(),
});

/** Zod schema for AIAiProviderStatus */
AIAiProviderStatusSchema = z.object({
  provider: z.string().nullable().optional(),
  configured: z.boolean().optional(),
  defaultModel: z.string().nullable().optional(),
  baseUrl: z.string().nullable().optional(),
  credentialsConfigured: z.boolean().optional(),
});

/** Zod schema for AIAiQuotaStatus */
AIAiQuotaStatusSchema = z.object({
  resourceType: z.string().nullable().optional(),
  currentUsage: z.number().int().optional(),
  softLimit: z.number().int().nullable().optional(),
  hardLimit: z.number().int().nullable().optional(),
  remaining: z.number().int().optional(),
  usagePercent: z.number().optional(),
  period: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
  lastReset: z.string().datetime().nullable().optional(),
  nextReset: z.string().datetime().nullable().optional(),
});

/** Zod schema for AIAiQuotaStatusOutput */
AIAiQuotaStatusOutputSchema = z.object({
  tenantId: z.string().uuid().optional(),
  quotas: z
    .array(z.lazy(() => AIAiQuotaStatusSchema))
    .nullable()
    .optional(),
  generatedAtUtc: z.string().datetime().optional(),
});

/** Zod schema for AIAiStatusOutput */
AIAiStatusOutputSchema = z.object({
  enabled: z.boolean().optional(),
  defaultProvider: z.string().nullable().optional(),
  allowTenantOverrides: z.boolean().optional(),
  providers: z
    .array(z.lazy(() => AIAiProviderStatusSchema))
    .nullable()
    .optional(),
});

/** Zod schema for AIAiUsage */
AIAiUsageSchema = z.object({
  inputTokens: z.number().int().nullable().optional(),
  outputTokens: z.number().int().nullable().optional(),
  totalTokens: z.number().int().nullable().optional(),
});

/** Zod schema for AICreateAiPromptTemplateInput */
AICreateAiPromptTemplateInputSchema = z.object({
  key: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  prompt: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  category: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
});

/** Zod schema for AIUpdateAiPromptTemplateInput */
AIUpdateAiPromptTemplateInputSchema = z.object({
  name: z.string().nullable().optional(),
  prompt: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  category: z.string().nullable().optional(),
  systemPrompt: z.string().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
});

/** Zod schema for APIControllersApplicationDetails */
APIControllersApplicationDetailsSchema = z.object({
  name: z.string().nullable().optional(),
  version: z.string().nullable().optional(),
  informationalVersion: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for APIControllersApplicationInfoOutput */
APIControllersApplicationInfoOutputSchema = z.object({
  application: z.lazy(() => APIControllersApplicationDetailsSchema).optional(),
  build: z.lazy(() => APIControllersBuildDetailsSchema).optional(),
  runtime: z.lazy(() => APIControllersRuntimeDetailsSchema).optional(),
  process: z.lazy(() => APIControllersProcessDetailsSchema).optional(),
  timestamp: z.string().datetime().optional(),
});

/** Zod schema for APIControllersBuildDetails */
APIControllersBuildDetailsSchema = z.object({
  timestamp: z.string().datetime().nullable().optional(),
  configuration: z.string().nullable().optional(),
  framework: z.string().nullable().optional(),
});

/** Zod schema for APIControllersDependencyHealthItem */
APIControllersDependencyHealthItemSchema = z.object({
  name: z.string().nullable().optional(),
  status: z.string().nullable().optional(),
  duration: z.string().optional(),
  description: z.string().nullable().optional(),
  isHealthy: z.boolean().optional(),
  tags: z.array(z.string()).nullable().optional(),
  data: z.record(z.string(), z.string()).nullable().optional(),
  exception: z.string().nullable().optional(),
});

/** Zod schema for APIControllersDependencyHealthOutput */
APIControllersDependencyHealthOutputSchema = z.object({
  status: z.string().nullable().optional(),
  totalDuration: z.string().optional(),
  timestamp: z.string().datetime().optional(),
  healthyCount: z.number().int().optional(),
  unhealthyCount: z.number().int().optional(),
  dependencies: z
    .array(z.lazy(() => APIControllersDependencyHealthItemSchema))
    .nullable()
    .optional(),
  error: z.string().nullable().optional(),
});

/** Zod schema for APIControllersHealthinessOutput */
APIControllersHealthinessOutputSchema = z.object({
  status: z.string().nullable().optional(),
  duration: z.string().optional(),
  timestamp: z.string().datetime().optional(),
  checks: z
    .record(
      z.string(),
      z.lazy(() => APIControllersHealthinessResponseItemSchema),
    )
    .nullable()
    .optional(),
  error: z.string().nullable().optional(),
});

/** Zod schema for APIControllersHealthinessResponseItem */
APIControllersHealthinessResponseItemSchema = z.object({
  status: z.string().nullable().optional(),
  duration: z.string().optional(),
  description: z.string().nullable().optional(),
  data: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for APIControllersLivenessOutput */
APIControllersLivenessOutputSchema = z.object({
  status: z.string().nullable().optional(),
  alive: z.boolean().optional(),
  timestamp: z.string().datetime().optional(),
  uptime: z.string().optional(),
  version: z.string().nullable().optional(),
});

/** Zod schema for APIControllersProcessDetails */
APIControllersProcessDetailsSchema = z.object({
  startTime: z.string().datetime().optional(),
  uptime: z.string().optional(),
});

/** Zod schema for APIControllersReadinessOutput */
APIControllersReadinessOutputSchema = z.object({
  status: z.string().nullable().optional(),
  ready: z.boolean().optional(),
  timestamp: z.string().datetime().optional(),
  services: z.record(z.string(), z.boolean()).nullable().optional(),
  error: z.string().nullable().optional(),
});

/** Zod schema for APIControllersRuntimeDetails */
APIControllersRuntimeDetailsSchema = z.object({
  dotNetVersion: z.string().nullable().optional(),
  osDescription: z.string().nullable().optional(),
  osArchitecture: z.string().nullable().optional(),
  processArchitecture: z.string().nullable().optional(),
});

/** Zod schema for BulkOperationOutput */
BulkOperationOutputSchema = z.object({
  totalRequested: z.number().int().optional(),
  successfulOperations: z.number().int().optional(),
  failedOperations: z.number().int().optional(),
  errors: z
    .array(z.lazy(() => BulkOperationErrorSchema))
    .nullable()
    .optional(),
  isComplete: z.boolean().optional(),
  successRate: z.number().optional(),
});

/** Zod schema for CQRSIDomainEvent */
CQRSIDomainEventSchema = z.object({
  eventId: z.string().uuid().optional(),
  occurredAt: z.string().datetime().optional(),
  version: z.number().int().optional(),
});

/** Zod schema for CommerceBillingInvoicePaymentRetryResult */
CommerceBillingInvoicePaymentRetryResultSchema = z.object({
  invoiceId: z.string().uuid().optional(),
  invoiceNumber: z.string().nullable().optional(),
  invoiceStatus: z.lazy(() => CommerceBillingInvoiceStatusSchema).optional(),
  accepted: z.boolean().optional(),
  code: z.string().nullable().optional(),
  message: z.string().nullable().optional(),
  retryScheduledAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommerceBillingInvoiceStatus */
CommerceBillingInvoiceStatusSchema = z.enum(['Draft', 'Open', 'Paid', 'Void', 'PastDue', 'Uncollectible']);

/** Zod schema for CommercePaymentsAddFundsInput */
CommercePaymentsAddFundsInputSchema = z.object({
  userId: z.string().uuid(),
  amount: z.number(),
  description: z.string().nullable(),
  referenceId: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsCalculateTaxInput */
CommercePaymentsCalculateTaxInputSchema = z.object({
  jurisdictionCode: z.string().nullable(),
  amount: z.number(),
  currency: z.string().nullable(),
  customerType: z.string().nullable(),
  productCategory: z.string().nullable().optional(),
  customerVatNumber: z.string().nullable().optional(),
  isTaxInclusive: z.boolean().optional(),
  transactionDate: z.string().datetime().nullable().optional(),
  applicableExemptions: z.array(z.string()).nullable().optional(),
});

/** Zod schema for CommercePaymentsCreateTaxJurisdictionInput */
CommercePaymentsCreateTaxJurisdictionInputSchema = z.object({
  code: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  state: z.string().nullable().optional(),
  taxType: z.string().nullable().optional(),
  defaultRate: z.number().optional(),
});

/** Zod schema for CommercePaymentsCreateTaxRuleInput */
CommercePaymentsCreateTaxRuleInputSchema = z.object({
  jurisdictionCode: z.string().nullable().optional(),
  productCategory: z.string().nullable().optional(),
  customerType: z.string().nullable().optional(),
  rate: z.number().optional(),
  effectiveFrom: z.string().datetime().optional(),
  effectiveTo: z.string().datetime().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsCreateWalletInput */
CommercePaymentsCreateWalletInputSchema = z.object({
  userId: z.string().uuid(),
  currency: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsCustomerType */
CommercePaymentsCustomerTypeSchema = z.enum(['B2C', 'B2B']);

/** Zod schema for CommercePaymentsDeductFundsInput */
CommercePaymentsDeductFundsInputSchema = z.object({
  userId: z.string().uuid(),
  amount: z.number(),
  description: z.string().nullable(),
  referenceId: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsLockWalletInput */
CommercePaymentsLockWalletInputSchema = z.object({
  reason: z.string().nullable(),
});

/** Zod schema for CommercePaymentsModelsFreezeWalletInput */
CommercePaymentsModelsFreezeWalletInputSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsModelsPatchWalletInput */
CommercePaymentsModelsPatchWalletInputSchema = z.object({
  currency: z.string().nullable().optional(),
  dailyLimit: z.number().nullable().optional(),
  monthlyLimit: z.number().nullable().optional(),
});

/** Zod schema for CommercePaymentsPatchTaxJurisdictionInput */
CommercePaymentsPatchTaxJurisdictionInputSchema = z.object({
  name: z.string().nullable().optional(),
  taxType: z.string().nullable().optional(),
  defaultRate: z.number().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
});

/** Zod schema for CommercePaymentsPatchTaxRuleInput */
CommercePaymentsPatchTaxRuleInputSchema = z.object({
  rate: z.number().nullable().optional(),
  effectiveFrom: z.string().datetime().nullable().optional(),
  effectiveTo: z.string().datetime().nullable().optional(),
  description: z.string().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentCancellationResult */
CommercePaymentsPaymentCancellationResultSchema = z.object({
  paymentId: z.string().uuid(),
  cancellationReason: z.string().nullable(),
  canceledAt: z.string().datetime(),
  canceledBy: z.string().uuid().nullable().optional(),
  success: z.boolean(),
  errorMessage: z.string().nullable().optional(),
  refundProcessed: z.boolean().optional(),
  refundAmount: z.number().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentResult */
CommercePaymentsPaymentResultSchema = z.object({
  success: z.boolean().optional(),
  transactionId: z.string().nullable().optional(),
  paymentId: z.string().nullable().optional(),
  amount: z.lazy(() => MoneySchema).optional(),
  processedAt: z.string().datetime().nullable().optional(),
  failureReason: z.string().nullable().optional(),
  paymentMethodId: z.string().nullable().optional(),
  status: z.lazy(() => CommercePaymentsPaymentStatusSchema).optional(),
  invoiceId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentRetryResult */
CommercePaymentsPaymentRetryResultSchema = z.object({
  success: z.boolean().optional(),
  retryAttempt: z.number().int().optional(),
  nextRetryAt: z.string().datetime().nullable().optional(),
  paymentResult: z.lazy(() => CommercePaymentsPaymentResultSchema).optional(),
  maxRetriesReached: z.boolean().optional(),
  failureReason: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentStatus */
CommercePaymentsPaymentStatusSchema = z.enum(['Pending', 'Processing', 'Succeeded', 'Failed', 'Cancelled', 'RequiresAction', 'Refunded', 'Disputed']);

/** Zod schema for CommercePaymentsPaymentsControllerCancelPaymentInput */
CommercePaymentsPaymentsControllerCancelPaymentInputSchema = z.object({
  cancellationReason: z.string().nullable().optional(),
  canceledBy: z.string().uuid().nullable().optional(),
  notes: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInput */
CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInputSchema = z.object({
  tenantId: z.string().uuid().optional(),
  subscriptionId: z.string().uuid().optional(),
  paymentMethodId: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentsControllerCreateSetupIntentInput */
CommercePaymentsPaymentsControllerCreateSetupIntentInputSchema = z.object({
  tenantId: z.string().uuid().optional(),
  subscriptionId: z.string().uuid().optional(),
  customerEmail: z.string().nullable().optional(),
  customerName: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentsControllerCreateSetupIntentOutput */
CommercePaymentsPaymentsControllerCreateSetupIntentOutputSchema = z.object({
  subscriptionId: z.string().uuid().optional(),
  customerId: z.string().nullable().optional(),
  setupIntentId: z.string().nullable().optional(),
  clientSecret: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentsControllerProcessPaymentInput */
CommercePaymentsPaymentsControllerProcessPaymentInputSchema = z.object({
  tenantId: z.string().uuid().optional(),
  subscriptionId: z.string().uuid().optional(),
  amount: z.number().optional(),
  paymentMethodId: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsPaymentsControllerRefundInput */
CommercePaymentsPaymentsControllerRefundInputSchema = z.object({
  amount: z.number().nullable().optional(),
  reason: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsProcessRefundResult */
CommercePaymentsProcessRefundResultSchema = z.object({
  refundId: z.string().uuid(),
  paymentId: z.string().uuid(),
  refundedAmount: z.number(),
  currency: z.string().nullable(),
  status: z.lazy(() => CommercePaymentsTransactionStatusSchema),
  reason: z.string().nullable(),
  processedAt: z.string().datetime(),
  referenceNumber: z.string().nullable().optional(),
  estimatedCompletionDate: z.string().datetime().nullable().optional(),
  processingFee: z.number().optional(),
  isSuccess: z.boolean().optional(),
  errorMessage: z.string().nullable().optional(),
  isSuccessful: z.boolean().optional(),
});

/** Zod schema for CommercePaymentsTaxBreakdown */
CommercePaymentsTaxBreakdownSchema = z.object({
  taxType: z.lazy(() => CommercePaymentsTaxTypeSchema).optional(),
  description: z.string().nullable().optional(),
  rate: z.number().optional(),
  taxableAmount: z.number().optional(),
  taxAmount: z.number().optional(),
  jurisdictionCode: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsTaxCalculationResult */
CommercePaymentsTaxCalculationResultSchema = z.object({
  subtotalAmount: z.number().optional(),
  taxAmount: z.number().optional(),
  totalAmount: z.number().optional(),
  effectiveTaxRate: z.number().optional(),
  jurisdictionCode: z.string().nullable().optional(),
  jurisdictionName: z.string().nullable().optional(),
  taxType: z.lazy(() => CommercePaymentsTaxTypeSchema).optional(),
  taxDescription: z.string().nullable().optional(),
  isTaxExempt: z.boolean().optional(),
  isReverseCharge: z.boolean().optional(),
  taxBreakdowns: z
    .array(z.lazy(() => CommercePaymentsTaxBreakdownSchema))
    .nullable()
    .optional(),
  exemptionReason: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsTaxExemptionValidationResult */
CommercePaymentsTaxExemptionValidationResultSchema = z.object({
  isValid: z.boolean().optional(),
  exemptionType: z.string().nullable().optional(),
  exemptionRate: z.number().optional(),
  validFrom: z.string().datetime().nullable().optional(),
  validTo: z.string().datetime().nullable().optional(),
  validationMessage: z.string().nullable().optional(),
  warnings: z.array(z.string()).nullable().optional(),
});

/** Zod schema for CommercePaymentsTaxJurisdiction */
CommercePaymentsTaxJurisdictionSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  code: z.string().min(1).max(20),
  name: z.string().min(1).max(200),
  type: z.lazy(() => CommercePaymentsTaxJurisdictionTypeSchema).optional(),
  parentJurisdictionId: z.string().uuid().nullable().optional(),
  parentJurisdiction: z.lazy(() => CommercePaymentsTaxJurisdictionSchema).optional(),
  childJurisdictions: z
    .array(z.lazy(() => CommercePaymentsTaxJurisdictionSchema))
    .nullable()
    .optional(),
  isActive: z.boolean().optional(),
  taxRegistrationNumber: z.string().max(100).nullable().optional(),
  isReverseChargeApplicable: z.boolean().optional(),
  taxRules: z
    .array(z.lazy(() => CommercePaymentsTaxRuleSchema))
    .nullable()
    .optional(),
});

/** Zod schema for CommercePaymentsTaxJurisdictionType */
CommercePaymentsTaxJurisdictionTypeSchema = z.enum(['Country', 'State', 'Province', 'Region', 'City', 'County', 'District']);

/** Zod schema for CommercePaymentsTaxRate */
CommercePaymentsTaxRateSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  taxJurisdictionId: z.string().uuid(),
  taxJurisdiction: z.lazy(() => CommercePaymentsTaxJurisdictionSchema).optional(),
  taxType: z.lazy(() => CommercePaymentsTaxTypeSchema).optional(),
  rate: z.number().optional(),
  productCategory: z.string().max(100).nullable().optional(),
  effectiveFrom: z.string().datetime().optional(),
  effectiveTo: z.string().datetime().nullable().optional(),
  isActive: z.boolean().optional(),
  minimumTaxableAmount: z.number().nullable().optional(),
  maximumTaxableAmount: z.number().nullable().optional(),
  description: z.string().max(500).nullable().optional(),
});

/** Zod schema for CommercePaymentsTaxRule */
CommercePaymentsTaxRuleSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  name: z.string().min(1).max(200),
  description: z.string().max(1000).nullable().optional(),
  taxJurisdictionId: z.string().uuid(),
  taxJurisdiction: z.lazy(() => CommercePaymentsTaxJurisdictionSchema).optional(),
  ruleType: z.lazy(() => CommercePaymentsTaxRuleTypeSchema).optional(),
  priority: z.number().int().optional(),
  isActive: z.boolean().optional(),
  effectiveFrom: z.string().datetime().nullable().optional(),
  effectiveTo: z.string().datetime().nullable().optional(),
  customerTypeFilter: z.lazy(() => CommercePaymentsCustomerTypeSchema).optional(),
  productCategories: z.string().max(2000).nullable().optional(),
  minimumAmount: z.number().nullable().optional(),
  maximumAmount: z.number().nullable().optional(),
  isTaxInclusive: z.boolean().optional(),
  isReverseCharge: z.boolean().optional(),
  exemptionConditions: z.string().max(2000).nullable().optional(),
  defaultTaxRateId: z.string().uuid().nullable().optional(),
  defaultTaxRate: z.lazy(() => CommercePaymentsTaxRateSchema).optional(),
});

/** Zod schema for CommercePaymentsTaxRuleType */
CommercePaymentsTaxRuleTypeSchema = z.enum(['Standard', 'Reduced', 'ZeroRated', 'Exempt', 'ReverseCharge', 'WithholdingTax', 'Compound', 'Custom']);

/** Zod schema for CommercePaymentsTaxType */
CommercePaymentsTaxTypeSchema = z.enum(['VAT', 'GST', 'SalesTax', 'ServiceTax', 'WithholdingTax', 'ExciseTax', 'CustomsDuty', 'Other']);

/** Zod schema for CommercePaymentsTransactionStatus */
CommercePaymentsTransactionStatusSchema = z.enum(['Pending', 'Processing', 'Completed', 'Failed', 'Cancelled', 'Reversed']);

/** Zod schema for CommercePaymentsTransferFundsInput */
CommercePaymentsTransferFundsInputSchema = z.object({
  fromUserId: z.string().uuid(),
  toUserId: z.string().uuid(),
  amount: z.number(),
  description: z.string().nullable(),
  referenceId: z.string().nullable().optional(),
});

/** Zod schema for CommercePaymentsTransferResult */
CommercePaymentsTransferResultSchema = z.object({
  debitTransaction: z.lazy(() => CommercePaymentsWalletTransactionSchema).optional(),
  creditTransaction: z.lazy(() => CommercePaymentsWalletTransactionSchema).optional(),
});

/** Zod schema for CommercePaymentsUserWallet */
CommercePaymentsUserWalletSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  userId: z.string().uuid(),
  balance: z.number().optional(),
  currency: z.string().min(1).max(3),
  isActive: z.boolean().optional(),
  isLocked: z.boolean().optional(),
  lockReason: z.string().max(500).nullable().optional(),
  lastTransactionAt: z.string().datetime().nullable().optional(),
  dailyLimit: z.number().nullable().optional(),
  monthlyLimit: z.number().nullable().optional(),
  transactions: z
    .array(z.lazy(() => CommercePaymentsWalletTransactionSchema))
    .nullable()
    .optional(),
});

/** Zod schema for CommercePaymentsValidateTaxExemptionInput */
CommercePaymentsValidateTaxExemptionInputSchema = z.object({
  jurisdictionCode: z.string().nullable().optional(),
  exemptionType: z.string().nullable().optional(),
  exemptionCertificateNumber: z.string().nullable().optional(),
  customerVatNumber: z.string().nullable().optional(),
  customerId: z.string().uuid().nullable().optional(),
  transactionDate: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommercePaymentsWalletTransaction */
CommercePaymentsWalletTransactionSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  walletId: z.string().uuid(),
  wallet: z.lazy(() => CommercePaymentsUserWalletSchema).optional(),
  type: z.lazy(() => CommercePaymentsWalletTransactionTypeSchema).optional(),
  amount: z.number().optional(),
  balanceAfter: z.number().optional(),
  description: z.string().min(1).max(500),
  referenceId: z.string().max(200).nullable().optional(),
  status: z.lazy(() => CommercePaymentsTransactionStatusSchema).optional(),
  metadata: z.string().max(2000).nullable().optional(),
  notes: z.string().max(1000).nullable().optional(),
  processedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommercePaymentsWalletTransactionType */
CommercePaymentsWalletTransactionTypeSchema = z.enum(['Credit', 'Debit', 'TransferIn', 'TransferOut', 'Refund', 'Fee', 'Adjustment']);

/** Zod schema for CommerceProductsAppliedPromoCode */
CommerceProductsAppliedPromoCodeSchema = z.object({
  code: z.string().nullable().optional(),
  discountAmount: z.number().optional(),
  discountPercentage: z.number().nullable().optional(),
});

/** Zod schema for CommerceProductsApplyPromoCodesInput */
CommerceProductsApplyPromoCodesInputSchema = z.object({
  orderAmount: z.number().optional(),
  promoCodes: z.array(z.string()).nullable().optional(),
  productId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceProductsBatchCreateProductsInput */
CommerceProductsBatchCreateProductsInputSchema = z.object({
  products: z
    .array(z.lazy(() => CommerceProductsBatchProductCreateItemSchema))
    .nullable()
    .optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceProductsBatchProductCreateItem */
CommerceProductsBatchProductCreateItemSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  shortDescription: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsProductTypeSchema).optional(),
  isBundle: z.boolean().optional(),
  creatorId: z.string().uuid().nullable().optional(),
  bundleItems: z.array(z.string().uuid()).nullable().optional(),
  referralCommissionPercentage: z.number().optional(),
  maxAffiliateDiscount: z.number().optional(),
  affiliateCommissionPercentage: z.number().optional(),
});

/** Zod schema for CommerceProductsCheckMultipleAccessInput */
CommerceProductsCheckMultipleAccessInputSchema = z.object({
  productIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for CommerceProductsCreateProductInput */
CommerceProductsCreateProductInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  shortDescription: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsProductTypeSchema).optional(),
  isBundle: z.boolean().optional(),
  creatorId: z.string().uuid().nullable().optional(),
  bundleItems: z.array(z.string().uuid()).nullable().optional(),
  referralCommissionPercentage: z.number().optional(),
  maxAffiliateDiscount: z.number().optional(),
  affiliateCommissionPercentage: z.number().optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceProductsCreatePromoCodeInput */
CommerceProductsCreatePromoCodeInputSchema = z.object({
  code: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsPromoCodeTypeSchema).optional(),
  discountPercentage: z.number().nullable().optional(),
  discountAmount: z.number().nullable().optional(),
  currency: z.string().nullable().optional(),
  minimumOrderAmount: z.number().nullable().optional(),
  maxUses: z.number().int().nullable().optional(),
  maxUsesPerUser: z.number().int().nullable().optional(),
  validFrom: z.string().datetime().nullable().optional(),
  validUntil: z.string().datetime().nullable().optional(),
  isActive: z.boolean().optional(),
  isExclusive: z.boolean().optional(),
  stackingPriority: z.number().int().optional(),
  productId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceProductsEntitlementCheckResult */
CommerceProductsEntitlementCheckResultSchema = z.object({
  productId: z.string().uuid().optional(),
  hasAccess: z.boolean().optional(),
});

/** Zod schema for CommerceProductsEntitlementInfo */
CommerceProductsEntitlementInfoSchema = z.object({
  productId: z.string().uuid().optional(),
  productName: z.string().nullable().optional(),
  status: z.string().nullable().optional(),
  acquisitionType: z.string().nullable().optional(),
  accessStartDate: z.string().datetime().nullable().optional(),
  accessEndDate: z.string().datetime().nullable().optional(),
  isSubscription: z.boolean().optional(),
  subscriptionStatus: z.string().nullable().optional(),
  pricePaid: z.number().optional(),
  currency: z.string().nullable().optional(),
});

/** Zod schema for CommerceProductsGrantEntitlementInput */
CommerceProductsGrantEntitlementInputSchema = z.object({
  userId: z.string().uuid().optional(),
  productId: z.string().uuid().optional(),
  acquisitionType: z.lazy(() => CommerceProductsProductAcquisitionTypeSchema).optional(),
  pricePaid: z.number().optional(),
  currency: z.string().nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommerceProductsPatchProductInput */
CommerceProductsPatchProductInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  shortDescription: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsProductTypeSchema).optional(),
  isBundle: z.boolean().nullable().optional(),
  bundleItems: z.array(z.string().uuid()).nullable().optional(),
  referralCommissionPercentage: z.number().nullable().optional(),
  maxAffiliateDiscount: z.number().nullable().optional(),
  affiliateCommissionPercentage: z.number().nullable().optional(),
  expectedVersion: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceProductsPatchPromoCodeInput */
CommerceProductsPatchPromoCodeInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsPromoCodeTypeSchema).optional(),
  discountPercentage: z.number().nullable().optional(),
  discountAmount: z.number().nullable().optional(),
  currency: z.string().nullable().optional(),
  minimumOrderAmount: z.number().nullable().optional(),
  maxUses: z.number().int().nullable().optional(),
  maxUsesPerUser: z.number().int().nullable().optional(),
  validFrom: z.string().datetime().nullable().optional(),
  validUntil: z.string().datetime().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
  isExclusive: z.boolean().nullable().optional(),
  stackingPriority: z.number().int().nullable().optional(),
  productId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceProductsProductAcquisitionType */
CommerceProductsProductAcquisitionTypeSchema = z.enum(['Purchase', 'Subscription', 'Grant', 'PromoCode', 'Bundle', 'Trial', 'Referral', 'Free', 'Gift']);

/** Zod schema for CommerceProductsProduct */
CommerceProductsProductSchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  shortDescription: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsProductTypeSchema).optional(),
  isBundle: z.boolean().optional(),
  isPublished: z.boolean().optional(),
  creatorId: z.string().uuid().nullable().optional(),
  bundleItems: z.array(z.string().uuid()).nullable().optional(),
  referralCommissionPercentage: z.number().optional(),
  maxAffiliateDiscount: z.number().optional(),
  affiliateCommissionPercentage: z.number().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
  pricing: z
    .array(z.lazy(() => CommerceProductsProductPricingSchema))
    .nullable()
    .optional(),
});

/** Zod schema for CommerceProductsProductPricing */
CommerceProductsProductPricingSchema = z.object({
  id: z.string().uuid().optional(),
  productId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  basePrice: z.number().optional(),
  salePrice: z.number().nullable().optional(),
  currency: z.string().nullable().optional(),
  saleStartDate: z.string().datetime().nullable().optional(),
  saleEndDate: z.string().datetime().nullable().optional(),
  isDefault: z.boolean().optional(),
  currentPrice: z.number().optional(),
  isSaleActive: z.boolean().optional(),
});

/** Zod schema for CommerceProductsProductType */
CommerceProductsProductTypeSchema = z.enum([
  'Program',
  'Course',
  'Bundle',
  'Subscription',
  'Workshop',
  'Mentorship',
  'Ebook',
  'ResourcePack',
  'Community',
  'Certification',
  'Physical',
  'Service',
  'LearningPathway',
  'Other',
]);

/** Zod schema for CommerceProductsPromoCodeApplicationResult */
CommerceProductsPromoCodeApplicationResultSchema = z.object({
  originalAmount: z.number().optional(),
  finalAmount: z.number().optional(),
  totalDiscount: z.number().optional(),
  appliedCodes: z
    .array(z.lazy(() => CommerceProductsAppliedPromoCodeSchema))
    .nullable()
    .optional(),
  rejectedCodes: z
    .array(z.lazy(() => CommerceProductsRejectedPromoCodeSchema))
    .nullable()
    .optional(),
});

/** Zod schema for CommerceProductsPromoCode */
CommerceProductsPromoCodeSchema = z.object({
  id: z.string().uuid().optional(),
  code: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsPromoCodeTypeSchema).optional(),
  discountPercentage: z.number().nullable().optional(),
  discountAmount: z.number().nullable().optional(),
  currency: z.string().nullable().optional(),
  minimumOrderAmount: z.number().nullable().optional(),
  maxUses: z.number().int().nullable().optional(),
  maxUsesPerUser: z.number().int().nullable().optional(),
  validFrom: z.string().datetime().nullable().optional(),
  validUntil: z.string().datetime().nullable().optional(),
  isActive: z.boolean().optional(),
  isExclusive: z.boolean().optional(),
  stackingPriority: z.number().int().optional(),
  productId: z.string().uuid().nullable().optional(),
  usageCount: z.number().int().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for CommerceProductsPromoCodeType */
CommerceProductsPromoCodeTypeSchema = z.enum(['PercentageOff', 'FixedAmountOff', 'FreeTrial', 'BuyOneGetOne', 'FreeShipping']);

/** Zod schema for CommerceProductsPromoCodeUsage */
CommerceProductsPromoCodeUsageSchema = z.object({
  promoCodeId: z.string().uuid().optional(),
  code: z.string().nullable().optional(),
  totalUses: z.number().int().optional(),
  uniqueUsers: z.number().int().optional(),
  totalDiscountGiven: z.number().optional(),
  averageDiscountPerUse: z.number().optional(),
  maxUses: z.number().int().nullable().optional(),
  remainingUses: z.number().int().nullable().optional(),
  firstUsedAt: z.string().datetime().nullable().optional(),
  lastUsedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommerceProductsPromoCodeValidationResult */
CommerceProductsPromoCodeValidationResultSchema = z.object({
  isValid: z.boolean().optional(),
  code: z.string().nullable().optional(),
  errorMessage: z.string().nullable().optional(),
  discountAmount: z.number().optional(),
  discountPercentage: z.number().nullable().optional(),
});

/** Zod schema for CommerceProductsRejectedPromoCode */
CommerceProductsRejectedPromoCodeSchema = z.object({
  code: z.string().nullable().optional(),
  reason: z.string().nullable().optional(),
});

/** Zod schema for CommerceProductsRevokeEntitlementInput */
CommerceProductsRevokeEntitlementInputSchema = z.object({
  userId: z.string().uuid().optional(),
  productId: z.string().uuid().optional(),
  reason: z.string().nullable().optional(),
});

/** Zod schema for CommerceProductsUpdateProductInput */
CommerceProductsUpdateProductInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  shortDescription: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsProductTypeSchema).optional(),
  isBundle: z.boolean().nullable().optional(),
  bundleItems: z.array(z.string().uuid()).nullable().optional(),
  referralCommissionPercentage: z.number().nullable().optional(),
  maxAffiliateDiscount: z.number().nullable().optional(),
  affiliateCommissionPercentage: z.number().nullable().optional(),
  expectedVersion: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceProductsUpdatePromoCodeInput */
CommerceProductsUpdatePromoCodeInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => CommerceProductsPromoCodeTypeSchema).optional(),
  discountPercentage: z.number().nullable().optional(),
  discountAmount: z.number().nullable().optional(),
  currency: z.string().nullable().optional(),
  minimumOrderAmount: z.number().nullable().optional(),
  maxUses: z.number().int().nullable().optional(),
  maxUsesPerUser: z.number().int().nullable().optional(),
  validFrom: z.string().datetime().nullable().optional(),
  validUntil: z.string().datetime().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
  isExclusive: z.boolean().nullable().optional(),
  stackingPriority: z.number().int().nullable().optional(),
  productId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceProductsValidatePromoCodeInput */
CommerceProductsValidatePromoCodeInputSchema = z.object({
  code: z.string().nullable().optional(),
  orderAmount: z.number().optional(),
  productId: z.string().uuid().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsBillingHistory */
CommerceSubscriptionsBillingHistorySchema = z.object({
  id: z.string().uuid().optional(),
  subscriptionId: z.string().uuid().optional(),
  billingDate: z.string().datetime().optional(),
  amount: z.number().optional(),
  currency: z.string().nullable().optional(),
  status: z.string().nullable().optional(),
  externalPaymentId: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
});

/** Zod schema for CommerceSubscriptionsCancellationReason */
CommerceSubscriptionsCancellationReasonSchema = z.enum([
  'UserRequested',
  'PaymentFailed',
  'PlanDiscontinued',
  'PolicyViolation',
  'Downgrade',
  'TrialEnded',
  'Custom',
  'ExternalRequest',
]);

/** Zod schema for CommerceSubscriptionsSubscription */
CommerceSubscriptionsSubscriptionSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  createdByUserId: z.string().uuid(),
  fulfilledOrderId: z.string().uuid().nullable().optional(),
  lastModifyingOrderId: z.string().uuid().nullable().optional(),
  lastRenewalIdempotencyKey: z.string().max(100).nullable().optional(),
  lastPaymentIdempotencyKey: z.string().max(100).nullable().optional(),
  lockedPriceVersionId: z.string().uuid().nullable().optional(),
  lastProcessedBillingCycle: z.number().int().optional(),
  trialEndDate: z.string().datetime().nullable().optional(),
  cancellationReason: z.lazy(() => CommerceSubscriptionsCancellationReasonSchema).optional(),
  cancellationNote: z.string().max(1000).nullable().optional(),
  cancelledAt: z.string().datetime().nullable().optional(),
  externalId: z.string().max(100).nullable().optional(),
  externalCustomerId: z.string().max(100).nullable().optional(),
  autoRenew: z.boolean().optional(),
  currentPeriodStart: z.string().datetime().optional(),
  currentPeriodEnd: z.string().datetime().optional(),
  billingCycleCount: z.number().int().optional(),
  lastPaymentAt: z.string().datetime().nullable().optional(),
  metadata: z.string().max(2000).nullable().optional(),
  rowVersion: z.string().nullable().optional(),
  plan: z.lazy(() => CommerceSubscriptionsSubscriptionPlanSchema).optional(),
  status: z.lazy(() => CommerceSubscriptionsSubscriptionStatusSchema).optional(),
  planId: z.string().uuid(),
  billingCycle: z.lazy(() => BillingCycleSchema).optional(),
  amount: z.lazy(() => MoneySchema).optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().nullable().optional(),
  nextBillingDate: z.string().datetime().optional(),
  isActive: z.boolean().optional(),
  isTrialing: z.boolean().optional(),
  isCancelled: z.boolean().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionDowngradeResult */
CommerceSubscriptionsSubscriptionDowngradeResultSchema = z.object({
  success: z.boolean().optional(),
  updatedSubscription: z.lazy(() => CommerceSubscriptionsSubscriptionSchema).optional(),
  effectiveDate: z.string().datetime().nullable().optional(),
  creditIssued: z.lazy(() => MoneySchema).optional(),
  failureReason: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerAutoRenewInput */
CommerceSubscriptionsSubscriptionLifecycleControllerAutoRenewInputSchema = z.object({
  autoRenew: z.boolean().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerCancelInput */
CommerceSubscriptionsSubscriptionLifecycleControllerCancelInputSchema = z.object({
  reason: z.string().nullable().optional(),
  note: z.string().nullable().optional(),
  effectiveDate: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerDowngradeInput */
CommerceSubscriptionsSubscriptionLifecycleControllerDowngradeInputSchema = z.object({
  newPlanId: z.string().uuid().optional(),
  effectiveDate: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerEndTrialInput */
CommerceSubscriptionsSubscriptionLifecycleControllerEndTrialInputSchema = z.object({
  convertToPaid: z.boolean().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerExternalIdsInput */
CommerceSubscriptionsSubscriptionLifecycleControllerExternalIdsInputSchema = z.object({
  externalSubscriptionId: z.string().nullable().optional(),
  externalCustomerId: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerPauseSubscriptionInput */
CommerceSubscriptionsSubscriptionLifecycleControllerPauseSubscriptionInputSchema = z.object({
  pauseUntil: z.string().datetime().nullable().optional(),
  reason: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerStartTrialInput */
CommerceSubscriptionsSubscriptionLifecycleControllerStartTrialInputSchema = z.object({
  trialDays: z.number().int().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerSuspendInput */
CommerceSubscriptionsSubscriptionLifecycleControllerSuspendInputSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionLifecycleControllerUpgradeInput */
CommerceSubscriptionsSubscriptionLifecycleControllerUpgradeInputSchema = z.object({
  newPlanId: z.string().uuid().optional(),
  effectiveDate: z.string().datetime().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlan */
CommerceSubscriptionsSubscriptionPlanSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  externalId: z.string().max(100).nullable().optional(),
  isFeatured: z.boolean().optional(),
  sortOrder: z.number().int().optional(),
  hasPrioritySupport: z.boolean().optional(),
  hasAdvancedAnalytics: z.boolean().optional(),
  hasCustomBranding: z.boolean().optional(),
  features: z.string().max(2000).nullable().optional(),
  metadata: z.string().max(4000).nullable().optional(),
  trialPeriodDays: z.number().int().optional(),
  subscriptions: z
    .array(z.lazy(() => CommerceSubscriptionsSubscriptionSchema))
    .nullable()
    .optional(),
  name: z.string().min(1).max(100),
  slug: z.string().min(1).max(50),
  description: z.string().max(1000).nullable().optional(),
  monthlyPriceInCents: z.number().int().optional(),
  annualPriceInCents: z.number().int().nullable().optional(),
  currency: z.string().min(1).max(3),
  isActive: z.boolean().optional(),
  maxUsers: z.number().int().nullable().optional(),
  maxStorageMb: z.number().int().nullable().optional(),
  maxApiCallsPerMonth: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerCloneSubscriptionPlanInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerCloneSubscriptionPlanInputSchema = z.object({
  newName: z.string().nullable().optional(),
  newSlug: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerSetExternalIdInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerSetExternalIdInputSchema = z.object({
  externalId: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerSetFeaturedInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerSetFeaturedInputSchema = z.object({
  featured: z.boolean().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateDetailsInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateDetailsInputSchema = z.object({
  planId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  sortOrder: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateFeaturesInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateFeaturesInputSchema = z.object({
  hasPrioritySupport: z.boolean().nullable().optional(),
  hasAdvancedAnalytics: z.boolean().nullable().optional(),
  hasCustomBranding: z.boolean().nullable().optional(),
  features: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateLimitsInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdateLimitsInputSchema = z.object({
  maxUsers: z.number().int().nullable().optional(),
  maxStorageMb: z.number().int().nullable().optional(),
  maxApiCallsPerMonth: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdatePricingInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerUpdatePricingInputSchema = z.object({
  monthlyPriceInCents: z.number().int().optional(),
  annualPriceInCents: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlanOperationsControllerValidateLimitsInput */
CommerceSubscriptionsSubscriptionPlanOperationsControllerValidateLimitsInputSchema = z.object({
  users: z.number().int().optional(),
  storageMb: z.number().int().optional(),
  apiCalls: z.number().int().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlansCrudControllerComparePlansInput */
CommerceSubscriptionsSubscriptionPlansCrudControllerComparePlansInputSchema = z.object({
  basePlanId: z.string().uuid().optional(),
  comparePlanIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlansCrudControllerCreatePlanInput */
CommerceSubscriptionsSubscriptionPlansCrudControllerCreatePlanInputSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  monthlyPriceInCents: z.number().int().optional(),
  currency: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionPlansCrudControllerPutSubscriptionPlanInput */
CommerceSubscriptionsSubscriptionPlansCrudControllerPutSubscriptionPlanInputSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  monthlyPriceInCents: z.number().int().optional(),
  annualPriceInCents: z.number().int().nullable().optional(),
  maxUsers: z.number().int().nullable().optional(),
  maxStorageMb: z.number().int().nullable().optional(),
  maxApiCallsPerMonth: z.number().int().nullable().optional(),
  hasPrioritySupport: z.boolean().nullable().optional(),
  hasAdvancedAnalytics: z.boolean().nullable().optional(),
  hasCustomBranding: z.boolean().nullable().optional(),
  features: z.string().nullable().optional(),
  sortOrder: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionStatus */
CommerceSubscriptionsSubscriptionStatusSchema = z.enum(['PendingActivation', 'Active', 'Trialing', 'PastDue', 'Suspended', 'Cancelled', 'Expired']);

/** Zod schema for CommerceSubscriptionsSubscriptionUpgradeResult */
CommerceSubscriptionsSubscriptionUpgradeResultSchema = z.object({
  success: z.boolean().optional(),
  updatedSubscription: z.lazy(() => CommerceSubscriptionsSubscriptionSchema).optional(),
  proratedAmount: z.lazy(() => MoneySchema).optional(),
  creditApplied: z.lazy(() => MoneySchema).optional(),
  failureReason: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionUsage */
CommerceSubscriptionsSubscriptionUsageSchema = z.object({
  subscriptionId: z.string().uuid().optional(),
  usersCount: z.number().int().optional(),
  maxUsers: z.number().int().nullable().optional(),
  storageUsedMb: z.number().int().optional(),
  maxStorageMb: z.number().int().nullable().optional(),
  apiCallsThisMonth: z.number().int().optional(),
  maxApiCallsPerMonth: z.number().int().nullable().optional(),
  isOverLimit: z.boolean().optional(),
  limitWarnings: z.array(z.string()).nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionsControllerCreateSubscriptionInput */
CommerceSubscriptionsSubscriptionsControllerCreateSubscriptionInputSchema = z.object({
  tenantId: z.string().uuid().optional(),
  planId: z.string().uuid().optional(),
  createdByUserId: z.string().uuid().optional(),
  billingCycle: z.lazy(() => BillingCycleSchema).optional(),
  amount: z.number().optional(),
  currency: z.string().nullable().optional(),
  fulfilledOrderId: z.string().uuid().nullable().optional(),
  startDate: z.string().datetime().nullable().optional(),
  trialDays: z.number().int().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionsControllerPatchSubscriptionInput */
CommerceSubscriptionsSubscriptionsControllerPatchSubscriptionInputSchema = z.object({
  billingCycle: z.lazy(() => BillingCycleSchema).optional(),
  autoRenew: z.boolean().nullable().optional(),
  externalSubscriptionId: z.string().nullable().optional(),
  externalCustomerId: z.string().nullable().optional(),
  metadata: z.string().nullable().optional(),
});

/** Zod schema for CommerceSubscriptionsSubscriptionsControllerPutSubscriptionInput */
CommerceSubscriptionsSubscriptionsControllerPutSubscriptionInputSchema = z.object({
  planId: z.string().uuid().optional(),
  billingCycle: z.lazy(() => BillingCycleSchema).optional(),
  amount: z.number().optional(),
  autoRenew: z.boolean().optional(),
  externalSubscriptionId: z.string().nullable().optional(),
  externalCustomerId: z.string().nullable().optional(),
});

/** Zod schema for ComplianceFERPACompleteFerpaInspectionRequestBody */
ComplianceFERPACompleteFerpaInspectionRequestBodySchema = z.object({
  processedByUserId: z.string().uuid().optional(),
  approved: z.boolean().optional(),
  notes: z.string().nullable().optional(),
});

/** Zod schema for ComplianceFERPAEducationRecordKind */
ComplianceFERPAEducationRecordKindSchema = z.enum([
  'CourseEnrollment',
  'AssessmentSubmission',
  'Grade',
  'Certificate',
  'Attendance',
  'Communication',
  'SupportCase',
  'Custom',
]);

/** Zod schema for ComplianceFERPAFerpaDirectoryInformationPolicy */
ComplianceFERPAFerpaDirectoryInformationPolicySchema = z.object({
  id: z.string().uuid().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  allowedFieldsJson: z.string().nullable().optional(),
  optOutEnabled: z.boolean().optional(),
  annualNoticeSentAt: z.string().datetime().nullable().optional(),
  noticeUrl: z.string().nullable().optional(),
});

/** Zod schema for ComplianceFERPAFerpaDisclosureBasis */
ComplianceFERPAFerpaDisclosureBasisSchema = z.enum([
  'StudentConsent',
  'GuardianConsent',
  'SchoolOfficial',
  'FinancialAid',
  'HealthOrSafetyEmergency',
  'AuditOrEvaluation',
  'CourtOrder',
  'DirectoryInformation',
  'Other',
]);

/** Zod schema for ComplianceFERPAFerpaDisclosureConsent */
ComplianceFERPAFerpaDisclosureConsentSchema = z.object({
  id: z.string().uuid().optional(),
  studentUserId: z.string().uuid().optional(),
  guardianUserId: z.string().uuid().nullable().optional(),
  recipient: z.string().nullable().optional(),
  purpose: z.string().nullable().optional(),
  scope: z.string().nullable().optional(),
  effectiveFrom: z.string().datetime().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
  revokedAt: z.string().datetime().nullable().optional(),
  isActive: z.boolean().optional(),
});

/** Zod schema for ComplianceFERPAFerpaDisclosureLog */
ComplianceFERPAFerpaDisclosureLogSchema = z.object({
  id: z.string().uuid().optional(),
  studentUserId: z.string().uuid().optional(),
  disclosedByUserId: z.string().uuid().optional(),
  recipient: z.string().nullable().optional(),
  basis: z.lazy(() => ComplianceFERPAFerpaDisclosureBasisSchema).optional(),
  purpose: z.string().nullable().optional(),
  recordIdsJson: z.string().nullable().optional(),
  disclosedAt: z.string().datetime().optional(),
});

/** Zod schema for ComplianceFERPAFerpaEducationRecord */
ComplianceFERPAFerpaEducationRecordSchema = z.object({
  id: z.string().uuid().optional(),
  studentUserId: z.string().uuid().optional(),
  recordKind: z.lazy(() => ComplianceFERPAEducationRecordKindSchema).optional(),
  externalRecordId: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  protectionLevel: z.lazy(() => ComplianceFERPAFerpaRecordProtectionLevelSchema).optional(),
  isDirectoryInformation: z.boolean().optional(),
  retentionUntil: z.string().datetime().nullable().optional(),
  metadataJson: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
});

/** Zod schema for ComplianceFERPAFerpaInspectionInput */
ComplianceFERPAFerpaInspectionInputSchema = z.object({
  id: z.string().uuid().optional(),
  studentUserId: z.string().uuid().optional(),
  requestedByUserId: z.string().uuid().optional(),
  status: z.lazy(() => ComplianceFERPAFerpaRequestStatusSchema).optional(),
  deadline: z.string().datetime().optional(),
  processedByUserId: z.string().uuid().nullable().optional(),
  processedAt: z.string().datetime().nullable().optional(),
  processingNotes: z.string().nullable().optional(),
});

/** Zod schema for ComplianceFERPAFerpaRecordProtectionLevel */
ComplianceFERPAFerpaRecordProtectionLevelSchema = z.enum(['DirectoryInformation', 'EducationRecord', 'SensitiveEducationRecord', 'Restricted']);

/** Zod schema for ComplianceFERPAFerpaRequestStatus */
ComplianceFERPAFerpaRequestStatusSchema = z.enum(['Pending', 'InReview', 'Completed', 'Denied', 'Expired']);

/** Zod schema for ComplianceFERPAGrantFerpaDisclosureConsentCommand */
ComplianceFERPAGrantFerpaDisclosureConsentCommandSchema = z.object({
  studentUserId: z.string().uuid().optional(),
  recipient: z.string().nullable().optional(),
  purpose: z.string().nullable().optional(),
  scope: z.string().nullable().optional(),
  effectiveFrom: z.string().datetime().optional(),
  guardianUserId: z.string().uuid().nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for ComplianceFERPARecordFerpaDisclosureCommand */
ComplianceFERPARecordFerpaDisclosureCommandSchema = z.object({
  studentUserId: z.string().uuid().optional(),
  disclosedByUserId: z.string().uuid().optional(),
  recipient: z.string().nullable().optional(),
  basis: z.lazy(() => ComplianceFERPAFerpaDisclosureBasisSchema).optional(),
  purpose: z.string().nullable().optional(),
  scope: z.string().nullable().optional(),
  recordIdsJson: z.string().nullable().optional(),
  disclosedAt: z.string().datetime().optional(),
});

/** Zod schema for ComplianceFERPARegisterEducationRecordCommand */
ComplianceFERPARegisterEducationRecordCommandSchema = z.object({
  studentUserId: z.string().uuid().optional(),
  recordKind: z.lazy(() => ComplianceFERPAEducationRecordKindSchema).optional(),
  externalRecordId: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  protectionLevel: z.lazy(() => ComplianceFERPAFerpaRecordProtectionLevelSchema).optional(),
  isDirectoryInformation: z.boolean().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  retentionUntil: z.string().datetime().nullable().optional(),
  metadataJson: z.string().nullable().optional(),
});

/** Zod schema for ComplianceFERPASubmitFerpaInspectionRequestCommand */
ComplianceFERPASubmitFerpaInspectionRequestCommandSchema = z.object({
  studentUserId: z.string().uuid().optional(),
  requestedByUserId: z.string().uuid().optional(),
  deadline: z.string().datetime().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for ComplianceFERPAUpsertDirectoryInformationPolicyCommand */
ComplianceFERPAUpsertDirectoryInformationPolicyCommandSchema = z.object({
  tenantId: z.string().uuid().nullable().optional(),
  allowedFieldsJson: z.string().nullable().optional(),
  optOutEnabled: z.boolean().optional(),
  annualNoticeSentAt: z.string().datetime().nullable().optional(),
  noticeUrl: z.string().nullable().optional(),
});

/** Zod schema for ContentPagesContentResource */
ContentPagesContentResourceSchema = z.object({
  id: z.string().uuid().optional(),
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  summary: z.string().nullable().optional(),
  body: z.string().nullable().optional(),
  resourceType: z.string().nullable().optional(),
  status: z.string().nullable().optional(),
  locale: z.string().nullable().optional(),
  categorySlug: z.string().nullable().optional(),
  tags: z.string().nullable().optional(),
  authorId: z.string().uuid().nullable().optional(),
  authorName: z.string().nullable().optional(),
  coverImageUrl: z.string().nullable().optional(),
  videoUrl: z.string().nullable().optional(),
  downloadUrl: z.string().nullable().optional(),
  externalUrl: z.string().nullable().optional(),
  linkedEntityId: z.string().uuid().nullable().optional(),
  linkedEntityType: z.string().nullable().optional(),
  metaTitle: z.string().nullable().optional(),
  metaDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
  readingTimeMinutes: z.number().int().nullable().optional(),
  viewCount: z.number().int().optional(),
  isFeatured: z.boolean().optional(),
  sortOrder: z.number().int().optional(),
  publishedAt: z.string().datetime().nullable().optional(),
  scheduledPublishAt: z.string().datetime().nullable().optional(),
  customData: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for ContentPagesContentResourceStatus */
ContentPagesContentResourceStatusSchema = z.enum(['Draft', 'InReview', 'Published', 'Archived']);

/** Zod schema for ContentPagesContentResourceType */
ContentPagesContentResourceTypeSchema = z.enum(['Article', 'Tutorial', 'Documentation', 'Video', 'Download', 'ExternalLink', 'Course', 'Custom']);

/** Zod schema for ContentPagesCreateContentResource */
ContentPagesCreateContentResourceSchema = z.object({
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  summary: z.string().nullable().optional(),
  body: z.string().nullable().optional(),
  resourceType: z.lazy(() => ContentPagesContentResourceTypeSchema).optional(),
  locale: z.string().nullable().optional(),
  categorySlug: z.string().nullable().optional(),
  tags: z.string().nullable().optional(),
  coverImageUrl: z.string().nullable().optional(),
  videoUrl: z.string().nullable().optional(),
  downloadUrl: z.string().nullable().optional(),
  externalUrl: z.string().nullable().optional(),
  linkedEntityId: z.string().uuid().nullable().optional(),
  linkedEntityType: z.string().nullable().optional(),
  metaTitle: z.string().nullable().optional(),
  metaDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
  readingTimeMinutes: z.number().int().nullable().optional(),
  isFeatured: z.boolean().optional(),
  sortOrder: z.number().int().optional(),
  customData: z.string().nullable().optional(),
});

/** Zod schema for ContentPagesCreateMarketingLead */
ContentPagesCreateMarketingLeadSchema = z.object({
  source: z.string().min(1).max(40),
  name: z.string().max(120).nullable().optional(),
  email: z.string().email().min(1).max(200),
  company: z.string().max(200).nullable().optional(),
  topic: z.string().max(40).nullable().optional(),
  plan: z.string().max(60).nullable().optional(),
  message: z.string().max(4000).nullable().optional(),
  locale: z.string().max(10).nullable().optional(),
  pagePath: z.string().max(300).nullable().optional(),
  referrer: z.string().max(2000).nullable().optional(),
  userAgent: z.string().max(500).nullable().optional(),
});

/** Zod schema for ContentPagesCreatePage */
ContentPagesCreatePageSchema = z.object({
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  pageType: z.lazy(() => ContentPagesPageTypeSchema).optional(),
  locale: z.string().nullable().optional(),
  metaTitle: z.string().nullable().optional(),
  metaDescription: z.string().nullable().optional(),
  metaKeywords: z.string().nullable().optional(),
  canonicalUrl: z.string().nullable().optional(),
  robotsDirective: z.string().nullable().optional(),
  ogTitle: z.string().nullable().optional(),
  ogDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  ogType: z.string().nullable().optional(),
  twitterCard: z.string().nullable().optional(),
  twitterSite: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
  body: z.string().nullable().optional(),
  customData: z.string().nullable().optional(),
  parentPageId: z.string().uuid().nullable().optional(),
  sortOrder: z.number().int().optional(),
});

/** Zod schema for ContentPagesCreatePageSection */
ContentPagesCreatePageSectionSchema = z.object({
  sectionType: z.lazy(() => ContentPagesSectionTypeSchema).optional(),
  heading: z.string().nullable().optional(),
  subheading: z.string().nullable().optional(),
  data: z.string().nullable().optional(),
  sortOrder: z.number().int().optional(),
  isVisible: z.boolean().optional(),
  cssClasses: z.string().nullable().optional(),
});

/** Zod schema for ContentPagesMarketingLead */
ContentPagesMarketingLeadSchema = z.object({
  id: z.string().uuid().optional(),
  source: z.string().nullable().optional(),
  status: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  email: z.string().nullable().optional(),
  company: z.string().nullable().optional(),
  topic: z.string().nullable().optional(),
  plan: z.string().nullable().optional(),
  message: z.string().nullable().optional(),
  locale: z.string().nullable().optional(),
  pagePath: z.string().nullable().optional(),
  referrer: z.string().nullable().optional(),
  userAgent: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for ContentPagesOpenGraphMetadata */
ContentPagesOpenGraphMetadataSchema = z.object({
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  ogTitle: z.string().nullable().optional(),
  ogDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  ogType: z.string().nullable().optional(),
  twitterCard: z.string().nullable().optional(),
  twitterSite: z.string().nullable().optional(),
  canonicalUrl: z.string().nullable().optional(),
  robotsDirective: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
});

/** Zod schema for ContentPagesPage */
ContentPagesPageSchema = z.object({
  id: z.string().uuid().optional(),
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  pageType: z.string().nullable().optional(),
  status: z.string().nullable().optional(),
  locale: z.string().nullable().optional(),
  metaTitle: z.string().nullable().optional(),
  metaDescription: z.string().nullable().optional(),
  metaKeywords: z.string().nullable().optional(),
  canonicalUrl: z.string().nullable().optional(),
  robotsDirective: z.string().nullable().optional(),
  ogTitle: z.string().nullable().optional(),
  ogDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  ogType: z.string().nullable().optional(),
  twitterCard: z.string().nullable().optional(),
  twitterSite: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
  body: z.string().nullable().optional(),
  customData: z.string().nullable().optional(),
  parentPageId: z.string().uuid().nullable().optional(),
  sortOrder: z.number().int().optional(),
  sections: z
    .array(z.lazy(() => ContentPagesPageSectionSchema))
    .nullable()
    .optional(),
  publishedAt: z.string().datetime().nullable().optional(),
  scheduledPublishAt: z.string().datetime().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for ContentPagesPageSection */
ContentPagesPageSectionSchema = z.object({
  id: z.string().uuid().optional(),
  pageId: z.string().uuid().optional(),
  sectionType: z.string().nullable().optional(),
  heading: z.string().nullable().optional(),
  subheading: z.string().nullable().optional(),
  data: z.string().nullable().optional(),
  sortOrder: z.number().int().optional(),
  isVisible: z.boolean().optional(),
  cssClasses: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for ContentPagesPageStatus */
ContentPagesPageStatusSchema = z.enum(['Draft', 'Published', 'Archived']);

/** Zod schema for ContentPagesPageType */
ContentPagesPageTypeSchema = z.enum(['Landing', 'Legal', 'ResourceIndex', 'Resource', 'Custom']);

/** Zod schema for ContentPagesSectionType */
ContentPagesSectionTypeSchema = z.enum([
  'Hero',
  'Features',
  'Testimonials',
  'Pricing',
  'CallToAction',
  'Faq',
  'RichText',
  'Gallery',
  'Stats',
  'Team',
  'LogoCloud',
  'Newsletter',
  'Contact',
  'ResourceCards',
  'Custom',
]);

/** Zod schema for ContentPagesSitemapEntry */
ContentPagesSitemapEntrySchema = z.object({
  slug: z.string().nullable().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  locale: z.string().nullable().optional(),
});

/** Zod schema for ContentPagesUpdateContentResource */
ContentPagesUpdateContentResourceSchema = z.object({
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  summary: z.string().nullable().optional(),
  body: z.string().nullable().optional(),
  resourceType: z.lazy(() => ContentPagesContentResourceTypeSchema).optional(),
  status: z.lazy(() => ContentPagesContentResourceStatusSchema).optional(),
  locale: z.string().nullable().optional(),
  categorySlug: z.string().nullable().optional(),
  tags: z.string().nullable().optional(),
  coverImageUrl: z.string().nullable().optional(),
  videoUrl: z.string().nullable().optional(),
  downloadUrl: z.string().nullable().optional(),
  externalUrl: z.string().nullable().optional(),
  linkedEntityId: z.string().uuid().nullable().optional(),
  linkedEntityType: z.string().nullable().optional(),
  metaTitle: z.string().nullable().optional(),
  metaDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
  readingTimeMinutes: z.number().int().nullable().optional(),
  isFeatured: z.boolean().nullable().optional(),
  sortOrder: z.number().int().nullable().optional(),
  scheduledPublishAt: z.string().datetime().nullable().optional(),
  customData: z.string().nullable().optional(),
});

/** Zod schema for ContentPagesUpdatePage */
ContentPagesUpdatePageSchema = z.object({
  slug: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  pageType: z.lazy(() => ContentPagesPageTypeSchema).optional(),
  status: z.lazy(() => ContentPagesPageStatusSchema).optional(),
  locale: z.string().nullable().optional(),
  metaTitle: z.string().nullable().optional(),
  metaDescription: z.string().nullable().optional(),
  metaKeywords: z.string().nullable().optional(),
  canonicalUrl: z.string().nullable().optional(),
  robotsDirective: z.string().nullable().optional(),
  ogTitle: z.string().nullable().optional(),
  ogDescription: z.string().nullable().optional(),
  ogImageUrl: z.string().nullable().optional(),
  ogType: z.string().nullable().optional(),
  twitterCard: z.string().nullable().optional(),
  twitterSite: z.string().nullable().optional(),
  structuredData: z.string().nullable().optional(),
  body: z.string().nullable().optional(),
  customData: z.string().nullable().optional(),
  parentPageId: z.string().uuid().nullable().optional(),
  sortOrder: z.number().int().nullable().optional(),
  scheduledPublishAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for ContentPagesUpdatePageSection */
ContentPagesUpdatePageSectionSchema = z.object({
  sectionType: z.lazy(() => ContentPagesSectionTypeSchema).optional(),
  heading: z.string().nullable().optional(),
  subheading: z.string().nullable().optional(),
  data: z.string().nullable().optional(),
  sortOrder: z.number().int().nullable().optional(),
  isVisible: z.boolean().nullable().optional(),
  cssClasses: z.string().nullable().optional(),
});

/** Zod schema for FeaturesBulkEvaluationInput */
FeaturesBulkEvaluationInputSchema = z.object({
  featureKeys: z.array(z.string()).nullable().optional(),
  context: z.lazy(() => FeaturesFeatureContextSchema).optional(),
});

/** Zod schema for FeaturesCapabilityAuditLog */
FeaturesCapabilityAuditLogSchema = z.object({
  id: z.string().uuid().optional(),
  tenantId: z.string().uuid().optional(),
  capabilityKey: z.string().nullable().optional(),
  oldValue: z.boolean().nullable().optional(),
  newValue: z.boolean().optional(),
  oldSource: z.string().nullable().optional(),
  newSource: z.string().nullable().optional(),
  changedByUserId: z.string().uuid().nullable().optional(),
  changeReason: z.string().nullable().optional(),
  changeType: z.string().nullable().optional(),
  changedAt: z.string().datetime().optional(),
});

/** Zod schema for FeaturesCapabilityCheckOutput */
FeaturesCapabilityCheckOutputSchema = z.object({
  capability: z.string().nullable().optional(),
  isEnabled: z.boolean().optional(),
});

/** Zod schema for FeaturesCreateFeatureInput */
FeaturesCreateFeatureInputSchema = z.object({
  key: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  isEnabled: z.boolean().optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for FeaturesFeatureContext */
FeaturesFeatureContextSchema = z.object({
  tenantId: z.string().uuid().nullable().optional(),
  userId: z.string().uuid().nullable().optional(),
  subscriptionPlanId: z.string().nullable().optional(),
  environment: z.string().nullable().optional(),
  permissions: z.array(z.string()).nullable().optional(),
  customAttributes: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  userAgent: z.string().nullable().optional(),
  ipAddress: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  requestTime: z.string().datetime().optional(),
});

/** Zod schema for FeaturesFeatureEvaluationInput */
FeaturesFeatureEvaluationInputSchema = z.object({
  featureKey: z.string().nullable().optional(),
  defaultValue: z.record(z.string(), z.unknown()).nullable().optional(),
  context: z.lazy(() => FeaturesFeatureContextSchema).optional(),
});

/** Zod schema for FeaturesFeatureFlag */
FeaturesFeatureFlagSchema = z.object({
  id: z.string().uuid(),
  key: z.string().nullable(),
  name: z.string().nullable(),
  description: z.string().nullable().optional(),
  isEnabled: z.boolean(),
  type: z.lazy(() => FeaturesFeatureFlagTypeSchema),
  environment: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  defaultValue: z.record(z.string(), z.unknown()).nullable().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime().nullable().optional(),
  deletedAt: z.string().datetime().nullable().optional(),
  targets: z
    .array(z.lazy(() => FeaturesFeatureFlagTargetSchema))
    .nullable()
    .optional(),
});

/** Zod schema for FeaturesFeatureFlagTarget */
FeaturesFeatureFlagTargetSchema = z.object({
  id: z.string().uuid(),
  featureFlagId: z.string().uuid(),
  targetType: z.string().nullable(),
  targetIdentifier: z.string().nullable(),
  isEnabled: z.boolean(),
  rolloutPercentage: z.number().int().optional(),
  customValue: z.string().nullable().optional(),
  metadata: z.string().nullable().optional(),
  priority: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime().nullable().optional(),
  deletedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for FeaturesFeatureFlagType */
FeaturesFeatureFlagTypeSchema = z.enum(['Toggle', 'Numeric', 'String', 'Percentage', 'UserSegment']);

/** Zod schema for FeaturesSetCapabilityOverrideInput */
FeaturesSetCapabilityOverrideInputSchema = z.object({
  capability: z.string().nullable().optional(),
  isEnabled: z.boolean().optional(),
  source: z.string().nullable().optional(),
  reason: z.string().nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for FeaturesToggleFeatureInput */
FeaturesToggleFeatureInputSchema = z.object({
  featureKey: z.string().nullable().optional(),
  isEnabled: z.boolean().optional(),
  reason: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  environment: z.string().nullable().optional(),
});

/** Zod schema for FeaturesUpdateFeatureInput */
FeaturesUpdateFeatureInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  isEnabled: z.boolean().nullable().optional(),
  rolloutPercentage: z.number().int().nullable().optional(),
  enabledValue: z.string().nullable().optional(),
  defaultValue: z.string().nullable().optional(),
});

/** Zod schema for Fido2NetLibAssertionOptions */
Fido2NetLibAssertionOptionsSchema = z.object({
  challenge: z.string().nullable().optional(),
  timeout: z.number().int().optional(),
  rpId: z.string().nullable().optional(),
  allowCredentials: z
    .array(z.lazy(() => ObjectsPublicKeyCredentialDescriptorSchema))
    .nullable()
    .optional(),
  userVerification: z.lazy(() => ObjectsUserVerificationRequirementSchema).optional(),
  hints: z
    .array(z.lazy(() => ObjectsPublicKeyCredentialHintSchema))
    .nullable()
    .optional(),
  extensions: z.lazy(() => ObjectsAuthenticationExtensionsClientInputsSchema).optional(),
});

/** Zod schema for Fido2NetLibAuthenticatorSelection */
Fido2NetLibAuthenticatorSelectionSchema = z.object({
  authenticatorAttachment: z.lazy(() => ObjectsAuthenticatorAttachmentSchema).optional(),
  residentKey: z.lazy(() => ObjectsResidentKeyRequirementSchema).optional(),
  requireResidentKey: z.boolean().optional(),
  userVerification: z.lazy(() => ObjectsUserVerificationRequirementSchema).optional(),
});

/** Zod schema for Fido2NetLibCredentialCreateOptions */
Fido2NetLibCredentialCreateOptionsSchema = z.object({
  rp: z.lazy(() => Fido2NetLibPublicKeyCredentialRpEntitySchema),
  user: z.lazy(() => Fido2NetLibFido2UserSchema),
  challenge: z.string().nullable(),
  pubKeyCredParams: z.array(z.lazy(() => Fido2NetLibPubKeyCredParamSchema)).nullable(),
  timeout: z.number().int().optional(),
  attestation: z.lazy(() => ObjectsAttestationConveyancePreferenceSchema).optional(),
  attestationFormats: z
    .array(z.lazy(() => ObjectsAttestationStatementFormatIdentifierSchema))
    .nullable()
    .optional(),
  authenticatorSelection: z.lazy(() => Fido2NetLibAuthenticatorSelectionSchema).optional(),
  hints: z
    .array(z.lazy(() => ObjectsPublicKeyCredentialHintSchema))
    .nullable()
    .optional(),
  excludeCredentials: z
    .array(z.lazy(() => ObjectsPublicKeyCredentialDescriptorSchema))
    .nullable()
    .optional(),
  extensions: z.lazy(() => ObjectsAuthenticationExtensionsClientInputsSchema).optional(),
});

/** Zod schema for Fido2NetLibFido2User */
Fido2NetLibFido2UserSchema = z.object({
  name: z.string().nullable().optional(),
  id: z.string().nullable().optional(),
  displayName: z.string().nullable().optional(),
});

/** Zod schema for Fido2NetLibPubKeyCredParam */
Fido2NetLibPubKeyCredParamSchema = z.object({
  type: z.lazy(() => ObjectsPublicKeyCredentialTypeSchema).optional(),
  alg: z.lazy(() => ObjectsCOSEAlgorithmSchema).optional(),
});

/** Zod schema for Fido2NetLibPublicKeyCredentialRpEntity */
Fido2NetLibPublicKeyCredentialRpEntitySchema = z.object({
  id: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  icon: z.string().nullable().optional(),
});

/** Zod schema for GameJamsAddJamCriteriaInput */
GameJamsAddJamCriteriaInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  weight: z.number().optional(),
  maxScore: z.number().int().optional(),
});

/** Zod schema for GameJamsCreateJamInput */
GameJamsCreateJamInputSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  createdBy: z.string().uuid().optional(),
  theme: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  rules: z.string().nullable().optional(),
  submissionCriteria: z.string().nullable().optional(),
  votingEndDate: z.string().datetime().nullable().optional(),
  maxParticipants: z.number().int().nullable().optional(),
});

/** Zod schema for GameJamsJamCriteria */
GameJamsJamCriteriaSchema = z.object({
  id: z.string().uuid().optional(),
  jamId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  weight: z.number().optional(),
  maxScore: z.number().int().optional(),
});

/** Zod schema for GameJamsJam */
GameJamsJamSchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  theme: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  votingEndDate: z.string().datetime().nullable().optional(),
  maxParticipants: z.number().int().nullable().optional(),
  participantCount: z.number().int().optional(),
  status: z.lazy(() => GameJamsJamStatusSchema).optional(),
  createdBy: z.string().uuid().optional(),
});

/** Zod schema for GameJamsJamScore */
GameJamsJamScoreSchema = z.object({
  id: z.string().uuid().optional(),
  submissionId: z.string().uuid().optional(),
  criteriaId: z.string().uuid().optional(),
  judgeUserId: z.string().uuid().optional(),
  score: z.number().int().optional(),
  feedback: z.string().nullable().optional(),
});

/** Zod schema for GameJamsJamStatus */
GameJamsJamStatusSchema = z.enum(['Upcoming', 'Active', 'Voting', 'Completed', 'Cancelled']);

/** Zod schema for GameJamsJamSubmission */
GameJamsJamSubmissionSchema = z.object({
  id: z.string().uuid().optional(),
  jamId: z.string().uuid().optional(),
  projectVersionId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  submissionNotes: z.string().nullable().optional(),
});

/** Zod schema for GameJamsScoreJamSubmissionInput */
GameJamsScoreJamSubmissionInputSchema = z.object({
  criteriaId: z.string().uuid().optional(),
  judgeUserId: z.string().uuid().optional(),
  score: z.number().int().optional(),
  feedback: z.string().nullable().optional(),
});

/** Zod schema for GameJamsSubmitJamEntryInput */
GameJamsSubmitJamEntryInputSchema = z.object({
  projectVersionId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  notes: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationApiKey */
IdentityAuthenticationApiKeySchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  keyPrefix: z.string().nullable().optional(),
  scopes: z.array(z.string()).nullable().optional(),
  isActive: z.boolean().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
  lastUsedAt: z.string().datetime().nullable().optional(),
  usageCount: z.number().int().optional(),
  createdAt: z.string().datetime().optional(),
});

/** Zod schema for IdentityAuthenticationBackupCodesOutput */
IdentityAuthenticationBackupCodesOutputSchema = z.object({
  codes: z.array(z.string()).nullable().optional(),
  generatedAt: z.string().datetime().optional(),
});

/** Zod schema for IdentityAuthenticationBackupCodesStatusOutput */
IdentityAuthenticationBackupCodesStatusOutputSchema = z.object({
  totalCount: z.number().int(),
  remainingCount: z.number().int(),
  usedCount: z.number().int(),
  hasBackupCodes: z.boolean(),
});

/** Zod schema for IdentityAuthenticationBeginWebAuthnAuthenticationInput */
IdentityAuthenticationBeginWebAuthnAuthenticationInputSchema = z.object({
  email: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationBeginWebAuthnRegistrationInput */
IdentityAuthenticationBeginWebAuthnRegistrationInputSchema = z.object({
  email: z.string().nullable().optional(),
  displayName: z.string().nullable().optional(),
  preferredAuthenticatorType: z.lazy(() => IdentityAuthenticationWebAuthnAuthenticatorTypeSchema).optional(),
});

/** Zod schema for IdentityAuthenticationCleanupKeysInput */
IdentityAuthenticationCleanupKeysInputSchema = z.object({
  retentionDays: z.number().int().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationCleanupResult */
IdentityAuthenticationCleanupResultSchema = z.object({
  deletedCount: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationClientCredentialsTokenOutput */
IdentityAuthenticationClientCredentialsTokenOutputSchema = z.object({
  accessToken: z.string().nullable().optional(),
  tokenType: z.string().nullable().optional(),
  expiresIn: z.number().int().optional(),
  scope: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationCompleteMfaSetupInput */
IdentityAuthenticationCompleteMfaSetupInputSchema = z.object({
  code: z.string().min(1),
  secretKey: z.string().min(1),
});

/** Zod schema for IdentityAuthenticationCompletePasswordResetInput */
IdentityAuthenticationCompletePasswordResetInputSchema = z.object({
  token: z.string().min(1),
  newPassword: z.string().min(8),
  confirmPassword: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationCompleteWebAuthnAuthenticationInput */
IdentityAuthenticationCompleteWebAuthnAuthenticationInputSchema = z.object({
  assertionResponse: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationCompleteWebAuthnRegistrationInput */
IdentityAuthenticationCompleteWebAuthnRegistrationInputSchema = z.object({
  attestationResponse: z.string().nullable().optional(),
  friendlyName: z.string().nullable().optional(),
  isPasswordless: z.boolean().optional(),
});

/** Zod schema for IdentityAuthenticationConsumeMagicLinkInput */
IdentityAuthenticationConsumeMagicLinkInputSchema = z.object({
  token: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
  deviceFingerprint: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationCreateApiKeyCommand */
IdentityAuthenticationCreateApiKeyCommandSchema = z.object({
  name: z.string().nullable(),
  scopes: z.array(z.string()).nullable(),
  expiresAt: z.string().datetime().nullable().optional(),
  ipWhitelist: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationCreateApiKeyOutput */
IdentityAuthenticationCreateApiKeyOutputSchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  apiKey: z.string().nullable().optional(),
  keyPrefix: z.string().nullable().optional(),
  scopes: z.array(z.string()).nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
  createdAt: z.string().datetime().optional(),
});

/** Zod schema for IdentityAuthenticationCreateServiceAccountInput */
IdentityAuthenticationCreateServiceAccountInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  scopes: z.string().nullable().optional(),
  allowedIpAddresses: z.string().nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationDeviceInfo */
IdentityAuthenticationDeviceInfoSchema = z.object({
  fingerprint: z.string().nullable().optional(),
  deviceId: z.string().nullable().optional(),
  ipAddress: z.string().nullable().optional(),
  deviceName: z.string().nullable().optional(),
  deviceType: z.string().nullable().optional(),
  operatingSystem: z.string().nullable().optional(),
  osVersion: z.string().nullable().optional(),
  browser: z.string().nullable().optional(),
  browserVersion: z.string().nullable().optional(),
  screenResolution: z.string().nullable().optional(),
  timezone: z.string().nullable().optional(),
  language: z.string().nullable().optional(),
  userAgent: z.string().nullable().optional(),
  isMobile: z.boolean().optional(),
  isBot: z.boolean().optional(),
});

/** Zod schema for IdentityAuthenticationDisableMfaInput */
IdentityAuthenticationDisableMfaInputSchema = z.object({
  password: z.string().min(1),
});

/** Zod schema for IdentityAuthenticationEmailVerificationOutput */
IdentityAuthenticationEmailVerificationOutputSchema = z.object({
  message: z.string().nullable(),
});

/** Zod schema for IdentityAuthenticationEmailVerificationResult */
IdentityAuthenticationEmailVerificationResultSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  email: z.string().nullable().optional(),
  userId: z.string().uuid().nullable().optional(),
  verifiedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationGitHubSignInOutput */
IdentityAuthenticationGitHubSignInOutputSchema = z.object({
  authUrl: z.string().nullable(),
});

/** Zod schema for IdentityAuthenticationGoogleIdTokenInput */
IdentityAuthenticationGoogleIdTokenInputSchema = z.object({
  idToken: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationJwtKeyInfo */
IdentityAuthenticationJwtKeyInfoSchema = z.object({
  keyId: z.string().nullable().optional(),
  algorithm: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
  validFrom: z.string().datetime().optional(),
  expiresAt: z.string().datetime().optional(),
  rotatedAt: z.string().datetime().nullable().optional(),
  rotationReason: z.string().nullable().optional(),
  keyVersion: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationLocalSignInInput */
IdentityAuthenticationLocalSignInInputSchema = z.object({
  username: z.string().nullable().optional(),
  email: z.string().email().min(1),
  password: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
  deviceFingerprint: z.string().nullable().optional(),
  emailOrUsername: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationLocalSignUpInput */
IdentityAuthenticationLocalSignUpInputSchema = z.object({
  username: z.string().min(1),
  email: z.string().email().min(1),
  password: z.string().min(8),
  tenantId: z.string().uuid().nullable().optional(),
  firstName: z.string().nullable().optional(),
  lastName: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationLocationInfo */
IdentityAuthenticationLocationInfoSchema = z.object({
  ipAddress: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  countryCode: z.string().nullable().optional(),
  region: z.string().nullable().optional(),
  city: z.string().nullable().optional(),
  postalCode: z.string().nullable().optional(),
  latitude: z.number().nullable().optional(),
  longitude: z.number().nullable().optional(),
  timezone: z.string().nullable().optional(),
  isp: z.string().nullable().optional(),
  organization: z.string().nullable().optional(),
  isProxy: z.boolean().nullable().optional(),
  isHosting: z.boolean().nullable().optional(),
  displayLocation: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationLockServiceAccountInput */
IdentityAuthenticationLockServiceAccountInputSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationMagicLinkRequestResult */
IdentityAuthenticationMagicLinkRequestResultSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  expiresInMinutes: z.number().int().optional(),
  developmentPreviewToken: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationMfaConfigurationOutput */
IdentityAuthenticationMfaConfigurationOutputSchema = z.object({
  isEnabled: z.boolean().optional(),
  enabledMethods: z.array(z.string()).nullable().optional(),
  enabledAt: z.string().datetime().nullable().optional(),
  backupCodesRemaining: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationMfaMethod */
IdentityAuthenticationMfaMethodSchema = z.enum(['Totp', 'BackupCode', 'Sms', 'Email', 'WebAuthn']);

/** Zod schema for IdentityAuthenticationMfaMethodInfo */
IdentityAuthenticationMfaMethodInfoSchema = z.object({
  method: z.lazy(() => IdentityAuthenticationMfaMethodSchema),
  name: z.string().nullable(),
  description: z.string().nullable(),
  isEnabled: z.boolean(),
  isAvailable: z.boolean(),
  priority: z.number().int(),
});

/** Zod schema for IdentityAuthenticationMfaMethodsOutput */
IdentityAuthenticationMfaMethodsOutputSchema = z.object({
  methods: z.array(z.lazy(() => IdentityAuthenticationMfaMethodInfoSchema)).nullable(),
  defaultMethod: z.lazy(() => IdentityAuthenticationMfaMethodSchema).optional(),
});

/** Zod schema for IdentityAuthenticationMfaSetupOutput */
IdentityAuthenticationMfaSetupOutputSchema = z.object({
  isSuccess: z.boolean().optional(),
  errorMessage: z.string().nullable().optional(),
  secretKey: z.string().nullable().optional(),
  qrCodeData: z.string().nullable().optional(),
  qrCodeUri: z.string().nullable().optional(),
  backupCodes: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityAuthenticationMfaSuccessOutput */
IdentityAuthenticationMfaSuccessOutputSchema = z.object({
  message: z.string().nullable(),
});

/** Zod schema for IdentityAuthenticationMfaVerificationOutput */
IdentityAuthenticationMfaVerificationOutputSchema = z.object({
  isValid: z.boolean().optional(),
  accessToken: z.string().nullable().optional(),
  refreshToken: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationOAuth2ErrorOutput */
IdentityAuthenticationOAuth2ErrorOutputSchema = z.object({
  error: z.string().nullable().optional(),
  errorDescription: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationPasswordChangeInput */
IdentityAuthenticationPasswordChangeInputSchema = z.object({
  currentPassword: z.string().min(1),
  newPassword: z.string().min(8),
  confirmPassword: z.string().min(1),
  revokeOtherSessions: z.boolean().optional(),
});

/** Zod schema for IdentityAuthenticationPasswordChangeResult */
IdentityAuthenticationPasswordChangeResultSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  sessionsRevoked: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationPasswordResetRequestResult */
IdentityAuthenticationPasswordResetRequestResultSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  expiresInMinutes: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationPasswordResetResult */
IdentityAuthenticationPasswordResetResultSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  userId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationPatchServiceAccountInput */
IdentityAuthenticationPatchServiceAccountInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  scopes: z.string().nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationRefreshTokenInput */
IdentityAuthenticationRefreshTokenInputSchema = z.object({
  refreshToken: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationRequestMagicLinkInput */
IdentityAuthenticationRequestMagicLinkInputSchema = z.object({
  email: z.string().email().min(1),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationRequestPasswordResetInput */
IdentityAuthenticationRequestPasswordResetInputSchema = z.object({
  email: z.string().email().min(1),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationRevokeApiKeyInput */
IdentityAuthenticationRevokeApiKeyInputSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationRevokeRefreshTokenInput */
IdentityAuthenticationRevokeRefreshTokenInputSchema = z.object({
  token: z.string().min(1),
  ipAddress: z.string().nullable().optional(),
  reason: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationRiskLevel */
IdentityAuthenticationRiskLevelSchema = z.enum(['Low', 'Medium', 'High', 'Critical']);

/** Zod schema for IdentityAuthenticationRotateKeyInput */
IdentityAuthenticationRotateKeyInputSchema = z.object({
  reason: z.string().nullable().optional(),
  validityDays: z.number().int().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationSecretRotationOutput */
IdentityAuthenticationSecretRotationOutputSchema = z.object({
  clientSecret: z.string().nullable().optional(),
  warning: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationSendEmailVerificationInput */
IdentityAuthenticationSendEmailVerificationInputSchema = z.object({
  email: z.string().email().min(1),
});

/** Zod schema for IdentityAuthenticationServiceAccountAuditEntry */
IdentityAuthenticationServiceAccountAuditEntrySchema = z.object({
  id: z.string().uuid().optional(),
  timestamp: z.string().datetime().optional(),
  action: z.string().nullable().optional(),
  performedBy: z.string().nullable().optional(),
  ipAddress: z.string().nullable().optional(),
  details: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationServiceAccountAuditLogOutput */
IdentityAuthenticationServiceAccountAuditLogOutputSchema = z.object({
  serviceAccountId: z.string().uuid().optional(),
  entries: z
    .array(z.lazy(() => IdentityAuthenticationServiceAccountAuditEntrySchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  page: z.number().int().optional(),
  pageSize: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationServiceAccountCreatedOutput */
IdentityAuthenticationServiceAccountCreatedOutputSchema = z.object({
  id: z.string().uuid().optional(),
  clientId: z.string().nullable().optional(),
  clientSecret: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  scopes: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
  warning: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationServiceAccountOutput */
IdentityAuthenticationServiceAccountOutputSchema = z.object({
  id: z.string().uuid().optional(),
  clientId: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  scopes: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
  isLocked: z.boolean().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  createdBy: z.string().nullable().optional(),
  lastAuthenticatedAt: z.string().datetime().nullable().optional(),
  authenticationCount: z.number().int().optional(),
  secretRotationCount: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationSessionOutput */
IdentityAuthenticationSessionOutputSchema = z.object({
  id: z.string().uuid().optional(),
  deviceInfo: z.lazy(() => IdentityAuthenticationDeviceInfoSchema).optional(),
  location: z.lazy(() => IdentityAuthenticationLocationInfoSchema).optional(),
  ipAddress: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  lastUsedAt: z.string().datetime().optional(),
  expiresAt: z.string().datetime().optional(),
  isTrustedDevice: z.boolean().optional(),
  isCurrent: z.boolean().optional(),
});

/** Zod schema for IdentityAuthenticationSessionSecurityAnalysis */
IdentityAuthenticationSessionSecurityAnalysisSchema = z.object({
  sessionId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  isSuspicious: z.boolean().optional(),
  unusualActivityDetected: z.boolean().optional(),
  riskScore: z.number().int().optional(),
  activeSessionCount: z.number().int().optional(),
  totalDeviceCount: z.number().int().optional(),
  riskLevel: z.lazy(() => IdentityAuthenticationRiskLevelSchema).optional(),
  securityFlags: z.array(z.string()).nullable().optional(),
  riskFactors: z.array(z.string()).nullable().optional(),
  metadata: z.record(z.string(), z.string()).nullable().optional(),
  analyzedAt: z.string().datetime().optional(),
});

/** Zod schema for IdentityAuthenticationSessionSuccessOutput */
IdentityAuthenticationSessionSuccessOutputSchema = z.object({
  message: z.string().nullable(),
});

/** Zod schema for IdentityAuthenticationSessionTerminationOutput */
IdentityAuthenticationSessionTerminationOutputSchema = z.object({
  message: z.string().nullable(),
  terminatedCount: z.number().int(),
});

/** Zod schema for IdentityAuthenticationSignInOutput */
IdentityAuthenticationSignInOutputSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  accessToken: z.string().nullable().optional(),
  refreshToken: z.string().nullable().optional(),
  expiresAt: z.string().datetime().optional(),
  accessTokenExpiresAt: z.string().datetime().optional(),
  refreshTokenExpiresAt: z.string().datetime().optional(),
  expiresIn: z.number().int().optional(),
  userId: z.string().uuid().optional(),
  email: z.string().nullable().optional(),
  sessionId: z.string().uuid().optional(),
  tempToken: z.string().nullable().optional(),
  mfaToken: z.string().nullable().optional(),
  user: z.lazy(() => IdentityAuthenticationUserSchema).optional(),
  tenantId: z.string().uuid().nullable().optional(),
  availableTenants: z
    .array(z.lazy(() => TenantInfoSchema))
    .nullable()
    .optional(),
  requiresMfa: z.boolean().optional(),
  mfaSessionId: z.string().nullable().optional(),
  requiresStepUp: z.boolean().optional(),
  stepUpToken: z.string().nullable().optional(),
  stepUpExpiresAt: z.string().datetime().nullable().optional(),
  riskLevel: z.lazy(() => IdentityAuthenticationRiskLevelSchema).optional(),
  riskFactors: z.array(z.string()).nullable().optional(),
  availableMethods: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityAuthenticationSmsMfaSetupInput */
IdentityAuthenticationSmsMfaSetupInputSchema = z.object({
  phoneNumber: z.string().nullable(),
});

/** Zod schema for IdentityAuthenticationSmsMfaSetupOutput */
IdentityAuthenticationSmsMfaSetupOutputSchema = z.object({
  message: z.string().nullable(),
  phoneNumberMasked: z.string().nullable(),
  expiresInSeconds: z.number().int(),
});

/** Zod schema for IdentityAuthenticationTrustDeviceInput */
IdentityAuthenticationTrustDeviceInputSchema = z.object({
  deviceName: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationTrustedDeviceOutput */
IdentityAuthenticationTrustedDeviceOutputSchema = z.object({
  id: z.string().uuid().optional(),
  deviceName: z.string().nullable().optional(),
  deviceInfo: z.lazy(() => IdentityAuthenticationDeviceInfoSchema).optional(),
  trustedAt: z.string().datetime().optional(),
  lastUsedAt: z.string().datetime().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationUpdateCredentialNameInput */
IdentityAuthenticationUpdateCredentialNameInputSchema = z.object({
  friendlyName: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationUpdateScopesInput */
IdentityAuthenticationUpdateScopesInputSchema = z.object({
  scopes: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationUser */
IdentityAuthenticationUserSchema = z.object({
  id: z.string().uuid().optional(),
  email: z.string().nullable().optional(),
  username: z.string().nullable().optional(),
  firstName: z.string().nullable().optional(),
  lastName: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
  emailVerified: z.boolean().optional(),
  phoneNumberVerified: z.boolean().optional(),
  createdAt: z.string().datetime().optional(),
  lastLoginAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationVerifyEmailInput */
IdentityAuthenticationVerifyEmailInputSchema = z.object({
  token: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationVerifyMfaInput */
IdentityAuthenticationVerifyMfaInputSchema = z.object({
  userId: z.string().uuid().optional(),
  code: z.string().min(1),
  method: z.lazy(() => IdentityAuthenticationMfaMethodSchema).optional(),
});

/** Zod schema for IdentityAuthenticationWeb3ChallengeInput */
IdentityAuthenticationWeb3ChallengeInputSchema = z.object({
  walletAddress: z.string().min(1),
  chainId: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationWeb3ChallengeOutput */
IdentityAuthenticationWeb3ChallengeOutputSchema = z.object({
  challenge: z.string().nullable().optional(),
  nonce: z.string().nullable().optional(),
  expiresAt: z.string().datetime().optional(),
});

/** Zod schema for IdentityAuthenticationWeb3VerifyInput */
IdentityAuthenticationWeb3VerifyInputSchema = z.object({
  walletAddress: z.string().min(1),
  signature: z.string().min(1),
  nonce: z.string().min(1),
  chainId: z.string().min(1),
  tenantId: z.string().uuid().nullable().optional(),
  deviceFingerprint: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnAuthenticationOptionsResult */
IdentityAuthenticationWebAuthnAuthenticationOptionsResultSchema = z.object({
  success: z.boolean().optional(),
  error: z.string().nullable().optional(),
  sessionId: z.string().nullable().optional(),
  optionsJson: z.string().nullable().optional(),
  options: z.lazy(() => Fido2NetLibAssertionOptionsSchema).optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnAuthenticationResult */
IdentityAuthenticationWebAuthnAuthenticationResultSchema = z.object({
  success: z.boolean().optional(),
  error: z.string().nullable().optional(),
  userId: z.string().uuid().nullable().optional(),
  credentialId: z.string().uuid().nullable().optional(),
  isPasswordless: z.boolean().optional(),
  email: z.string().nullable().optional(),
  accessToken: z.string().nullable().optional(),
  refreshToken: z.string().nullable().optional(),
  accessTokenExpiresAt: z.string().datetime().nullable().optional(),
  refreshTokenExpiresAt: z.string().datetime().nullable().optional(),
  expiresIn: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnAuthenticatorType */
IdentityAuthenticationWebAuthnAuthenticatorTypeSchema = z.enum(['Platform', 'CrossPlatform']);

/** Zod schema for IdentityAuthenticationWebAuthnCredentialInfo */
IdentityAuthenticationWebAuthnCredentialInfoSchema = z.object({
  id: z.string().uuid().optional(),
  friendlyName: z.string().nullable().optional(),
  authenticatorType: z.lazy(() => IdentityAuthenticationWebAuthnAuthenticatorTypeSchema).optional(),
  createdAt: z.string().datetime().optional(),
  lastUsedAt: z.string().datetime().nullable().optional(),
  isPasswordless: z.boolean().optional(),
  isDefault: z.boolean().optional(),
  backedUp: z.boolean().optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnCredentialVerifyResult */
IdentityAuthenticationWebAuthnCredentialVerifyResultSchema = z.object({
  success: z.boolean().optional(),
  error: z.string().nullable().optional(),
  isValid: z.boolean().optional(),
  isExpired: z.boolean().optional(),
  isRevoked: z.boolean().optional(),
  lastUsedAt: z.string().datetime().nullable().optional(),
  signatureCount: z.number().int().optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnRegistrationOptionsResult */
IdentityAuthenticationWebAuthnRegistrationOptionsResultSchema = z.object({
  success: z.boolean().optional(),
  error: z.string().nullable().optional(),
  sessionId: z.string().nullable().optional(),
  optionsJson: z.string().nullable().optional(),
  options: z.lazy(() => Fido2NetLibCredentialCreateOptionsSchema).optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnRegistrationResult */
IdentityAuthenticationWebAuthnRegistrationResultSchema = z.object({
  success: z.boolean().optional(),
  error: z.string().nullable().optional(),
  credentialId: z.string().uuid().nullable().optional(),
  friendlyName: z.string().nullable().optional(),
});

/** Zod schema for IdentityAuthenticationWebAuthnStatusOutput */
IdentityAuthenticationWebAuthnStatusOutputSchema = z.object({
  isEnabled: z.boolean().optional(),
  credentialCount: z.number().int().optional(),
  hasPasswordlessCredential: z.boolean().optional(),
  hasPlatformAuthenticator: z.boolean().optional(),
  hasSecurityKey: z.boolean().optional(),
});

/** Zod schema for IdentityTenantsAddTenantMemberOutput */
IdentityTenantsAddTenantMemberOutputSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  memberId: z.string().uuid().nullable().optional(),
});

/** Zod schema for IdentityTenantsAddUserMembershipInput */
IdentityTenantsAddUserMembershipInputSchema = z.object({
  tenantId: z.string().uuid().optional(),
  role: z.string().nullable().optional(),
  invitedByEmail: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsArchiveInput */
IdentityTenantsArchiveInputSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkActivateTenantsCommand */
IdentityTenantsBulkActivateTenantsCommandSchema = z.object({
  tenantIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkArchiveTenantsCommand */
IdentityTenantsBulkArchiveTenantsCommandSchema = z.object({
  tenantIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkCreateTenantItem */
IdentityTenantsBulkCreateTenantItemSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  adminEmail: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkCreateTenantsCommand */
IdentityTenantsBulkCreateTenantsCommandSchema = z.object({
  tenants: z
    .array(z.lazy(() => IdentityTenantsBulkCreateTenantItemSchema))
    .nullable()
    .optional(),
});

/** Zod schema for IdentityTenantsBulkDeactivateTenantsCommand */
IdentityTenantsBulkDeactivateTenantsCommandSchema = z.object({
  tenantIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkDeleteTenantsCommand */
IdentityTenantsBulkDeleteTenantsCommandSchema = z.object({
  tenantIds: z.array(z.string().uuid()).nullable().optional(),
  hardDelete: z.boolean().optional(),
});

/** Zod schema for IdentityTenantsBulkPurgeTenantsCommand */
IdentityTenantsBulkPurgeTenantsCommandSchema = z.object({
  tenantIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkUndeleteTenantsCommand */
IdentityTenantsBulkUndeleteTenantsCommandSchema = z.object({
  tenantIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkUpdateTenantItem */
IdentityTenantsBulkUpdateTenantItemSchema = z.object({
  tenantId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsBulkUpdateTenantsCommand */
IdentityTenantsBulkUpdateTenantsCommandSchema = z.object({
  updates: z
    .array(z.lazy(() => IdentityTenantsBulkUpdateTenantItemSchema))
    .nullable()
    .optional(),
});

/** Zod schema for IdentityTenantsCreateTenantInput */
IdentityTenantsCreateTenantInputSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  adminEmail: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsGetUserMembershipsOutput */
IdentityTenantsGetUserMembershipsOutputSchema = z.object({
  memberships: z
    .array(z.lazy(() => IdentityTenantsUserMembershipSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
});

/** Zod schema for IdentityTenantsMembershipCountOutput */
IdentityTenantsMembershipCountOutputSchema = z.object({
  count: z.number().int().optional(),
});

/** Zod schema for IdentityTenantsRecoverInput */
IdentityTenantsRecoverInputSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsReplaceTenantMetadataInput */
IdentityTenantsReplaceTenantMetadataInputSchema = z.object({
  customFields: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  tags: z.array(z.string()).nullable().optional(),
  externalReferences: z.record(z.string(), z.string()).nullable().optional(),
  businessInfo: z.lazy(() => IdentityTenantsUpdateTenantBusinessInfoInputSchema).optional(),
  contactInfo: z.lazy(() => IdentityTenantsUpdateTenantContactInfoInputSchema).optional(),
  adminNotes: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsReplaceTenantSettingsInput */
IdentityTenantsReplaceTenantSettingsInputSchema = z.object({
  systemConfiguration: z.lazy(() => IdentityTenantsUpdateTenantSystemConfigurationInputSchema).optional(),
  featureFlags: z.record(z.string(), z.boolean()).nullable().optional(),
  businessRules: z.lazy(() => IdentityTenantsUpdateTenantBusinessRulesInputSchema).optional(),
  userInterfaceSettings: z.lazy(() => IdentityTenantsUpdateTenantUiSettingsInputSchema).optional(),
  securitySettings: z.lazy(() => IdentityTenantsUpdateTenantSecuritySettingsInputSchema).optional(),
  integrationSettings: z.lazy(() => IdentityTenantsUpdateTenantIntegrationSettingsInputSchema).optional(),
  systemLimits: z.lazy(() => IdentityTenantsUpdateTenantSystemLimitsInputSchema).optional(),
});

/** Zod schema for IdentityTenantsSlugValidation */
IdentityTenantsSlugValidationSchema = z.object({
  isAvailable: z.boolean().optional(),
  isValid: z.boolean().optional(),
  suggestedAlternatives: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenant */
IdentityTenantsTenantSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  isDefault: z.boolean().optional(),
  isArchived: z.boolean().optional(),
  archivedAt: z.string().datetime().nullable().optional(),
  tenantMembers: z
    .array(z.lazy(() => IdentityTenantsTenantMemberSchema))
    .nullable()
    .optional(),
  tenantDomains: z
    .array(z.lazy(() => IdentityTenantsTenantDomainSchema))
    .nullable()
    .optional(),
  tenantSettings: z.lazy(() => IdentityTenantsTenantSettingsSchema).optional(),
  tenantStatistics: z.lazy(() => IdentityTenantsTenantStatisticsSchema).optional(),
  usageTrackingRecords: z
    .array(z.lazy(() => IdentityTenantsUsageTrackingSchema))
    .nullable()
    .optional(),
  name: z.string().min(1).max(100),
  description: z.string().max(500).nullable().optional(),
  isActive: z.boolean().optional(),
  slug: z.string().min(1).max(255),
  adminEmail: z.string().max(255).nullable().optional(),
  canAcceptMembers: z.boolean().optional(),
  activeMemberCount: z.number().int().optional(),
  hasActiveMembers: z.boolean().optional(),
});

/** Zod schema for IdentityTenantsTenantAddress */
IdentityTenantsTenantAddressSchema = z.object({
  street: z.string().nullable().optional(),
  city: z.string().nullable().optional(),
  state: z.string().nullable().optional(),
  postalCode: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantAuditLogEntry */
IdentityTenantsTenantAuditLogEntrySchema = z.object({
  id: z.string().uuid().optional(),
  tenantId: z.string().uuid().optional(),
  timestamp: z.string().datetime().optional(),
  action: z.string().nullable().optional(),
  actorId: z.string().uuid().nullable().optional(),
  actorName: z.string().nullable().optional(),
  actorEmail: z.string().nullable().optional(),
  beforeValues: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  afterValues: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  ipAddress: z.string().nullable().optional(),
  userAgent: z.string().nullable().optional(),
  correlationId: z.string().nullable().optional(),
  metadata: z.record(z.string(), z.string()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantBranding */
IdentityTenantsTenantBrandingSchema = z.object({
  logoUrl: z.string().nullable().optional(),
  faviconUrl: z.string().nullable().optional(),
  primaryColor: z.string().nullable().optional(),
  secondaryColor: z.string().nullable().optional(),
  companyName: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantBusinessInfo */
IdentityTenantsTenantBusinessInfoSchema = z.object({
  industry: z.string().nullable().optional(),
  organizationSize: z.string().nullable().optional(),
  tenantType: z.string().nullable().optional(),
  geographicRegion: z.string().nullable().optional(),
  complianceRequirements: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantBusinessRules */
IdentityTenantsTenantBusinessRulesSchema = z.object({
  workflowRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  validationRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  approvalRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  notificationRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantContactInfo */
IdentityTenantsTenantContactInfoSchema = z.object({
  primaryContactName: z.string().nullable().optional(),
  primaryContactEmail: z.string().nullable().optional(),
  primaryContactPhone: z.string().nullable().optional(),
  organizationName: z.string().nullable().optional(),
  address: z.lazy(() => IdentityTenantsTenantAddressSchema).optional(),
  website: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantCurrencySettings */
IdentityTenantsTenantCurrencySettingsSchema = z.object({
  defaultCurrency: z.string().nullable().optional(),
  displayFormat: z.string().nullable().optional(),
  decimalPlaces: z.number().int().optional(),
});

/** Zod schema for IdentityTenantsTenantDomain */
IdentityTenantsTenantDomainSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid(),
  topLevelDomain: z.string().min(1).max(255),
  subdomain: z.string().max(100).nullable().optional(),
  isMainDomain: z.boolean().optional(),
  isSecondaryDomain: z.boolean().optional(),
  userGroupId: z.string().uuid().nullable().optional(),
  fullDomain: z.string().nullable().optional(),
  tenant: z.lazy(() => IdentityTenantsTenantSchema).optional(),
});

/** Zod schema for IdentityTenantsTenantIntegrationSettings */
IdentityTenantsTenantIntegrationSettingsSchema = z.object({
  externalServices: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  webhookSettings: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  apiKeys: z.record(z.string(), z.string()).nullable().optional(),
  ssoConfiguration: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantMember */
IdentityTenantsTenantMemberSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid(),
  userId: z.string().uuid(),
  parentMemberId: z.string().uuid().nullable().optional(),
  parentMember: z.lazy(() => IdentityTenantsTenantMemberSchema).optional(),
  childMembers: z
    .array(z.lazy(() => IdentityTenantsTenantMemberSchema))
    .nullable()
    .optional(),
  role: z.string().min(1).max(100),
  isActive: z.boolean().optional(),
  joinedAt: z.string().datetime().optional(),
  leftAt: z.string().datetime().nullable().optional(),
  leaveReason: z.string().max(500).nullable().optional(),
  metadata: z.string().max(4000).nullable().optional(),
  tenant: z.lazy(() => IdentityTenantsTenantSchema).optional(),
});

/** Zod schema for IdentityTenantsTenantMetadata */
IdentityTenantsTenantMetadataSchema = z.object({
  id: z.string().uuid().optional(),
  customFields: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  tags: z.array(z.string()).nullable().optional(),
  externalReferences: z.record(z.string(), z.string()).nullable().optional(),
  businessInfo: z.lazy(() => IdentityTenantsTenantBusinessInfoSchema).optional(),
  contactInfo: z.lazy(() => IdentityTenantsTenantContactInfoSchema).optional(),
  adminNotes: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for IdentityTenantsTenantSecuritySettings */
IdentityTenantsTenantSecuritySettingsSchema = z.object({
  passwordPolicy: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  sessionTimeout: z.number().int().optional(),
  twoFactorRequired: z.boolean().optional(),
  ipWhitelist: z.array(z.string()).nullable().optional(),
  apiRateLimits: z.record(z.string(), z.number().int()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantSettings */
IdentityTenantsTenantSettingsSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid(),
  defaultLanguage: z.string().max(10).nullable().optional(),
  defaultTimezone: z.string().max(50).nullable().optional(),
  defaultCurrency: z.string().max(3).nullable().optional(),
  allowUserRegistration: z.boolean().optional(),
  requireRegistrationApproval: z.boolean().optional(),
  requireTwoFactorAuth: z.boolean().optional(),
  maxUsers: z.number().int().nullable().optional(),
  storageQuota: z.number().int().nullable().optional(),
  enableAuditLogging: z.boolean().optional(),
  enableApiAccess: z.boolean().optional(),
  brandingSettings: z.string().max(5000).nullable().optional(),
  notificationSettings: z.string().max(5000).nullable().optional(),
  securitySettings: z.string().max(5000).nullable().optional(),
  integrationSettingsJson: z.string().nullable().optional(),
  tenant: z.lazy(() => IdentityTenantsTenantSchema).optional(),
});

/** Zod schema for IdentityTenantsTenantStatistics */
IdentityTenantsTenantStatisticsSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid(),
  statisticDate: z.string().datetime().optional(),
  totalMembers: z.number().int().optional(),
  activeMembers: z.number().int().optional(),
  inactiveMembers: z.number().int().optional(),
  storageUsed: z.number().int().optional(),
  apiCalls: z.number().int().optional(),
  newMembers: z.number().int().optional(),
  membersLeft: z.number().int().optional(),
  customMetrics: z.string().max(10000).nullable().optional(),
  tenant: z.lazy(() => IdentityTenantsTenantSchema).optional(),
});

/** Zod schema for IdentityTenantsTenantSystemConfiguration */
IdentityTenantsTenantSystemConfigurationSchema = z.object({
  timeZone: z.string().nullable().optional(),
  locale: z.string().nullable().optional(),
  dateFormat: z.string().nullable().optional(),
  numberFormat: z.string().nullable().optional(),
  currencySettings: z.lazy(() => IdentityTenantsTenantCurrencySettingsSchema).optional(),
  customConfiguration: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantSystemLimits */
IdentityTenantsTenantSystemLimitsSchema = z.object({
  maxUsers: z.number().int().optional(),
  maxStorage: z.number().int().optional(),
  maxApiCalls: z.number().int().optional(),
  maxProjects: z.number().int().optional(),
  customLimits: z.record(z.string(), z.number().int()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantUiSettings */
IdentityTenantsTenantUiSettingsSchema = z.object({
  theme: z.string().nullable().optional(),
  layout: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  branding: z.lazy(() => IdentityTenantsTenantBrandingSchema).optional(),
  customCss: z.string().nullable().optional(),
  componentSettings: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantValidationError */
IdentityTenantsTenantValidationErrorSchema = z.object({
  field: z.string().nullable().optional(),
  code: z.string().nullable().optional(),
  message: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsTenantValidationOutput */
IdentityTenantsTenantValidationOutputSchema = z.object({
  isValid: z.boolean().optional(),
  errors: z
    .array(z.lazy(() => IdentityTenantsTenantValidationErrorSchema))
    .nullable()
    .optional(),
  warnings: z
    .array(z.lazy(() => IdentityTenantsTenantValidationWarningSchema))
    .nullable()
    .optional(),
  suggestions: z.array(z.string()).nullable().optional(),
  slugValidation: z.lazy(() => IdentityTenantsSlugValidationSchema).optional(),
});

/** Zod schema for IdentityTenantsTenantValidationWarning */
IdentityTenantsTenantValidationWarningSchema = z.object({
  field: z.string().nullable().optional(),
  code: z.string().nullable().optional(),
  message: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantAddressInput */
IdentityTenantsUpdateTenantAddressInputSchema = z.object({
  street: z.string().nullable().optional(),
  city: z.string().nullable().optional(),
  state: z.string().nullable().optional(),
  postalCode: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantBrandingInput */
IdentityTenantsUpdateTenantBrandingInputSchema = z.object({
  logoUrl: z.string().nullable().optional(),
  faviconUrl: z.string().nullable().optional(),
  primaryColor: z.string().nullable().optional(),
  secondaryColor: z.string().nullable().optional(),
  companyName: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantBusinessInfoInput */
IdentityTenantsUpdateTenantBusinessInfoInputSchema = z.object({
  industry: z.string().nullable().optional(),
  organizationSize: z.string().nullable().optional(),
  tenantType: z.string().nullable().optional(),
  geographicRegion: z.string().nullable().optional(),
  complianceRequirements: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantBusinessRulesInput */
IdentityTenantsUpdateTenantBusinessRulesInputSchema = z.object({
  workflowRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  validationRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  approvalRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  notificationRules: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantContactInfoInput */
IdentityTenantsUpdateTenantContactInfoInputSchema = z.object({
  primaryContactName: z.string().nullable().optional(),
  primaryContactEmail: z.string().nullable().optional(),
  primaryContactPhone: z.string().nullable().optional(),
  organizationName: z.string().nullable().optional(),
  address: z.lazy(() => IdentityTenantsUpdateTenantAddressInputSchema).optional(),
  website: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantCurrencySettingsInput */
IdentityTenantsUpdateTenantCurrencySettingsInputSchema = z.object({
  defaultCurrency: z.string().nullable().optional(),
  displayFormat: z.string().nullable().optional(),
  decimalPlaces: z.number().int().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantIntegrationSettingsInput */
IdentityTenantsUpdateTenantIntegrationSettingsInputSchema = z.object({
  externalServices: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  webhookSettings: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  apiKeys: z.record(z.string(), z.string()).nullable().optional(),
  ssoConfiguration: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantMemberRoleOutput */
IdentityTenantsUpdateTenantMemberRoleOutputSchema = z.object({
  success: z.boolean().optional(),
  message: z.string().nullable().optional(),
  memberId: z.string().uuid().optional(),
  newRole: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantMetadataInput */
IdentityTenantsUpdateTenantMetadataInputSchema = z.object({
  customFields: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  tags: z.array(z.string()).nullable().optional(),
  externalReferences: z.record(z.string(), z.string()).nullable().optional(),
  businessInfo: z.lazy(() => IdentityTenantsUpdateTenantBusinessInfoInputSchema).optional(),
  contactInfo: z.lazy(() => IdentityTenantsUpdateTenantContactInfoInputSchema).optional(),
  adminNotes: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantInput */
IdentityTenantsUpdateTenantInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantSecuritySettingsInput */
IdentityTenantsUpdateTenantSecuritySettingsInputSchema = z.object({
  passwordPolicy: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  sessionTimeout: z.number().int().nullable().optional(),
  twoFactorRequired: z.boolean().nullable().optional(),
  ipWhitelist: z.array(z.string()).nullable().optional(),
  apiRateLimits: z.record(z.string(), z.number().int()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantSettingsInput */
IdentityTenantsUpdateTenantSettingsInputSchema = z.object({
  systemConfiguration: z.lazy(() => IdentityTenantsUpdateTenantSystemConfigurationInputSchema).optional(),
  featureFlags: z.record(z.string(), z.boolean()).nullable().optional(),
  businessRules: z.lazy(() => IdentityTenantsUpdateTenantBusinessRulesInputSchema).optional(),
  userInterfaceSettings: z.lazy(() => IdentityTenantsUpdateTenantUiSettingsInputSchema).optional(),
  securitySettings: z.lazy(() => IdentityTenantsUpdateTenantSecuritySettingsInputSchema).optional(),
  integrationSettings: z.lazy(() => IdentityTenantsUpdateTenantIntegrationSettingsInputSchema).optional(),
  systemLimits: z.lazy(() => IdentityTenantsUpdateTenantSystemLimitsInputSchema).optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantSystemConfigurationInput */
IdentityTenantsUpdateTenantSystemConfigurationInputSchema = z.object({
  timeZone: z.string().nullable().optional(),
  locale: z.string().nullable().optional(),
  dateFormat: z.string().nullable().optional(),
  numberFormat: z.string().nullable().optional(),
  currencySettings: z.lazy(() => IdentityTenantsUpdateTenantCurrencySettingsInputSchema).optional(),
  customConfiguration: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantSystemLimitsInput */
IdentityTenantsUpdateTenantSystemLimitsInputSchema = z.object({
  maxUsers: z.number().int().nullable().optional(),
  maxStorage: z.number().int().nullable().optional(),
  maxApiCalls: z.number().int().nullable().optional(),
  maxProjects: z.number().int().nullable().optional(),
  customLimits: z.record(z.string(), z.number().int()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantTagsInput */
IdentityTenantsUpdateTenantTagsInputSchema = z.object({
  tags: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateTenantUiSettingsInput */
IdentityTenantsUpdateTenantUiSettingsInputSchema = z.object({
  theme: z.string().nullable().optional(),
  layout: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
  branding: z.lazy(() => IdentityTenantsUpdateTenantBrandingInputSchema).optional(),
  customCss: z.string().nullable().optional(),
  componentSettings: z.record(z.string(), z.record(z.string(), z.unknown()).nullable()).nullable().optional(),
});

/** Zod schema for IdentityTenantsUpdateUserMembershipRoleInput */
IdentityTenantsUpdateUserMembershipRoleInputSchema = z.object({
  role: z.string().nullable().optional(),
});

/** Zod schema for IdentityTenantsUsageTracking */
IdentityTenantsUsageTrackingSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid(),
  date: z.string().datetime().optional(),
  resourceType: z.string().min(1).max(100),
  usageAmount: z.number().int().optional(),
  unit: z.string().max(50).nullable().optional(),
  cost: z.number().optional(),
  metadata: z.string().max(4000).nullable().optional(),
  tenant: z.lazy(() => IdentityTenantsTenantSchema).optional(),
});

/** Zod schema for IdentityTenantsUserMembership */
IdentityTenantsUserMembershipSchema = z.object({
  membershipId: z.string().uuid().optional(),
  tenantId: z.string().uuid().optional(),
  tenantName: z.string().nullable().optional(),
  tenantSlug: z.string().nullable().optional(),
  tenantIsActive: z.boolean().optional(),
  tenantDescription: z.string().nullable().optional(),
  role: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
  joinedAt: z.string().datetime().optional(),
  leftAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityTenantsValidateTenantInput */
IdentityTenantsValidateTenantInputSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  adminEmail: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersBulkActivateUsersInput */
IdentityUsersBulkActivateUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkActivateUsersOutput */
IdentityUsersBulkActivateUsersOutputSchema = z.object({
  activatedUsers: z
    .array(z.lazy(() => IdentityUsersUserSchema))
    .nullable()
    .optional(),
  failedUserIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkCreateUsersInput */
IdentityUsersBulkCreateUsersInputSchema = z.object({
  users: z
    .array(z.lazy(() => IdentityUsersCreateUserRequestItemSchema))
    .nullable()
    .optional(),
});

/** Zod schema for IdentityUsersBulkCreateUsersOutput */
IdentityUsersBulkCreateUsersOutputSchema = z.object({
  createdUserIds: z.array(z.string().uuid()).nullable().optional(),
  failedEmails: z.array(z.string()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkDeactivateUsersInput */
IdentityUsersBulkDeactivateUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkDeactivateUsersOutput */
IdentityUsersBulkDeactivateUsersOutputSchema = z.object({
  deactivatedUsers: z
    .array(z.lazy(() => IdentityUsersUserSchema))
    .nullable()
    .optional(),
  failedUserIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkDeleteUsersInput */
IdentityUsersBulkDeleteUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkNotificationInput */
IdentityUsersBulkNotificationInputSchema = z.object({
  notificationIds: z.array(z.string().uuid()).nullable().optional(),
  operation: z.string().nullable().optional(),
  filterCriteria: z.lazy(() => IdentityUsersNotificationFilterCriteriaSchema).optional(),
});

/** Zod schema for IdentityUsersBulkPurgeUsersInput */
IdentityUsersBulkPurgeUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
  strategy: z.lazy(() => IdentityUsersPurgeStrategySchema).optional(),
});

/** Zod schema for IdentityUsersBulkRestoreUsersInput */
IdentityUsersBulkRestoreUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkRestoreUsersOutput */
IdentityUsersBulkRestoreUsersOutputSchema = z.object({
  restoredUsers: z
    .array(z.lazy(() => IdentityUsersUserSchema))
    .nullable()
    .optional(),
  failedUserIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkSuspendUsersInput */
IdentityUsersBulkSuspendUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkSuspendUsersOutput */
IdentityUsersBulkSuspendUsersOutputSchema = z.object({
  suspendedUsers: z
    .array(z.lazy(() => IdentityUsersUserSchema))
    .nullable()
    .optional(),
  failedUserIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkUnsuspendUsersInput */
IdentityUsersBulkUnsuspendUsersInputSchema = z.object({
  userIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkUnsuspendUsersOutput */
IdentityUsersBulkUnsuspendUsersOutputSchema = z.object({
  unsuspendedUsers: z
    .array(z.lazy(() => IdentityUsersUserSchema))
    .nullable()
    .optional(),
  failedUserIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for IdentityUsersBulkUpdateUsersInput */
IdentityUsersBulkUpdateUsersInputSchema = z.object({
  updates: z
    .array(z.lazy(() => IdentityUsersUpdateUserRequestItemSchema))
    .nullable()
    .optional(),
});

/** Zod schema for IdentityUsersCreateUserInput */
IdentityUsersCreateUserInputSchema = z.object({
  email: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersCreateUserRequestItem */
IdentityUsersCreateUserRequestItemSchema = z.object({
  email: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersNotificationAction */
IdentityUsersNotificationActionSchema = z.object({
  id: z.string().nullable().optional(),
  text: z.string().nullable().optional(),
  url: z.string().nullable().optional(),
  type: z.string().nullable().optional(),
  isPrimary: z.boolean().optional(),
});

/** Zod schema for IdentityUsersNotificationFilterCriteria */
IdentityUsersNotificationFilterCriteriaSchema = z.object({
  categories: z.array(z.string()).nullable().optional(),
  priorities: z.array(z.string()).nullable().optional(),
  types: z.array(z.string()).nullable().optional(),
  isRead: z.boolean().nullable().optional(),
  isArchived: z.boolean().nullable().optional(),
  dateFrom: z.string().datetime().nullable().optional(),
  dateTo: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityUsersPurgeStrategy */
IdentityUsersPurgeStrategySchema = z.enum(['Immediate', 'Scheduled', 'GracePeriod']);

/** Zod schema for IdentityUsersReplaceUserAccessibilityPreferencesInput */
IdentityUsersReplaceUserAccessibilityPreferencesInputSchema = z.object({
  accessibilityPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersReplaceUserLocalizationPreferencesInput */
IdentityUsersReplaceUserLocalizationPreferencesInputSchema = z.object({
  localizationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersReplaceUserMetadataInput */
IdentityUsersReplaceUserMetadataInputSchema = z.object({
  customFields: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  tags: z.array(z.string()).nullable().optional(),
  externalReferences: z.record(z.string(), z.string()).nullable().optional(),
});

/** Zod schema for IdentityUsersReplaceUserNotificationPreferencesInput */
IdentityUsersReplaceUserNotificationPreferencesInputSchema = z.object({
  notificationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersReplaceUserPreferencesInput */
IdentityUsersReplaceUserPreferencesInputSchema = z.object({
  generalPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  notificationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  accessibilityPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  privacyPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersReplaceUserPrivacyPreferencesInput */
IdentityUsersReplaceUserPrivacyPreferencesInputSchema = z.object({
  privacyPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersReplaceUserProfileInput */
IdentityUsersReplaceUserProfileInputSchema = z.object({
  displayName: z.string().nullable().optional(),
  bio: z.string().nullable().optional(),
  location: z.string().nullable().optional(),
  website: z.string().nullable().optional(),
  jobTitle: z.string().nullable().optional(),
  company: z.string().nullable().optional(),
  timeZone: z.string().nullable().optional(),
  language: z.string().nullable().optional(),
  profileVisibility: z.string().nullable().optional(),
  showEmail: z.boolean().optional(),
  showLocation: z.boolean().optional(),
});

/** Zod schema for IdentityUsersUpdateUserAccessibilityPreferencesInput */
IdentityUsersUpdateUserAccessibilityPreferencesInputSchema = z.object({
  accessibilityPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserLocalizationPreferencesInput */
IdentityUsersUpdateUserLocalizationPreferencesInputSchema = z.object({
  localizationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserMetadataInput */
IdentityUsersUpdateUserMetadataInputSchema = z.object({
  customFields: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  tagsToAdd: z.array(z.string()).nullable().optional(),
  tagsToRemove: z.array(z.string()).nullable().optional(),
  externalReferences: z.record(z.string(), z.string()).nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserNotificationPreferencesInput */
IdentityUsersUpdateUserNotificationPreferencesInputSchema = z.object({
  notificationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserPreferencesInput */
IdentityUsersUpdateUserPreferencesInputSchema = z.object({
  generalPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  notificationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  accessibilityPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  privacyPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserPrivacyPreferencesInput */
IdentityUsersUpdateUserPrivacyPreferencesInputSchema = z.object({
  privacyPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserProfileInput */
IdentityUsersUpdateUserProfileInputSchema = z.object({
  displayName: z.string().nullable().optional(),
  bio: z.string().nullable().optional(),
  location: z.string().nullable().optional(),
  website: z.string().nullable().optional(),
  jobTitle: z.string().nullable().optional(),
  company: z.string().nullable().optional(),
  timeZone: z.string().nullable().optional(),
  language: z.string().nullable().optional(),
  profileVisibility: z.string().nullable().optional(),
  showEmail: z.boolean().nullable().optional(),
  showLocation: z.boolean().nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserInput */
IdentityUsersUpdateUserInputSchema = z.object({
  name: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersUpdateUserRequestItem */
IdentityUsersUpdateUserRequestItemSchema = z.object({
  userId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersUserAccessibilityPreferences */
IdentityUsersUserAccessibilityPreferencesSchema = z.object({
  highContrast: z.boolean().optional(),
  largeText: z.boolean().optional(),
  screenReader: z.boolean().optional(),
  reducedMotion: z.boolean().optional(),
  keyboardNavigation: z.boolean().optional(),
  fontSize: z.number().int().optional(),
  colorScheme: z.string().nullable().optional(),
  customSettings: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUser */
IdentityUsersUserSchema = z.object({
  id: z.string().uuid().optional(),
  email: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  isActive: z.boolean().optional(),
  phoneNumber: z.string().nullable().optional(),
  lastSeenAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for IdentityUsersUserLocalizationPreferences */
IdentityUsersUserLocalizationPreferencesSchema = z.object({
  language: z.string().nullable().optional(),
  timezone: z.string().nullable().optional(),
  dateFormat: z.string().nullable().optional(),
  timeFormat: z.string().nullable().optional(),
  currency: z.string().nullable().optional(),
  numberFormat: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  customSettings: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUserMetadata */
IdentityUsersUserMetadataSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  customFields: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  tags: z.array(z.string()).nullable().optional(),
  externalReferences: z.record(z.string(), z.string()).nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  version: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersUserNotificationDetail */
IdentityUsersUserNotificationDetailSchema = z.object({
  notification: z.lazy(() => IdentityUsersUserNotificationSchema).optional(),
  relatedNotifications: z
    .array(z.lazy(() => IdentityUsersUserNotificationSchema))
    .nullable()
    .optional(),
  actions: z
    .array(z.lazy(() => IdentityUsersNotificationActionSchema))
    .nullable()
    .optional(),
});

/** Zod schema for IdentityUsersUserNotification */
IdentityUsersUserNotificationSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  type: z.string().nullable().optional(),
  title: z.string().nullable().optional(),
  message: z.string().nullable().optional(),
  priority: z.string().nullable().optional(),
  category: z.string().nullable().optional(),
  isRead: z.boolean().optional(),
  isArchived: z.boolean().optional(),
  readAt: z.string().datetime().nullable().optional(),
  archivedAt: z.string().datetime().nullable().optional(),
  expiresAt: z.string().datetime().nullable().optional(),
  actionUrl: z.string().nullable().optional(),
  actionText: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  metadata: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  version: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersUserNotificationPreferences */
IdentityUsersUserNotificationPreferencesSchema = z.object({
  emailEnabled: z.boolean().optional(),
  pushEnabled: z.boolean().optional(),
  smsEnabled: z.boolean().optional(),
  inAppEnabled: z.boolean().optional(),
  frequency: z.string().nullable().optional(),
  quietHours: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  categoryPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUserPreferences */
IdentityUsersUserPreferencesSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  generalPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  notificationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  accessibilityPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  privacyPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  localizationPreferences: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  version: z.string().nullable().optional(),
});

/** Zod schema for IdentityUsersUserPrivacyPreferences */
IdentityUsersUserPrivacyPreferencesSchema = z.object({
  profileVisibility: z.string().nullable().optional(),
  activityTracking: z.boolean().optional(),
  dataCollection: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  thirdPartySharing: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
  marketingEmails: z.boolean().optional(),
  analyticsCookies: z.boolean().optional(),
  personalizedContent: z.boolean().optional(),
  customSettings: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for IdentityUsersUserProfile */
IdentityUsersUserProfileSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  displayName: z.string().nullable().optional(),
  bio: z.string().nullable().optional(),
  location: z.string().nullable().optional(),
  website: z.string().nullable().optional(),
  jobTitle: z.string().nullable().optional(),
  company: z.string().nullable().optional(),
  avatarUrl: z.string().nullable().optional(),
  bannerUrl: z.string().nullable().optional(),
  timeZone: z.string().nullable().optional(),
  language: z.string().nullable().optional(),
  profileVisibility: z.string().nullable().optional(),
  showEmail: z.boolean().optional(),
  showLocation: z.boolean().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  version: z.string().nullable().optional(),
});

/** Zod schema for LearningAssessmentsAssessment */
LearningAssessmentsAssessmentSchema = z.object({
  id: z.string().uuid().optional(),
  courseId: z.string().uuid().optional(),
  contentId: z.string().uuid().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => LearningAssessmentsAssessmentTypeSchema).optional(),
  maxScore: z.number().int().optional(),
  passingScore: z.number().int().optional(),
  timeLimitMinutes: z.number().int().nullable().optional(),
  maxAttempts: z.number().int().nullable().optional(),
  isRequired: z.boolean().optional(),
  order: z.number().int().optional(),
  availableFrom: z.string().datetime().nullable().optional(),
  availableUntil: z.string().datetime().nullable().optional(),
  isAvailable: z.boolean().optional(),
});

/** Zod schema for LearningAssessmentsAssessmentSubmission */
LearningAssessmentsAssessmentSubmissionSchema = z.object({
  id: z.string().uuid().optional(),
  assessmentId: z.string().uuid().optional(),
  enrollmentId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  attemptNumber: z.number().int().optional(),
  score: z.number().int().nullable().optional(),
  passed: z.boolean().nullable().optional(),
  startedAt: z.string().datetime().optional(),
  submittedAt: z.string().datetime().nullable().optional(),
  gradedAt: z.string().datetime().nullable().optional(),
  gradedBy: z.string().uuid().nullable().optional(),
  feedback: z.string().nullable().optional(),
  status: z.lazy(() => LearningAssessmentsSubmissionStatusSchema).optional(),
});

/** Zod schema for LearningAssessmentsAssessmentType */
LearningAssessmentsAssessmentTypeSchema = z.enum(['Quiz', 'Exam', 'Assignment', 'Project', 'PeerReview', 'SelfAssessment']);

/** Zod schema for LearningAssessmentsCanAttemptOutput */
LearningAssessmentsCanAttemptOutputSchema = z.object({
  canAttempt: z.boolean().optional(),
  currentAttemptCount: z.number().int().optional(),
});

/** Zod schema for LearningAssessmentsCreateAssessmentInput */
LearningAssessmentsCreateAssessmentInputSchema = z.object({
  courseId: z.string().uuid().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => LearningAssessmentsAssessmentTypeSchema).optional(),
  maxScore: z.number().int().optional(),
  passingScore: z.number().int().optional(),
  timeLimitMinutes: z.number().int().nullable().optional(),
  maxAttempts: z.number().int().nullable().optional(),
  isRequired: z.boolean().optional(),
  availableFrom: z.string().datetime().nullable().optional(),
  availableUntil: z.string().datetime().nullable().optional(),
});

/** Zod schema for LearningAssessmentsGradeSubmissionInput */
LearningAssessmentsGradeSubmissionInputSchema = z.object({
  score: z.number().int().optional(),
  gradedBy: z.string().uuid().nullable().optional(),
  feedback: z.string().nullable().optional(),
});

/** Zod schema for LearningAssessmentsStartSubmissionInput */
LearningAssessmentsStartSubmissionInputSchema = z.object({
  enrollmentId: z.string().uuid().optional(),
});

/** Zod schema for LearningAssessmentsSubmissionStatus */
LearningAssessmentsSubmissionStatusSchema = z.enum(['InProgress', 'Submitted', 'Graded', 'Returned', 'Late']);

/** Zod schema for LearningAssessmentsUpdateAssessmentInput */
LearningAssessmentsUpdateAssessmentInputSchema = z.object({
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  maxScore: z.number().int().nullable().optional(),
  passingScore: z.number().int().nullable().optional(),
  timeLimitMinutes: z.number().int().nullable().optional(),
  maxAttempts: z.number().int().nullable().optional(),
  isRequired: z.boolean().nullable().optional(),
  availableFrom: z.string().datetime().nullable().optional(),
  availableUntil: z.string().datetime().nullable().optional(),
  contentId: z.string().uuid().nullable().optional(),
  clearContentId: z.boolean().optional(),
});

/** Zod schema for LearningCoursesActivityGrade */
LearningCoursesActivityGradeSchema = z.object({
  id: z.string().uuid().optional(),
  contentInteractionId: z.string().uuid().optional(),
  graderProgramUserId: z.string().uuid().nullable().optional(),
  grade: z.number().optional(),
  feedback: z.string().nullable().optional(),
  gradingDetails: z.string().nullable().optional(),
  gradedAt: z.string().datetime().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
  contentInteraction: z.lazy(() => LearningCoursesContentInteractionSummarySchema).optional(),
  grader: z.lazy(() => LearningCoursesGraderSummarySchema).optional(),
  isPassingGrade: z.boolean().optional(),
  gradePercentage: z.string().nullable().optional(),
  hasFeedback: z.boolean().optional(),
  hasGradingDetails: z.boolean().optional(),
});

/** Zod schema for LearningCoursesCircularDependencyCheckResult */
LearningCoursesCircularDependencyCheckResultSchema = z.object({
  wouldCreateCycle: z.boolean().optional(),
});

/** Zod schema for LearningCoursesCloneProgram */
LearningCoursesCloneProgramSchema = z.object({
  newTitle: z.string().nullable().optional(),
  newDescription: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesCompleteContentInput */
LearningCoursesCompleteContentInputSchema = z.object({
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
});

/** Zod schema for LearningCoursesCompletionRates */
LearningCoursesCompletionRatesSchema = z.object({
  programId: z.string().uuid().optional(),
  overallCompletionRate: z.number().optional(),
  contentCompletionRates: z.record(z.string(), z.number()).nullable().optional(),
  completionTrends: z
    .array(z.lazy(() => LearningCoursesCompletionTrendSchema))
    .nullable()
    .optional(),
});

/** Zod schema for LearningCoursesCompletionTrend */
LearningCoursesCompletionTrendSchema = z.object({
  date: z.string().datetime().optional(),
  completedCount: z.number().int().optional(),
  totalCount: z.number().int().optional(),
  rate: z.number().optional(),
});

/** Zod schema for LearningCoursesContentInteraction */
LearningCoursesContentInteractionSchema = z.object({
  id: z.string().uuid().optional(),
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  status: z.lazy(() => LearningCoursesProgressStatusSchema).optional(),
  submissionData: z.string().nullable().optional(),
  completionPercentage: z.number().optional(),
  timeSpentMinutes: z.number().int().nullable().optional(),
  firstAccessedAt: z.string().datetime().nullable().optional(),
  lastAccessedAt: z.string().datetime().nullable().optional(),
  completedAt: z.string().datetime().nullable().optional(),
  submittedAt: z.string().datetime().nullable().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
  content: z.lazy(() => LearningCoursesContentSummarySchema).optional(),
  programUser: z.lazy(() => LearningCoursesProgramUserSummarySchema).optional(),
  isSubmitted: z.boolean().optional(),
  isCompleted: z.boolean().optional(),
  canModify: z.boolean().optional(),
  durationInMinutes: z.number().int().optional(),
});

/** Zod schema for LearningCoursesContentInteractionSummary */
LearningCoursesContentInteractionSummarySchema = z.object({
  id: z.string().uuid().optional(),
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  status: z.string().nullable().optional(),
  submittedAt: z.string().datetime().nullable().optional(),
  content: z.lazy(() => LearningCoursesContentSummarySchema).optional(),
  student: z.lazy(() => LearningCoursesStudentSummarySchema).optional(),
});

/** Zod schema for LearningCoursesContentProgress */
LearningCoursesContentProgressSchema = z.object({
  contentId: z.string().uuid().optional(),
  title: z.string().nullable().optional(),
  status: z.lazy(() => LearningCoursesProgressStatusSchema).optional(),
  completionPercentage: z.number().optional(),
  firstAccessedAt: z.string().datetime().nullable().optional(),
  lastAccessedAt: z.string().datetime().nullable().optional(),
  completedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for LearningCoursesContentStats */
LearningCoursesContentStatsSchema = z.object({
  programId: z.string().uuid().optional(),
  totalContent: z.number().int().optional(),
  requiredContent: z.number().int().optional(),
  optionalContent: z.number().int().optional(),
  contentByType: z
    .object({
      Lesson: z.number().int(),
      Page: z.number().int(),
      Assignment: z.number().int(),
      Questionnaire: z.number().int(),
      Discussion: z.number().int(),
      Code: z.number().int(),
      Challenge: z.number().int(),
      Reflection: z.number().int(),
      Survey: z.number().int(),
    })
    .nullable()
    .optional(),
  contentByVisibility: z
    .object({
      Public: z.number().int(),
      Internal: z.number().int(),
      Private: z.number().int(),
      Restricted: z.number().int(),
    })
    .nullable()
    .optional(),
  topLevelContent: z.number().int().optional(),
  nestedContent: z.number().int().optional(),
});

/** Zod schema for LearningCoursesContentSummary */
LearningCoursesContentSummarySchema = z.object({
  id: z.string().uuid().optional(),
  title: z.string().nullable().optional(),
  contentType: z.string().nullable().optional(),
  estimatedMinutes: z.number().int().nullable().optional(),
});

/** Zod schema for LearningCoursesCreateActivityGrade */
LearningCoursesCreateActivityGradeSchema = z.object({
  contentInteractionId: z.string().uuid().optional(),
  graderProgramUserId: z.string().uuid().optional(),
  grade: z.number().optional(),
  feedback: z.string().nullable().optional(),
  gradingDetails: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesCreatePrerequisiteApiInput */
LearningCoursesCreatePrerequisiteApiInputSchema = z.object({
  courseId: z.string().uuid().optional(),
  prerequisiteCourseId: z.string().uuid().optional(),
  type: z.lazy(() => LearningCoursesPrerequisiteTypeSchema).optional(),
  minimumGrade: z.number().int().nullable().optional(),
  description: z.string().nullable().optional(),
  displayOrder: z.number().int().optional(),
  prerequisiteGroup: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesCreateProductFromProgram */
LearningCoursesCreateProductFromProgramSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  basePrice: z.number().optional(),
  currency: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesCreateProgramContent */
LearningCoursesCreateProgramContentSchema = z.object({
  programId: z.string().uuid(),
  parentId: z.string().uuid().nullable().optional(),
  title: z.string().min(0).max(255),
  description: z.string().nullable().optional(),
  type: z.lazy(() => LearningCoursesProgramContentTypeSchema),
  body: z.string().nullable().optional(),
  sortOrder: z.number().int().optional(),
  isRequired: z.boolean().optional(),
  gradingMethod: z.lazy(() => LearningCoursesGradingMethodSchema).optional(),
  maxPoints: z.number().nullable().optional(),
  estimatedMinutes: z.number().int().nullable().optional(),
  visibility: z.lazy(() => LearningCoursesVisibilitySchema).optional(),
});

/** Zod schema for LearningCoursesCreateProgram */
LearningCoursesCreateProgramSchema = z.object({
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  thumbnail: z.string().nullable().optional(),
  creatorId: z.string().uuid().nullable().optional(),
});

/** Zod schema for LearningCoursesEngagementMetrics */
LearningCoursesEngagementMetricsSchema = z.object({
  programId: z.string().uuid().optional(),
  dailyActiveUsers: z.number().int().optional(),
  weeklyActiveUsers: z.number().int().optional(),
  monthlyActiveUsers: z.number().int().optional(),
  averageSessionDuration: z.string().optional(),
  totalSessions: z.number().int().optional(),
  retentionRate: z.number().optional(),
  contentEngagement: z.record(z.string(), z.number().int()).nullable().optional(),
});

/** Zod schema for LearningCoursesEnrollmentStatus */
LearningCoursesEnrollmentStatusSchema = z.enum(['Open', 'Active', 'Paused', 'Cancelled', 'Expired', 'Completed', 'Closed', 'InviteOnly', 'Waitlist']);

/** Zod schema for LearningCoursesGradeStatistics */
LearningCoursesGradeStatisticsSchema = z.object({
  totalGrades: z.number().int().optional(),
  averageGrade: z.number().optional(),
  minGrade: z.number().optional(),
  maxGrade: z.number().optional(),
  passingRate: z.number().optional(),
  averageGradeFormatted: z.string().nullable().optional(),
  passingRateFormatted: z.string().nullable().optional(),
  hasGrades: z.boolean().optional(),
});

/** Zod schema for LearningCoursesGraderSummary */
LearningCoursesGraderSummarySchema = z.object({
  id: z.string().uuid().optional(),
  userDisplayName: z.string().nullable().optional(),
  userEmail: z.string().nullable().optional(),
  role: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesGradingMethod */
LearningCoursesGradingMethodSchema = z.enum(['None', 'Instructor', 'Peer', 'Ai', 'AutomatedTests']);

/** Zod schema for LearningCoursesMonetization */
LearningCoursesMonetizationSchema = z.object({
  price: z.number().optional(),
  currency: z.string().nullable().optional(),
  isSubscription: z.boolean().optional(),
  subscriptionDurationDays: z.number().int().nullable().optional(),
});

/** Zod schema for LearningCoursesMoveContent */
LearningCoursesMoveContentSchema = z.object({
  contentId: z.string().uuid(),
  newParentId: z.string().uuid().nullable().optional(),
  newSortOrder: z.number().int(),
});

/** Zod schema for LearningCoursesPrerequisiteCheckResult */
LearningCoursesPrerequisiteCheckResultSchema = z.object({
  isSatisfied: z.boolean().optional(),
  prerequisites: z
    .array(z.lazy(() => LearningCoursesPrerequisiteStatusSchema))
    .nullable()
    .optional(),
});

/** Zod schema for LearningCoursesPrerequisite */
LearningCoursesPrerequisiteSchema = z.object({
  id: z.string().uuid().optional(),
  courseId: z.string().uuid().optional(),
  prerequisiteCourseId: z.string().uuid().optional(),
  prerequisiteCourseName: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  type: z.lazy(() => LearningCoursesPrerequisiteTypeSchema).optional(),
  minimumGrade: z.number().int().nullable().optional(),
  description: z.string().nullable().optional(),
  displayOrder: z.number().int().optional(),
  prerequisiteGroup: z.string().nullable().optional(),
  createdAt: z.string().datetime().optional(),
});

/** Zod schema for LearningCoursesPrerequisiteStatus */
LearningCoursesPrerequisiteStatusSchema = z.object({
  prerequisiteId: z.string().uuid().optional(),
  prerequisiteCourseId: z.string().uuid().optional(),
  courseName: z.string().nullable().optional(),
  type: z.lazy(() => LearningCoursesPrerequisiteTypeSchema).optional(),
  isSatisfied: z.boolean().optional(),
  requiredGrade: z.number().int().nullable().optional(),
  achievedGrade: z.number().int().nullable().optional(),
  reason: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesPrerequisiteType */
LearningCoursesPrerequisiteTypeSchema = z.enum(['Required', 'Recommended', 'Corequisite']);

/** Zod schema for LearningCoursesPricing */
LearningCoursesPricingSchema = z.object({
  price: z.number().optional(),
  currency: z.string().nullable().optional(),
  isSubscription: z.boolean().optional(),
  subscriptionDurationDays: z.number().int().nullable().optional(),
  isMonetizationEnabled: z.boolean().optional(),
});

/** Zod schema for LearningCoursesProgramAnalytics */
LearningCoursesProgramAnalyticsSchema = z.object({
  programId: z.string().uuid().optional(),
  title: z.string().nullable().optional(),
  totalUsers: z.number().int().optional(),
  activeUsers: z.number().int().optional(),
  completedUsers: z.number().int().optional(),
  completionRate: z.number().optional(),
  averageCompletionTime: z.string().optional(),
  totalViews: z.number().int().optional(),
  lastActivity: z.string().datetime().nullable().optional(),
  additionalMetrics: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for LearningCoursesProgramContent */
LearningCoursesProgramContentSchema = z.object({
  id: z.string().uuid().optional(),
  programId: z.string().uuid().optional(),
  parentId: z.string().uuid().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => LearningCoursesProgramContentTypeSchema).optional(),
  body: z.record(z.string(), z.unknown()).nullable().optional(),
  sortOrder: z.number().int().optional(),
  isRequired: z.boolean().optional(),
  gradingMethod: z.lazy(() => LearningCoursesGradingMethodSchema).optional(),
  maxPoints: z.number().nullable().optional(),
  estimatedMinutes: z.number().int().nullable().optional(),
  visibility: z.lazy(() => LearningCoursesVisibilitySchema).optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
  programTitle: z.string().nullable().optional(),
  parentTitle: z.string().nullable().optional(),
  childrenCount: z.number().int().optional(),
  children: z
    .array(z.lazy(() => LearningCoursesProgramContentSchema))
    .nullable()
    .optional(),
});

/** Zod schema for LearningCoursesProgramContentType */
LearningCoursesProgramContentTypeSchema = z.enum(['Lesson', 'Page', 'Assignment', 'Questionnaire', 'Discussion', 'Code', 'Challenge', 'Reflection', 'Survey']);

/** Zod schema for LearningCoursesProgramDifficulty */
LearningCoursesProgramDifficultySchema = z.enum(['Beginner', 'Intermediate', 'Advanced', 'Expert']);

/** Zod schema for LearningCoursesProgram */
LearningCoursesProgramSchema = z.object({
  id: z.string().uuid().optional(),
  creatorId: z.string().uuid().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  visibility: z.lazy(() => ContentVisibilitySchema).optional(),
  slug: z.string().nullable().optional(),
  status: z.lazy(() => ContentStatusSchema).optional(),
  thumbnail: z.string().nullable().optional(),
  videoShowcaseUrl: z.string().nullable().optional(),
  estimatedHours: z.number().int().nullable().optional(),
  enrollmentStatus: z.lazy(() => LearningCoursesEnrollmentStatusSchema).optional(),
  maxEnrollments: z.number().int().nullable().optional(),
  enrollmentDeadline: z.string().datetime().nullable().optional(),
  category: z.lazy(() => ProgramCategorySchema).optional(),
  difficulty: z.lazy(() => LearningCoursesProgramDifficultySchema).optional(),
  skillsRequired: z.string().nullable().optional(),
  skillsProvided: z.string().nullable().optional(),
  currentEnrollments: z.number().int().optional(),
  averageRating: z.number().optional(),
  totalRatings: z.number().int().optional(),
  isEnrollmentOpen: z.boolean().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for LearningCoursesProgramUserSummary */
LearningCoursesProgramUserSummarySchema = z.object({
  id: z.string().uuid().optional(),
  userDisplayName: z.string().nullable().optional(),
  userEmail: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesProgressStatus */
LearningCoursesProgressStatusSchema = z.enum(['NotStarted', 'InProgress', 'Completed', 'Submitted']);

/** Zod schema for LearningCoursesRejectProgram */
LearningCoursesRejectProgramSchema = z.object({
  reason: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesReorderContent */
LearningCoursesReorderContentSchema = z.object({
  contentIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for LearningCoursesReorderPrerequisitesInput */
LearningCoursesReorderPrerequisitesInputSchema = z.object({
  prerequisiteIds: z.array(z.string().uuid()).nullable().optional(),
});

/** Zod schema for LearningCoursesRevenueAnalytics */
LearningCoursesRevenueAnalyticsSchema = z.object({
  programId: z.string().uuid().optional(),
  totalRevenue: z.number().optional(),
  monthlyRevenue: z.number().optional(),
  totalPurchases: z.number().int().optional(),
  monthlyPurchases: z.number().int().optional(),
  averageRevenuePerUser: z.number().optional(),
  conversionRate: z.number().optional(),
  revenueChart: z
    .array(z.lazy(() => LearningCoursesRevenueChartSchema))
    .nullable()
    .optional(),
});

/** Zod schema for LearningCoursesRevenueChart */
LearningCoursesRevenueChartSchema = z.object({
  date: z.string().datetime().optional(),
  revenue: z.number().optional(),
  purchases: z.number().int().optional(),
});

/** Zod schema for LearningCoursesScheduleProgram */
LearningCoursesScheduleProgramSchema = z.object({
  publishAt: z.string().datetime().optional(),
});

/** Zod schema for LearningCoursesSearchContent */
LearningCoursesSearchContentSchema = z.object({
  programId: z.string().uuid(),
  searchTerm: z.string().min(0).max(255),
  type: z.lazy(() => LearningCoursesProgramContentTypeSchema).optional(),
  visibility: z.lazy(() => LearningCoursesVisibilitySchema).optional(),
  isRequired: z.boolean().nullable().optional(),
  parentId: z.string().uuid().nullable().optional(),
});

/** Zod schema for LearningCoursesStartContentInput */
LearningCoursesStartContentInputSchema = z.object({
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
});

/** Zod schema for LearningCoursesStudentSummary */
LearningCoursesStudentSummarySchema = z.object({
  id: z.string().uuid().optional(),
  userDisplayName: z.string().nullable().optional(),
  userEmail: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesSubmitContentInput */
LearningCoursesSubmitContentInputSchema = z.object({
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  submissionData: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesSubmitUserContent */
LearningCoursesSubmitUserContentSchema = z.object({
  submissionData: z.string().min(1),
});

/** Zod schema for LearningCoursesUpdateActivityGrade */
LearningCoursesUpdateActivityGradeSchema = z.object({
  grade: z.number().nullable().optional(),
  feedback: z.string().nullable().optional(),
  gradingDetails: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesUpdatePrerequisiteApiInput */
LearningCoursesUpdatePrerequisiteApiInputSchema = z.object({
  type: z.lazy(() => LearningCoursesPrerequisiteTypeSchema).optional(),
  minimumGrade: z.number().int().nullable().optional(),
  description: z.string().nullable().optional(),
  displayOrder: z.number().int().nullable().optional(),
  prerequisiteGroup: z.string().nullable().optional(),
});

/** Zod schema for LearningCoursesUpdatePricing */
LearningCoursesUpdatePricingSchema = z.object({
  price: z.number().nullable().optional(),
  currency: z.string().nullable().optional(),
  isSubscription: z.boolean().nullable().optional(),
  subscriptionDurationDays: z.number().int().nullable().optional(),
});

/** Zod schema for LearningCoursesUpdateProgramContent */
LearningCoursesUpdateProgramContentSchema = z.object({
  id: z.string().uuid(),
  title: z.string().min(0).max(255).nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => LearningCoursesProgramContentTypeSchema).optional(),
  body: z.string().nullable().optional(),
  sortOrder: z.number().int().nullable().optional(),
  isRequired: z.boolean().nullable().optional(),
  gradingMethod: z.lazy(() => LearningCoursesGradingMethodSchema).optional(),
  maxPoints: z.number().nullable().optional(),
  estimatedMinutes: z.number().int().nullable().optional(),
  visibility: z.lazy(() => LearningCoursesVisibilitySchema).optional(),
});

/** Zod schema for LearningCoursesUpdateProgram */
LearningCoursesUpdateProgramSchema = z.object({
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  thumbnail: z.string().nullable().optional(),
  videoShowcaseUrl: z.string().nullable().optional(),
  estimatedHours: z.number().int().nullable().optional(),
  visibility: z.lazy(() => ContentVisibilitySchema).optional(),
  category: z.lazy(() => ProgramCategorySchema).optional(),
  difficulty: z.lazy(() => LearningCoursesProgramDifficultySchema).optional(),
  skillsRequired: z.string().nullable().optional(),
  skillsProvided: z.string().nullable().optional(),
  enrollmentStatus: z.lazy(() => LearningCoursesEnrollmentStatusSchema).optional(),
  maxEnrollments: z.number().int().nullable().optional(),
  enrollmentDeadline: z.string().datetime().nullable().optional(),
});

/** Zod schema for LearningCoursesUpdateProgress */
LearningCoursesUpdateProgressSchema = z.object({
  status: z.lazy(() => LearningCoursesProgressStatusSchema).optional(),
  lastAccessedAt: z.string().datetime().nullable().optional(),
  additionalData: z.record(z.string(), z.record(z.string(), z.unknown())).nullable().optional(),
});

/** Zod schema for LearningCoursesUpdateProgressInput */
LearningCoursesUpdateProgressInputSchema = z.object({
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  completionPercentage: z.number().optional(),
});

/** Zod schema for LearningCoursesUpdateTimeSpentInput */
LearningCoursesUpdateTimeSpentInputSchema = z.object({
  programUserId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  additionalMinutes: z.number().int().optional(),
});

/** Zod schema for LearningCoursesUserProgress */
LearningCoursesUserProgressSchema = z.object({
  completionPercentage: z.number().optional(),
  lastAccessedAt: z.string().datetime().nullable().optional(),
  startedAt: z.string().datetime().nullable().optional(),
  completedAt: z.string().datetime().nullable().optional(),
  contentProgress: z
    .array(z.lazy(() => LearningCoursesContentProgressSchema))
    .nullable()
    .optional(),
});

/** Zod schema for LearningCoursesVisibility */
LearningCoursesVisibilitySchema = z.enum(['Public', 'Internal', 'Private', 'Restricted']);

/** Zod schema for LearningEnrollmentsEnrollUserInput */
LearningEnrollmentsEnrollUserInputSchema = z.object({
  courseId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  cohortId: z.string().uuid().nullable().optional(),
});

/** Zod schema for LearningEnrollmentsEnrollment */
LearningEnrollmentsEnrollmentSchema = z.object({
  id: z.string().uuid().optional(),
  courseId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  cohortId: z.string().uuid().nullable().optional(),
  status: z.lazy(() => LearningEnrollmentsEnrollmentStatusSchema).optional(),
  enrolledAt: z.string().datetime().optional(),
  completedAt: z.string().datetime().nullable().optional(),
  droppedAt: z.string().datetime().nullable().optional(),
  progress: z.number().int().optional(),
  lastActivityAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for LearningEnrollmentsEnrollmentStatus */
LearningEnrollmentsEnrollmentStatusSchema = z.enum(['Active', 'Paused', 'Completed', 'Dropped', 'Expired']);

/** Zod schema for LearningEnrollmentsUpdateEnrollmentProgressInput */
LearningEnrollmentsUpdateEnrollmentProgressInputSchema = z.object({
  progress: z.number().int().optional(),
});

/** Zod schema for MvcProblemDetails */
MvcProblemDetailsSchema = z
  .object({
    type: z.string().nullable().optional(),
    title: z.string().nullable().optional(),
    status: z.number().int().nullable().optional(),
    detail: z.string().nullable().optional(),
    instance: z.string().nullable().optional(),
  })
  .catchall(z.record(z.string(), z.unknown()));

/** Zod schema for ObjectsAttestationConveyancePreference */
ObjectsAttestationConveyancePreferenceSchema = z.enum(['None', 'Indirect', 'Direct', 'Enterprise']);

/** Zod schema for ObjectsAttestationStatementFormatIdentifier */
ObjectsAttestationStatementFormatIdentifierSchema = z.enum(['Packed', 'Tpm', 'AndroidKey', 'AndroidSafetyNet', 'FidoU2f', 'Apple', 'None']);

/** Zod schema for ObjectsAuthenticationExtensionsClientInputs */
ObjectsAuthenticationExtensionsClientInputsSchema = z.object({
  'example.extension.bool': z.boolean().nullable().optional(),
  exts: z.boolean().nullable().optional(),
  uvm: z.boolean().nullable().optional(),
  credProps: z.boolean().nullable().optional(),
  prf: z.lazy(() => ObjectsAuthenticationExtensionsPRFInputsSchema).optional(),
  largeBlob: z.lazy(() => ObjectsAuthenticationExtensionsLargeBlobInputsSchema).optional(),
  credentialProtectionPolicy: z.lazy(() => ObjectsCredentialProtectionPolicySchema).optional(),
  enforceCredentialProtectionPolicy: z.boolean().nullable().optional(),
});

/** Zod schema for ObjectsAuthenticationExtensionsLargeBlobInputs */
ObjectsAuthenticationExtensionsLargeBlobInputsSchema = z.object({
  support: z.lazy(() => ObjectsLargeBlobSupportSchema).optional(),
  read: z.boolean().optional(),
  write: z.string().nullable().optional(),
});

/** Zod schema for ObjectsAuthenticationExtensionsPRFInputs */
ObjectsAuthenticationExtensionsPRFInputsSchema = z.object({
  eval: z.lazy(() => ObjectsAuthenticationExtensionsPRFValuesSchema).optional(),
  evalByCredential: z.lazy(() => KeyValuePairStringAuthenticationExtensionsPRFValuesSchema).optional(),
});

/** Zod schema for ObjectsAuthenticationExtensionsPRFValues */
ObjectsAuthenticationExtensionsPRFValuesSchema = z.object({
  first: z.string().nullable(),
  second: z.string().nullable().optional(),
});

/** Zod schema for ObjectsAuthenticatorAttachment */
ObjectsAuthenticatorAttachmentSchema = z.enum(['Platform', 'CrossPlatform']);

/** Zod schema for ObjectsAuthenticatorTransport */
ObjectsAuthenticatorTransportSchema = z.enum(['Usb', 'Nfc', 'Ble', 'SmartCard', 'Hybrid', 'Internal']);

/** Zod schema for ObjectsCOSEAlgorithm */
ObjectsCOSEAlgorithmSchema = z.enum(['RS1', 'RS512', 'RS384', 'RS256', 'ES256K', 'PS512', 'PS384', 'PS256', 'ES512', 'ES384', 'EdDSA', 'ES256']);

/** Zod schema for ObjectsCredentialProtectionPolicy */
ObjectsCredentialProtectionPolicySchema = z.enum(['UserVerificationOptional', 'UserVerificationOptionalWithCredentialIdList', 'UserVerificationRequired']);

/** Zod schema for ObjectsLargeBlobSupport */
ObjectsLargeBlobSupportSchema = z.enum(['Required', 'Preferred']);

/** Zod schema for ObjectsPublicKeyCredentialDescriptor */
ObjectsPublicKeyCredentialDescriptorSchema = z.object({
  type: z.lazy(() => ObjectsPublicKeyCredentialTypeSchema).optional(),
  id: z.string().nullable().optional(),
  transports: z
    .array(z.lazy(() => ObjectsAuthenticatorTransportSchema))
    .nullable()
    .optional(),
});

/** Zod schema for ObjectsPublicKeyCredentialHint */
ObjectsPublicKeyCredentialHintSchema = z.enum(['SecurityKey', 'ClientDevice', 'Hybrid']);

/** Zod schema for ObjectsPublicKeyCredentialType */
ObjectsPublicKeyCredentialTypeSchema = z.enum(['PublicKey', 'Invalid']);

/** Zod schema for ObjectsResidentKeyRequirement */
ObjectsResidentKeyRequirementSchema = z.enum(['Required', 'Preferred', 'Discouraged']);

/** Zod schema for ObjectsUserVerificationRequirement */
ObjectsUserVerificationRequirementSchema = z.enum(['Required', 'Preferred', 'Discouraged']);

/** Zod schema for PagedResult */
PagedResultSchema = z.object({
  items: z
    .array(z.lazy(() => CommerceProductsProductSchema))
    .nullable()
    .optional(),
  totalCount: z.number().int().optional(),
  skip: z.number().int().optional(),
  take: z.number().int().optional(),
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  totalPages: z.number().int().optional(),
  hasNextPage: z.boolean().optional(),
  hasPreviousPage: z.boolean().optional(),
});

/** Zod schema for ResourcesArchiveResourceUsageRecordsInput */
ResourcesArchiveResourceUsageRecordsInputSchema = z.object({
  olderThan: z.string().datetime().optional(),
});

/** Zod schema for ResourcesCheckResourceQuotaInput */
ResourcesCheckResourceQuotaInputSchema = z.object({
  amount: z.number().int().optional(),
});

/** Zod schema for ResourcesCleanupOrphanedResourcesInput */
ResourcesCleanupOrphanedResourcesInputSchema = z.object({
  dryRun: z.boolean().optional(),
  resourceTypes: z
    .array(z.lazy(() => ResourcesResourceUsageTypeSchema))
    .nullable()
    .optional(),
});

/** Zod schema for ResourcesEffectiveSettingOutput */
ResourcesEffectiveSettingOutputSchema = z.object({
  key: z.string().nullable().optional(),
  value: z.string().nullable().optional(),
  isUserOverride: z.boolean().optional(),
});

/** Zod schema for ResourcesRecordTenantResourceUsageInput */
ResourcesRecordTenantResourceUsageInputSchema = z.object({
  resourceUsageType: z.lazy(() => ResourcesResourceUsageTypeSchema).optional(),
  count: z.number().int().optional(),
  periodStart: z.string().datetime().optional(),
  periodEnd: z.string().datetime().optional(),
  metadata: z.record(z.string(), z.string()).nullable().optional(),
});

/** Zod schema for ResourcesRecordUserResourceUsageInput */
ResourcesRecordUserResourceUsageInputSchema = z.object({
  resourceUsageType: z.lazy(() => ResourcesResourceUsageTypeSchema).optional(),
  count: z.number().int().optional(),
  periodStart: z.string().datetime().optional(),
  periodEnd: z.string().datetime().optional(),
  metadata: z.record(z.string(), z.string()).nullable().optional(),
});

/** Zod schema for ResourcesResourceMetadata */
ResourcesResourceMetadataSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  key: z.string().min(1).max(100),
  value: z.string().max(4000).nullable().optional(),
  dataType: z.string().max(50).nullable().optional(),
  description: z.string().max(500).nullable().optional(),
  category: z.string().max(100).nullable().optional(),
  isSystemManaged: z.boolean().optional(),
  isActive: z.boolean().optional(),
  displayOrder: z.number().int().optional(),
  userId: z.string().uuid().nullable().optional(),
  resourceId: z.string().uuid().nullable().optional(),
  rowVersion: z.string().nullable().optional(),
});

/** Zod schema for ResourcesResourceQuotaEnforcementResult */
ResourcesResourceQuotaEnforcementResultSchema = z.object({
  isAllowed: z.boolean().optional(),
  isSoftLimitExceeded: z.boolean().optional(),
  isHardLimitExceeded: z.boolean().optional(),
  currentUsage: z.number().int().optional(),
  softLimit: z.number().int().nullable().optional(),
  hardLimit: z.number().int().nullable().optional(),
  usagePercentage: z.number().optional(),
  excessAmount: z.number().int().optional(),
  message: z.string().nullable().optional(),
  type: z.lazy(() => ResourcesResourceUsageTypeSchema).optional(),
  nextReset: z.string().datetime().nullable().optional(),
  remainingQuota: z.number().int().nullable().optional(),
});

/** Zod schema for ResourcesResourceQuotaPeriod */
ResourcesResourceQuotaPeriodSchema = z.enum(['Daily', 'Weekly', 'Monthly', 'Quarterly', 'Yearly', 'Unlimited']);

/** Zod schema for ResourcesResourceQuotaOutput */
ResourcesResourceQuotaOutputSchema = z.object({
  id: z.string().uuid().optional(),
  tenantId: z.string().uuid().optional(),
  type: z.lazy(() => ResourcesResourceUsageTypeSchema).optional(),
  limit: z.number().int().optional(),
  currentUsage: z.number().int().optional(),
  remainingQuota: z.number().int().optional(),
  usagePercentage: z.number().optional(),
  softLimitPercentage: z.number().optional(),
  isActive: z.boolean().optional(),
  period: z.lazy(() => ResourcesResourceQuotaPeriodSchema).optional(),
  lastResetDate: z.string().datetime().optional(),
  nextResetDate: z.string().datetime().optional(),
  description: z.string().nullable().optional(),
  isSoftLimitExceeded: z.boolean().optional(),
  isHardLimitExceeded: z.boolean().optional(),
  shouldReset: z.boolean().optional(),
  softLimit: z.number().int().nullable().optional(),
  hardLimit: z.number().int().nullable().optional(),
});

/** Zod schema for ResourcesResourceSettings */
ResourcesResourceSettingsSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  key: z.string().min(1).max(100),
  value: z.string().max(4000).nullable().optional(),
  defaultValue: z.string().max(4000).nullable().optional(),
  dataType: z.string().max(50).nullable().optional(),
  description: z.string().max(500).nullable().optional(),
  category: z.string().max(100).nullable().optional(),
  isSystemManaged: z.boolean().optional(),
  isActive: z.boolean().optional(),
  allowUserOverride: z.boolean().optional(),
  displayOrder: z.number().int().optional(),
  userId: z.string().uuid().nullable().optional(),
  validationRules: z.string().max(1000).nullable().optional(),
  rowVersion: z.string().nullable().optional(),
});

/** Zod schema for ResourcesResourceUsageType */
ResourcesResourceUsageTypeSchema = z.enum([
  'Users',
  'Projects',
  'Storage',
  'ApiCalls',
  'Programs',
  'Courses',
  'FeatureFlags',
  'SubscriptionPlans',
  'Products',
  'TestingSessions',
  'Roles',
  'Tenants',
  'Subscriptions',
  'SLOs',
  'AccessReviewCampaigns',
  'SoDRules',
  'AbacPolicies',
  'ConditionalPolicies',
  'Wallets',
  'Disputes',
  'PromoCodes',
  'Orders',
  'AuditEntries',
  'Assets',
  'AssetStorage',
  'AssetDownloads',
  'AssetTransformations',
  'AiRequests',
  'AiTokens',
]);

/** Zod schema for ResourcesSetQuotaInput */
ResourcesSetQuotaInputSchema = z.object({
  softLimit: z.number().int().nullable().optional(),
  hardLimit: z.number().int().nullable().optional(),
  period: z.lazy(() => ResourcesResourceQuotaPeriodSchema).optional(),
  isActive: z.boolean().optional(),
  resetTime: z.string().nullable().optional(),
});

/** Zod schema for ResourcesSetResourceMetadataInput */
ResourcesSetResourceMetadataInputSchema = z.object({
  value: z.string().nullable().optional(),
  dataType: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  category: z.string().nullable().optional(),
  displayOrder: z.number().int().nullable().optional(),
});

/** Zod schema for ResourcesSetResourceSettingsInput */
ResourcesSetResourceSettingsInputSchema = z.object({
  value: z.string().nullable().optional(),
  defaultValue: z.string().nullable().optional(),
  dataType: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  category: z.string().nullable().optional(),
  allowUserOverride: z.boolean().nullable().optional(),
  displayOrder: z.number().int().nullable().optional(),
  validationRules: z.string().nullable().optional(),
});

/** Zod schema for ResourcesSetUserResourceSettingsInput */
ResourcesSetUserResourceSettingsInputSchema = z.object({
  value: z.string().nullable().optional(),
});

/** Zod schema for ResourcesToggleResourceQuotaInput */
ResourcesToggleResourceQuotaInputSchema = z.object({
  isActive: z.boolean().optional(),
});

/** Zod schema for ResourcesTrendGranularity */
ResourcesTrendGranularitySchema = z.enum(['Daily', 'Weekly', 'Monthly']);

/** Zod schema for ResourcesUsageRecord */
ResourcesUsageRecordSchema = z.object({
  isGlobal: z.boolean().optional(),
  isNew: z.boolean().optional(),
  isDeleted: z.boolean().optional(),
  domainEvents: z
    .array(z.lazy(() => CQRSIDomainEventSchema))
    .nullable()
    .optional(),
  id: z.string().uuid().optional(),
  version: z.number().int().optional(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  deletedAt: z.string().datetime().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  type: z.lazy(() => ResourcesResourceUsageTypeSchema).optional(),
  count: z.number().int().optional(),
  usageAmount: z.number().int().optional(),
  periodStart: z.string().datetime().optional(),
  periodEnd: z.string().datetime().optional(),
  averagePerDay: z.number().nullable().optional(),
  peakUsage: z.number().int().nullable().optional(),
  peakUsageDate: z.string().datetime().nullable().optional(),
  metadata: z.string().max(1000).nullable().optional(),
  source: z.string().max(50).nullable().optional(),
  userId: z.string().uuid().nullable().optional(),
  resourceId: z.string().uuid().nullable().optional(),
  resourceQuotaId: z.string().uuid().nullable().optional(),
});

/** Zod schema for ResourcesUsageTrendDataPoint */
ResourcesUsageTrendDataPointSchema = z.object({
  period: z.string().datetime().optional(),
  totalUsage: z.number().int().optional(),
  tenantCount: z.number().int().optional(),
});

/** Zod schema for ResourcesUsageTrendsResult */
ResourcesUsageTrendsResultSchema = z.object({
  type: z.lazy(() => ResourcesResourceUsageTypeSchema).optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  granularity: z.lazy(() => ResourcesTrendGranularitySchema).optional(),
  dataPoints: z
    .array(z.lazy(() => ResourcesUsageTrendDataPointSchema))
    .nullable()
    .optional(),
});

/** Zod schema for SocialBlogBlogPost */
SocialBlogBlogPostSchema = z.object({
  id: z.string().uuid().optional(),
  authorId: z.string().uuid().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  title: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  excerpt: z.string().nullable().optional(),
  content: z.string().nullable().optional(),
  coverImageUrl: z.string().nullable().optional(),
  status: z.lazy(() => SocialBlogBlogPostStatusSchema).optional(),
  publishedAt: z.string().datetime().nullable().optional(),
  isFeatured: z.boolean().optional(),
  allowComments: z.boolean().optional(),
  viewsCount: z.number().int().optional(),
  likesCount: z.number().int().optional(),
  commentsCount: z.number().int().optional(),
  readTimeMinutes: z.number().int().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for SocialBlogBlogPostStatus */
SocialBlogBlogPostStatusSchema = z.enum(['Draft', 'Published', 'Archived']);

/** Zod schema for SocialBlogCreateBlogPostInput */
SocialBlogCreateBlogPostInputSchema = z.object({
  authorId: z.string().uuid().optional(),
  title: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  content: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for SocialFeedAddFeedItemInput */
SocialFeedAddFeedItemInputSchema = z.object({
  userId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  contentType: z.lazy(() => SocialFeedFeedContentTypeSchema).optional(),
  authorId: z.string().uuid().optional(),
  reason: z.lazy(() => SocialFeedFeedItemReasonSchema).optional(),
  contentCreatedAt: z.string().datetime().nullable().optional(),
  relevanceScore: z.number().optional(),
});

/** Zod schema for SocialFeedFeedContentType */
SocialFeedFeedContentTypeSchema = z.enum(['Post', 'BlogPost', 'CourseReview', 'ProjectUpdate', 'Achievement', 'CourseCompletion']);

/** Zod schema for SocialFeedFeedItem */
SocialFeedFeedItemSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  contentId: z.string().uuid().optional(),
  contentType: z.lazy(() => SocialFeedFeedContentTypeSchema).optional(),
  authorId: z.string().uuid().optional(),
  relevanceScore: z.number().optional(),
  reason: z.lazy(() => SocialFeedFeedItemReasonSchema).optional(),
  isRead: z.boolean().optional(),
  isHidden: z.boolean().optional(),
  contentCreatedAt: z.string().datetime().optional(),
  createdAt: z.string().datetime().optional(),
});

/** Zod schema for SocialFeedFeedItemReason */
SocialFeedFeedItemReasonSchema = z.enum(['Following', 'Trending', 'Recommended', 'Mentioned', 'Replied', 'Liked', 'InNetwork']);

/** Zod schema for SocialGroupsApproveSocialGroupMemberInput */
SocialGroupsApproveSocialGroupMemberInputSchema = z.object({
  approvedByUserId: z.string().uuid().optional(),
});

/** Zod schema for SocialGroupsChangeSocialGroupMemberRoleInput */
SocialGroupsChangeSocialGroupMemberRoleInputSchema = z.object({
  role: z.lazy(() => SocialGroupsSocialGroupMemberRoleSchema).optional(),
});

/** Zod schema for SocialGroupsCreateSocialGroupInput */
SocialGroupsCreateSocialGroupInputSchema = z.object({
  ownerId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  type: z.lazy(() => SocialGroupsSocialGroupTypeSchema).optional(),
  visibility: z.lazy(() => SocialGroupsSocialGroupVisibilitySchema).optional(),
  description: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for SocialGroupsJoinSocialGroupInput */
SocialGroupsJoinSocialGroupInputSchema = z.object({
  userId: z.string().uuid().optional(),
  requestedRole: z.lazy(() => SocialGroupsSocialGroupMemberRoleSchema).optional(),
});

/** Zod schema for SocialGroupsSocialGroup */
SocialGroupsSocialGroupSchema = z.object({
  id: z.string().uuid().optional(),
  tenantId: z.string().uuid().nullable().optional(),
  ownerId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => SocialGroupsSocialGroupTypeSchema).optional(),
  visibility: z.lazy(() => SocialGroupsSocialGroupVisibilitySchema).optional(),
  status: z.lazy(() => SocialGroupsSocialGroupStatusSchema).optional(),
  memberCount: z.number().int().optional(),
  pendingMemberCount: z.number().int().optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for SocialGroupsSocialGroupMember */
SocialGroupsSocialGroupMemberSchema = z.object({
  id: z.string().uuid().optional(),
  groupId: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  role: z.lazy(() => SocialGroupsSocialGroupMemberRoleSchema).optional(),
  status: z.lazy(() => SocialGroupsSocialGroupMembershipStatusSchema).optional(),
  requestedAt: z.string().datetime().optional(),
  joinedAt: z.string().datetime().nullable().optional(),
  approvedByUserId: z.string().uuid().nullable().optional(),
  removedAt: z.string().datetime().nullable().optional(),
});

/** Zod schema for SocialGroupsSocialGroupMemberRole */
SocialGroupsSocialGroupMemberRoleSchema = z.enum(['Owner', 'Admin', 'Moderator', 'Member']);

/** Zod schema for SocialGroupsSocialGroupMembershipStatus */
SocialGroupsSocialGroupMembershipStatusSchema = z.enum(['Pending', 'Active', 'Rejected', 'Removed']);

/** Zod schema for SocialGroupsSocialGroupStatus */
SocialGroupsSocialGroupStatusSchema = z.enum(['Active', 'Archived', 'Suspended']);

/** Zod schema for SocialGroupsSocialGroupType */
SocialGroupsSocialGroupTypeSchema = z.enum(['StudyGroup', 'ProjectTeam', 'InterestCommunity', 'CourseCohort', 'Institution', 'GameJamTeam']);

/** Zod schema for SocialGroupsSocialGroupVisibility */
SocialGroupsSocialGroupVisibilitySchema = z.enum(['Public', 'Private', 'InviteOnly']);

/** Zod schema for SocialGroupsUpdateSocialGroupInput */
SocialGroupsUpdateSocialGroupInputSchema = z.object({
  name: z.string().nullable().optional(),
  slug: z.string().nullable().optional(),
  type: z.lazy(() => SocialGroupsSocialGroupTypeSchema).optional(),
  visibility: z.lazy(() => SocialGroupsSocialGroupVisibilitySchema).optional(),
  description: z.string().nullable().optional(),
});

/** Zod schema for SocialProfilesAddProfilePortfolioItemBody */
SocialProfilesAddProfilePortfolioItemBodySchema = z.object({
  title: z.string().nullable().optional(),
  projectId: z.string().uuid().nullable().optional(),
  description: z.string().nullable().optional(),
  url: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  isPinned: z.boolean().optional(),
  displayOrder: z.number().int().optional(),
});

/** Zod schema for SocialProfilesAddProfileSkillBody */
SocialProfilesAddProfileSkillBodySchema = z.object({
  name: z.string().nullable().optional(),
  proficiency: z.lazy(() => SocialProfilesProfileSkillProficiencySchema).optional(),
  displayOrder: z.number().int().optional(),
});

/** Zod schema for SocialProfilesProfileAvailabilityStatus */
SocialProfilesProfileAvailabilityStatusSchema = z.enum(['NotSet', 'OpenToWork', 'OpenToCollaborate', 'Busy', 'Hidden']);

/** Zod schema for SocialProfilesProfilePortfolioItem */
SocialProfilesProfilePortfolioItemSchema = z.object({
  id: z.string().uuid().optional(),
  profileId: z.string().uuid().optional(),
  projectId: z.string().uuid().nullable().optional(),
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  url: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  isPinned: z.boolean().optional(),
  displayOrder: z.number().int().optional(),
});

/** Zod schema for SocialProfilesProfileSkill */
SocialProfilesProfileSkillSchema = z.object({
  id: z.string().uuid().optional(),
  profileId: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  proficiency: z.lazy(() => SocialProfilesProfileSkillProficiencySchema).optional(),
  displayOrder: z.number().int().optional(),
});

/** Zod schema for SocialProfilesProfileSkillProficiency */
SocialProfilesProfileSkillProficiencySchema = z.enum(['Beginner', 'Intermediate', 'Advanced', 'Expert']);

/** Zod schema for SocialProfilesProfileVisibility */
SocialProfilesProfileVisibilitySchema = z.enum(['Private', 'Connections', 'Public']);

/** Zod schema for SocialProfilesSocialProfile */
SocialProfilesSocialProfileSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  handle: z.string().nullable().optional(),
  displayName: z.string().nullable().optional(),
  bio: z.string().nullable().optional(),
  avatarUrl: z.string().nullable().optional(),
  bannerUrl: z.string().nullable().optional(),
  headline: z.string().nullable().optional(),
  location: z.string().nullable().optional(),
  timeZone: z.string().nullable().optional(),
  websiteUrl: z.string().nullable().optional(),
  socialLinksJson: z.string().nullable().optional(),
  visibility: z.lazy(() => SocialProfilesProfileVisibilitySchema).optional(),
  availabilityStatus: z.lazy(() => SocialProfilesProfileAvailabilityStatusSchema).optional(),
  showActivity: z.boolean().optional(),
  showPortfolio: z.boolean().optional(),
  showSkills: z.boolean().optional(),
  verifiedAt: z.string().datetime().nullable().optional(),
  completenessScore: z.number().int().optional(),
  followerCount: z.number().int().optional(),
  followingCount: z.number().int().optional(),
  postCount: z.number().int().optional(),
  projectCount: z.number().int().optional(),
  skills: z
    .array(z.lazy(() => SocialProfilesProfileSkillSchema))
    .nullable()
    .optional(),
  portfolioItems: z
    .array(z.lazy(() => SocialProfilesProfilePortfolioItemSchema))
    .nullable()
    .optional(),
});

/** Zod schema for SocialProfilesUpdateProfilePortfolioItemBody */
SocialProfilesUpdateProfilePortfolioItemBodySchema = z.object({
  title: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  url: z.string().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
  isPinned: z.boolean().optional(),
  displayOrder: z.number().int().optional(),
});

/** Zod schema for SocialProfilesUpdateProfilePrivacyBody */
SocialProfilesUpdateProfilePrivacyBodySchema = z.object({
  visibility: z.lazy(() => SocialProfilesProfileVisibilitySchema).optional(),
  showActivity: z.boolean().optional(),
  showPortfolio: z.boolean().optional(),
  showSkills: z.boolean().optional(),
});

/** Zod schema for SocialProfilesUpdateProfileStatsBody */
SocialProfilesUpdateProfileStatsBodySchema = z.object({
  followerCount: z.number().int().optional(),
  followingCount: z.number().int().optional(),
  postCount: z.number().int().optional(),
  projectCount: z.number().int().optional(),
});

/** Zod schema for SocialProfilesUpdateSocialProfileBody */
SocialProfilesUpdateSocialProfileBodySchema = z.object({
  handle: z.string().nullable().optional(),
  displayName: z.string().nullable().optional(),
  bio: z.string().nullable().optional(),
  avatarUrl: z.string().nullable().optional(),
  bannerUrl: z.string().nullable().optional(),
  headline: z.string().nullable().optional(),
  location: z.string().nullable().optional(),
  timeZone: z.string().nullable().optional(),
  websiteUrl: z.string().nullable().optional(),
  socialLinksJson: z.string().nullable().optional(),
  availabilityStatus: z.lazy(() => SocialProfilesProfileAvailabilityStatusSchema).optional(),
});

/** Zod schema for SocialReactionsReaction */
SocialReactionsReactionSchema = z.object({
  id: z.string().uuid().optional(),
  userId: z.string().uuid().optional(),
  targetId: z.string().uuid().optional(),
  targetType: z.lazy(() => SocialReactionsReactionTargetTypeSchema).optional(),
  type: z.lazy(() => SocialReactionsReactionTypeSchema).optional(),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional(),
});

/** Zod schema for SocialReactionsReactionTargetType */
SocialReactionsReactionTargetTypeSchema = z.enum(['Post', 'Comment', 'BlogPost', 'CourseReview', 'Discussion', 'Reply']);

/** Zod schema for SocialReactionsReactionType */
SocialReactionsReactionTypeSchema = z.enum(['Like', 'Love', 'Insightful', 'Celebrate', 'Support', 'Curious']);

/** Zod schema for SocialReactionsRemoveReactionInput */
SocialReactionsRemoveReactionInputSchema = z.object({
  userId: z.string().uuid().optional(),
  targetId: z.string().uuid().optional(),
  targetType: z.lazy(() => SocialReactionsReactionTargetTypeSchema).optional(),
});

/** Zod schema for SocialReactionsSetReactionInput */
SocialReactionsSetReactionInputSchema = z.object({
  userId: z.string().uuid().optional(),
  targetId: z.string().uuid().optional(),
  targetType: z.lazy(() => SocialReactionsReactionTargetTypeSchema).optional(),
  type: z.lazy(() => SocialReactionsReactionTypeSchema).optional(),
});

/** Zod schema for SocialReactionsTargetReactionSummary */
SocialReactionsTargetReactionSummarySchema = z.object({
  targetId: z.string().uuid().optional(),
  targetType: z.lazy(() => SocialReactionsReactionTargetTypeSchema).optional(),
  counts: z
    .object({
      Like: z.number().int(),
      Love: z.number().int(),
      Insightful: z.number().int(),
      Celebrate: z.number().int(),
      Support: z.number().int(),
      Curious: z.number().int(),
    })
    .nullable()
    .optional(),
  total: z.number().int().optional(),
});

/** Zod schema for TagsCreateTagProficiencyInput */
TagsCreateTagProficiencyInputSchema = z.object({
  name: z.string().nullable().optional(),
  type: z.lazy(() => TagsTagTypeSchema).optional(),
  proficiencyLevel: z.lazy(() => TagsSkillProficiencyLevelSchema).optional(),
  description: z.string().nullable().optional(),
  color: z.string().nullable().optional(),
  icon: z.string().nullable().optional(),
});

/** Zod schema for TagsCreateTagRelationshipInput */
TagsCreateTagRelationshipInputSchema = z.object({
  sourceId: z.string().uuid().optional(),
  targetId: z.string().uuid().optional(),
  type: z.lazy(() => TagsTagRelationshipTypeSchema).optional(),
  weight: z.number().nullable().optional(),
  metadata: z.string().nullable().optional(),
});

/** Zod schema for TagsCreateTagInput */
TagsCreateTagInputSchema = z.object({
  name: z.string().nullable().optional(),
  type: z.lazy(() => TagsTagTypeSchema).optional(),
  description: z.string().nullable().optional(),
  color: z.string().nullable().optional(),
  icon: z.string().nullable().optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for TagsSkillProficiencyLevel */
TagsSkillProficiencyLevelSchema = z.enum(['Beginner', 'Elementary', 'Intermediate', 'Advanced', 'Expert', 'Master']);

/** Zod schema for TagsTag */
TagsTagSchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => TagsTagTypeSchema).optional(),
  color: z.string().nullable().optional(),
  icon: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
  tenantId: z.string().uuid().nullable().optional(),
});

/** Zod schema for TagsTagProficiency */
TagsTagProficiencySchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  type: z.lazy(() => TagsTagTypeSchema).optional(),
  proficiencyLevel: z.lazy(() => TagsSkillProficiencyLevelSchema).optional(),
  color: z.string().nullable().optional(),
  icon: z.string().nullable().optional(),
  isActive: z.boolean().optional(),
});

/** Zod schema for TagsTagRelationship */
TagsTagRelationshipSchema = z.object({
  id: z.string().uuid().optional(),
  sourceId: z.string().uuid().optional(),
  targetId: z.string().uuid().optional(),
  type: z.lazy(() => TagsTagRelationshipTypeSchema).optional(),
  weight: z.number().nullable().optional(),
  metadata: z.string().nullable().optional(),
});

/** Zod schema for TagsTagRelationshipType */
TagsTagRelationshipTypeSchema = z.enum(['Related', 'Parent', 'Child', 'Requires', 'Suggested']);

/** Zod schema for TagsTagType */
TagsTagTypeSchema = z.enum(['Skill', 'Topic', 'Technology', 'Difficulty', 'Category', 'Industry', 'Certification']);

/** Zod schema for TagsUpdateTagInput */
TagsUpdateTagInputSchema = z.object({
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  color: z.string().nullable().optional(),
  icon: z.string().nullable().optional(),
  isActive: z.boolean().nullable().optional(),
});
