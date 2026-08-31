/**
 * @game-guild/client - EconomyAdministration Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class EconomyAdministrationModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAdminEconomyAdRewardsReports(query?: {
    network?: string;
    limit?: number;
  }): Promise<Result<Array<Types.EconomyAdRewardsDurableAdProviderReportStatus>, ApiError>> {
    const url = '/api/v1/admin/economy/ad-rewards/reports';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.EconomyAdRewardsDurableAdProviderReportStatus>, ApiError>;
  }

  /**
   */
  async postAdminEconomyAdRewardsReports(
    body: Types.EconomyAdRewardsAdProviderReport,
  ): Promise<Result<Types.EconomyAdRewardsDurableAdProviderReportImportResult, ApiError>> {
    const url = '/api/v1/admin/economy/ad-rewards/reports';

    // Validate request body
    const validatedBody = safeParse(Types.EconomyAdRewardsAdProviderReportSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyAdRewardsDurableAdProviderReportImportResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAdminEconomyBountiesExpired(): Promise<Result<Array<Types.EconomyBountiesDurableBountyView>, ApiError>> {
    const url = '/api/v1/admin/economy/bounties/expired';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.EconomyBountiesDurableBountyView>, ApiError>;
  }

  /**
   */
  async getAdminEconomyCapabilitiesConfiguration(query?: {
    includeInactiveKillSwitches?: boolean;
    limit?: number;
  }): Promise<Result<Types.EconomyOperationsEconomyCapabilityConfigurationSnapshot, ApiError>> {
    const url = '/api/v1/admin/economy/capabilities/configuration';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyOperationsEconomyCapabilityConfigurationSnapshotSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyCapabilitiesReadiness(
    body: Types.APIControllersInspectEconomyCapabilityReadinessInput,
  ): Promise<Result<Types.EconomyRiskEconomyCapabilityEvaluationResult, ApiError>> {
    const url = '/api/v1/admin/economy/capabilities/readiness';

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersInspectEconomyCapabilityReadinessInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyCapabilityEvaluationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyCustodyObservations(
    body: Types.EconomyReservesCustodyObservationCommand,
  ): Promise<Result<Types.EconomyReservesDurableCustodyObservation, ApiError>> {
    const url = '/api/v1/admin/economy/custody/observations';

    // Validate request body
    const validatedBody = safeParse(Types.EconomyReservesCustodyObservationCommandSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyReservesDurableCustodyObservationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyKillSwitches(
    body: Types.APIControllersActivateEconomyKillSwitchInput,
  ): Promise<Result<Types.EconomyRiskEconomyKillSwitchState, ApiError>> {
    const url = '/api/v1/admin/economy/kill-switches';

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersActivateEconomyKillSwitchInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyKillSwitchStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyKillSwitchesRelease(killSwitchId: string): Promise<Result<Types.EconomyRiskEconomyKillSwitchState, ApiError>> {
    const url = `/api/v1/admin/economy/kill-switches/${killSwitchId}/release`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyKillSwitchStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyKillSwitchesReleaseApprovals(
    killSwitchId: string,
    body: Types.APIControllersEconomyStepUpInput,
  ): Promise<Result<Types.EconomyRiskEconomyKillSwitchState, ApiError>> {
    const url = `/api/v1/admin/economy/kill-switches/${killSwitchId}/release-approvals`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersEconomyStepUpInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyKillSwitchStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyKillSwitchesReleaseProposals(
    killSwitchId: string,
    body: Types.APIControllersEconomyStepUpInput,
  ): Promise<Result<Types.EconomyRiskEconomyKillSwitchState, ApiError>> {
    const url = `/api/v1/admin/economy/kill-switches/${killSwitchId}/release-proposals`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersEconomyStepUpInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyKillSwitchStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyLedgerAnchors(
    body: Types.APIControllersPublishEconomyAnchorInput,
  ): Promise<Result<Types.EconomyLedgerEconomyAnchorPublicationResult, ApiError>> {
    const url = '/api/v1/admin/economy/ledger/anchors';

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersPublishEconomyAnchorInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyLedgerEconomyAnchorPublicationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyLedgerAnchorsVerificationRuns(): Promise<Result<Types.EconomyLedgerAnchorVerificationRunResult, ApiError>> {
    const url = '/api/v1/admin/economy/ledger/anchors/verification-runs';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyLedgerAnchorVerificationRunResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAdminEconomyLedgerHealth(): Promise<Result<Types.EconomyOperationsEconomyLedgerHealthSnapshot, ApiError>> {
    const url = '/api/v1/admin/economy/ledger/health';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyOperationsEconomyLedgerHealthSnapshotSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyLedgerProjectionGenerations(): Promise<Result<Types.EconomyProjectionsProjectionGenerationState, ApiError>> {
    const url = '/api/v1/admin/economy/ledger/projection-generations';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyProjectionsProjectionGenerationStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyLedgerProjectionGenerationsApprovals(
    generation: number,
    body: Types.APIControllersEconomyStepUpInput,
  ): Promise<Result<Types.EconomyProjectionsProjectionGenerationState, ApiError>> {
    const url = `/api/v1/admin/economy/ledger/projection-generations/${generation}/approvals`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersEconomyStepUpInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyProjectionsProjectionGenerationStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyLedgerVerificationRuns(): Promise<Result<Types.EconomyLedgerJournalIntegrityRunResult, ApiError>> {
    const url = '/api/v1/admin/economy/ledger/verification-runs';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyLedgerJournalIntegrityRunResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyMarketplaceSettlementsRefund(
    settlementId: string,
    body: Types.APIControllersRefundMarketplaceSettlementInput,
  ): Promise<Result<Types.EconomyMarketplaceDurableMarketplaceRefundResult, ApiError>> {
    const url = `/api/v1/admin/economy/marketplace/settlements/${settlementId}:refund`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersRefundMarketplaceSettlementInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyMarketplaceDurableMarketplaceRefundResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Reserve FIFO funds for a fully approved payout request
   *
   * Tenant and actor authority come exclusively from the authenticated actor context. Fresh MFA and the full capability control plane are required.
   */
  async postAdminEconomyPayoutRequestsReserve(
    requestId: string,
    body: Types.APIControllersReserveApprovedPayoutExecutionInput,
  ): Promise<Result<Types.APIControllersEconomyPayoutExecutionOperation, ApiError>> {
    const url = `/api/v1/admin/economy/payout-requests/${requestId}/reserve`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersReserveApprovedPayoutExecutionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIControllersEconomyPayoutExecutionOperationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * List tenant-scoped payout execution operations
   */
  async getAdminEconomyPayoutRequestsOperationsForGetAdminEconomyPayoutRequestsOperations(query?: {
    take?: number;
  }): Promise<Result<Array<Types.APIControllersEconomyPayoutExecutionOperation>, ApiError>> {
    const url = '/api/v1/admin/economy/payout-requests/operations';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.APIControllersEconomyPayoutExecutionOperation>, ApiError>;
  }

  /**
   * Get a tenant-scoped payout execution operation
   */
  async getAdminEconomyPayoutRequestsOperationsForGetAdminEconomyPayoutRequestsOperationsByOperationId(
    operationId: string,
  ): Promise<Result<Types.APIControllersEconomyPayoutExecutionOperation, ApiError>> {
    const url = `/api/v1/admin/economy/payout-requests/operations/${operationId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIControllersEconomyPayoutExecutionOperationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Atomically authorize and enqueue an approved payout dispatch
   */
  async postAdminEconomyPayoutRequestsOperationsDispatch(
    operationId: string,
    body: Types.APIControllersDispatchPayoutExecutionInput,
  ): Promise<Result<Types.APIControllersEconomyPayoutExecutionOperation, ApiError>> {
    const url = `/api/v1/admin/economy/payout-requests/operations/${operationId}/dispatch`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersDispatchPayoutExecutionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIControllersEconomyPayoutExecutionOperationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Reconcile an in-flight payout directly with its provider
   */
  async postAdminEconomyPayoutRequestsOperationsReconcile(operationId: string): Promise<Result<Types.APIControllersEconomyPayoutExecutionOperation, ApiError>> {
    const url = `/api/v1/admin/economy/payout-requests/operations/${operationId}/reconcile`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIControllersEconomyPayoutExecutionOperationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyPolicies(body: Types.APIControllersProposeEconomyPolicyInput): Promise<Result<Types.EconomyRiskEconomyCapabilityPolicy, ApiError>> {
    const url = '/api/v1/admin/economy/policies';

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersProposeEconomyPolicyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyCapabilityPolicySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyPoliciesApprove(
    policyId: string,
    body: Types.APIControllersApproveEconomyPolicyInput,
  ): Promise<Result<Types.EconomyRiskEconomyCapabilityPolicy, ApiError>> {
    const url = `/api/v1/admin/economy/policies/${policyId}/approve`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersApproveEconomyPolicyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyRiskEconomyCapabilityPolicySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAdminEconomyReservesLiabilities(): Promise<Result<Types.EconomyReservesEconomyLiabilitySnapshot, ApiError>> {
    const url = '/api/v1/admin/economy/reserves/liabilities';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyReservesEconomyLiabilitySnapshotSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyReservesProposals(
    body: Types.APIControllersProposeEconomyReserveInput,
  ): Promise<Result<Types.EconomyReservesDurableReserveProposalState, ApiError>> {
    const url = '/api/v1/admin/economy/reserves/proposals';

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersProposeEconomyReserveInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyReservesDurableReserveProposalStateSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminEconomyReservesProposalsApprove(
    proposalId: string,
    body: Types.APIControllersEconomyStepUpInput,
  ): Promise<Result<Types.EconomyReservesReserveHead, ApiError>> {
    const url = `/api/v1/admin/economy/reserves/proposals/${proposalId}/approve`;

    // Validate request body
    const validatedBody = safeParse(Types.APIControllersEconomyStepUpInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyReservesReserveHeadSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createEconomyAdministrationModule(client: ApiClient): EconomyAdministrationModule {
  return new EconomyAdministrationModule(client);
}
