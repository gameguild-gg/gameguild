'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import {
  createEconomyConsoleModules,
  requireEconomyConsoleSurface,
  type EconomyConsoleSurface,
} from './console';
import { buildEconomyStepUpBinding } from './console-step-up-binding';

export type EconomyConsoleAction =
  | 'readiness.inspect'
  | 'payout.reserve' | 'payout.dispatch' | 'payout.reconcile'
  | 'risk.approve' | 'risk.reject' | 'hold.release.propose' | 'hold.release.approve'
  | 'financial-crime.assign' | 'financial-crime.decide' | 'financial-crime.reference'
  | 'trust-safety.assign' | 'trust-safety.decide'
  | 'policy.propose' | 'policy.approve'
  | 'custody.record' | 'reserve.propose' | 'reserve.approve'
  | 'ledger.verify' | 'anchor.publish' | 'anchor.verify' | 'projection.rebuild' | 'projection.approve'
  | 'kill-switch.activate' | 'kill-switch.release.propose' | 'kill-switch.release.approve' | 'kill-switch.release.execute'
  | 'ad-reward.report.import'
  | 'marketplace.refund'
  | 'treasury.propose' | 'treasury.approve' | 'treasury.dispatch' | 'treasury.reconcile'
  | 'legacy.capture' | 'legacy.backfill' | 'legacy.reconcile'
  | 'legacy.cutover.propose' | 'legacy.cutover.approve' | 'legacy.cutover.rollback';

export interface EconomyConsoleActionResult {
  challengeId?: string;
  expiresAt?: string;
  message: string;
  receipt?: string;
  success: boolean;
}

type ActionValues = Record<string, unknown>;
type ApiResult = { ok: boolean; error?: { message?: string | null } };

const actionSurface: Record<EconomyConsoleAction, EconomyConsoleSurface> = {
  'readiness.inspect': 'readiness',
  'payout.reserve': 'payout-operations',
  'payout.dispatch': 'payout-operations',
  'payout.reconcile': 'payout-operations',
  'risk.approve': 'risk-reviews',
  'risk.reject': 'risk-reviews',
  'hold.release.propose': 'risk-reviews',
  'hold.release.approve': 'risk-reviews',
  'financial-crime.assign': 'financial-crime',
  'financial-crime.decide': 'financial-crime',
  'financial-crime.reference': 'financial-crime',
  'trust-safety.assign': 'trust-safety',
  'trust-safety.decide': 'trust-safety',
  'policy.propose': 'policies',
  'policy.approve': 'policies',
  'custody.record': 'reserves',
  'reserve.propose': 'reserves',
  'reserve.approve': 'reserves',
  'ledger.verify': 'ledger',
  'anchor.publish': 'ledger',
  'anchor.verify': 'ledger',
  'projection.rebuild': 'ledger',
  'projection.approve': 'ledger',
  'kill-switch.activate': 'kill-switches',
  'kill-switch.release.propose': 'kill-switches',
  'kill-switch.release.approve': 'kill-switches',
  'kill-switch.release.execute': 'kill-switches',
  'ad-reward.report.import': 'ad-rewards',
  'marketplace.refund': 'marketplace',
  'treasury.propose': 'treasury',
  'treasury.approve': 'treasury',
  'treasury.dispatch': 'treasury',
  'treasury.reconcile': 'treasury',
  'legacy.capture': 'legacy-migration',
  'legacy.backfill': 'legacy-migration',
  'legacy.reconcile': 'legacy-migration',
  'legacy.cutover.propose': 'legacy-migration',
  'legacy.cutover.approve': 'legacy-migration',
  'legacy.cutover.rollback': 'legacy-migration',
};

const stepUpActions = new Set<EconomyConsoleAction>([
  'payout.reserve', 'payout.dispatch',
  'hold.release.propose', 'hold.release.approve',
  'policy.approve', 'reserve.approve', 'projection.approve',
  'kill-switch.release.propose', 'kill-switch.release.approve',
  'treasury.propose', 'treasury.approve', 'treasury.dispatch',
  'legacy.cutover.propose', 'legacy.cutover.approve', 'legacy.cutover.rollback',
]);

function failure(message: string): EconomyConsoleActionResult {
  return { success: false, message };
}

function required(values: ActionValues, key: string): string {
  const value = String(values[key] ?? '').trim();
  if (!value) throw new Error(`${key} is required.`);
  return value;
}

function optional(values: ActionValues, key: string): string | undefined {
  const value = String(values[key] ?? '').trim();
  return value || undefined;
}

function positiveInteger(values: ActionValues, key: string): number {
  const value = Number(required(values, key));
  if (!Number.isSafeInteger(value) || value <= 0) throw new Error(`${key} must be a positive integer.`);
  return value;
}

function nonNegativeInteger(values: ActionValues, key: string): number {
  const value = Number(required(values, key));
  if (!Number.isSafeInteger(value) || value < 0) throw new Error(`${key} must be a non-negative integer.`);
  return value;
}

function optionalInteger(values: ActionValues, key: string): number | undefined {
  return optional(values, key) === undefined ? undefined : nonNegativeInteger(values, key);
}

function dateTime(values: ActionValues, key: string): string {
  const parsed = new Date(required(values, key));
  if (Number.isNaN(parsed.valueOf())) throw new Error(`${key} must be a valid date and time.`);
  return parsed.toISOString();
}

function boolean(values: ActionValues, key: string): boolean {
  return values[key] === true || values[key] === 'true' || values[key] === 'on';
}

function jsonObject(values: ActionValues, key: string): Record<string, unknown> {
  const parsed = JSON.parse(required(values, key)) as unknown;
  if (!parsed || Array.isArray(parsed) || typeof parsed !== 'object') throw new Error(`${key} must be a JSON object.`);
  return parsed as Record<string, unknown>;
}

function optionalJsonArray(values: ActionValues, key: string): Array<unknown> | undefined {
  const source = optional(values, key);
  if (!source) return undefined;
  const parsed = JSON.parse(source) as unknown;
  if (!Array.isArray(parsed)) throw new Error(`${key} must be a JSON array.`);
  return parsed;
}

export async function beginEconomyConsoleStepUpAction(
  action: EconomyConsoleAction,
  values: ActionValues,
): Promise<EconomyConsoleActionResult> {
  try {
    if (!stepUpActions.has(action)) return failure('This action does not require step-up authentication.');
    await requireEconomyConsoleSurface(actionSurface[action]);
    const binding = buildEconomyStepUpBinding(action, values);
    const { authStepUp } = await createEconomyConsoleModules();
    const result = await authStepUp.postAuthStepUpChallenges(binding);
    if (!result.ok || !result.data.challengeId) return failure(result.ok ? 'The MFA challenge was not created.' : result.error.message || 'The MFA challenge was not created.');
    return { success: true, message: 'MFA challenge created.', challengeId: result.data.challengeId, expiresAt: result.data.expiresAt };
  } catch (error) {
    return failure(error instanceof Error ? error.message : 'The MFA challenge was not created.');
  }
}

export async function verifyEconomyConsoleStepUpAction(challengeId: string, evidence: string): Promise<EconomyConsoleActionResult> {
  try {
    if (!challengeId.trim() || !evidence.trim()) return failure('Challenge and MFA evidence are required.');
    const { authStepUp } = await createEconomyConsoleModules();
    const result = await authStepUp.postAuthStepUpChallengesVerify(challengeId, { method: 'Totp', evidence: evidence.trim() });
    if (!result.ok || !result.data.receipt) return failure(result.ok ? 'MFA verification did not produce a receipt.' : result.error.message || 'MFA verification failed.');
    return { success: true, message: 'MFA verified. The receipt can be consumed once.', receipt: result.data.receipt, expiresAt: result.data.expiresAt };
  } catch (error) {
    return failure(error instanceof Error ? error.message : 'MFA verification failed.');
  }
}

function actionSucceeded(result: ApiResult, message: string): EconomyConsoleActionResult {
  return result.ok ? { success: true, message } : failure(result.error?.message || 'The operation was not accepted.');
}

export async function executeEconomyConsoleAction(
  action: EconomyConsoleAction,
  values: ActionValues,
  stepUpReceipt?: string,
): Promise<EconomyConsoleActionResult> {
  try {
    await requireEconomyConsoleSurface(actionSurface[action]);
    if (stepUpActions.has(action) && !stepUpReceipt?.trim()) return failure('Fresh MFA verification is required.');
    const modules = await createEconomyConsoleModules();
    const stepUp = { stepUpReceipt: stepUpReceipt?.trim() };
    let result: ApiResult;

    switch (action) {
      case 'readiness.inspect':
        result = await modules.admin.postAdminEconomyCapabilitiesReadiness({
          capability: required(values, 'capability') as never,
          subjectReference: optional(values, 'subjectReference'), jurisdictionCode: optional(values, 'jurisdictionCode'),
          riskDecisionId: optional(values, 'riskDecisionId'), operationFingerprint: optional(values, 'operationFingerprint'),
          providerHash: optional(values, 'providerHash'), destinationHash: optional(values, 'destinationHash'),
          sourceRootHashes: optional(values, 'sourceRootHashes')?.split(',').map((value) => value.trim()).filter(Boolean),
        });
        break;
      case 'payout.reserve': result = await modules.admin.postAdminEconomyPayoutRequestsReserve(required(values, 'requestId'), stepUp); break;
      case 'payout.dispatch': result = await modules.admin.postAdminEconomyPayoutRequestsOperationsDispatch(required(values, 'operationId'), { expectedVersion: positiveInteger(values, 'expectedVersion'), ...stepUp }); break;
      case 'payout.reconcile': result = await modules.admin.postAdminEconomyPayoutRequestsOperationsReconcile(required(values, 'operationId')); break;
      case 'risk.approve': result = await modules.risk.postAdminEconomyRiskReviewsApprove(required(values, 'reviewId'), { decisionCode: required(values, 'decisionCode') as never, resolution: required(values, 'resolution') }); break;
      case 'risk.reject': result = await modules.risk.postAdminEconomyRiskReviewsReject(required(values, 'reviewId'), { decisionCode: required(values, 'decisionCode') as never, resolution: required(values, 'resolution') }); break;
      case 'hold.release.propose': result = await modules.holds.postAdminEconomyComplianceHoldsReleaseProposals(required(values, 'holdId'), stepUp); break;
      case 'hold.release.approve': result = await modules.holds.postAdminEconomyComplianceHoldsReleaseApprovals(required(values, 'holdId'), stepUp); break;
      case 'financial-crime.assign': result = await modules.compliance.postAdminEconomyComplianceFinancialCrimeCasesAssignment(required(values, 'caseId'), { expectedVersion: positiveInteger(values, 'expectedVersion') }); break;
      case 'financial-crime.decide': result = await modules.compliance.postAdminEconomyComplianceFinancialCrimeCasesDecisions(required(values, 'caseId'), {
        id: required(values, 'decisionId'), evidenceHash: required(values, 'evidenceHash'), expectedCaseVersion: positiveInteger(values, 'expectedVersion'),
        expiresAt: dateTime(values, 'expiresAt'), outcome: required(values, 'outcome') as never, policyVersion: positiveInteger(values, 'policyVersion'),
        rawObjectReference: optional(values, 'rawObjectReference'), reasonCode: required(values, 'reasonCode'), version: positiveInteger(values, 'decisionVersion'),
      }); break;
      case 'financial-crime.reference': result = await modules.compliance.postAdminEconomyComplianceFinancialCrimeCasesRegulatoryReferences(required(values, 'caseId'), {
        jurisdictionCode: required(values, 'jurisdictionCode'), kind: required(values, 'kind'), referenceHash: required(values, 'referenceHash'),
      }); break;
      case 'trust-safety.assign': result = await modules.compliance.postAdminEconomyComplianceTrustSafetyAppealsAssignment(required(values, 'appealId'), { expectedVersion: positiveInteger(values, 'expectedVersion') }); break;
      case 'trust-safety.decide': result = await modules.compliance.postAdminEconomyComplianceTrustSafetyAppealsDecisions(required(values, 'appealId'), {
        evidenceHash: required(values, 'evidenceHash'), expectedVersion: positiveInteger(values, 'expectedVersion'), overturn: boolean(values, 'overturn'), reasonCode: required(values, 'reasonCode'),
      }); break;
      case 'policy.propose': result = await modules.admin.postAdminEconomyPolicies({
        id: optional(values, 'policyId') ?? randomUUID(), capability: required(values, 'capability') as never,
        effectiveAt: dateTime(values, 'effectiveAt'), expiresAt: dateTime(values, 'expiresAt'), jurisdictionCode: required(values, 'jurisdictionCode'),
        payload: jsonObject(values, 'payload'), providerReady: boolean(values, 'providerReady'), version: positiveInteger(values, 'version'),
      }); break;
      case 'policy.approve': result = await modules.admin.postAdminEconomyPoliciesApprove(required(values, 'policyId'), stepUp); break;
      case 'custody.record': result = await modules.admin.postAdminEconomyCustodyObservations({
        id: optional(values, 'observationId') ?? randomUUID(), assetKey: required(values, 'assetKey'), eligibleUsdNanos: nonNegativeInteger(values, 'eligibleUsdNanos'),
        expiresAt: dateTime(values, 'expiresAt'), keyId: required(values, 'keyId'), observedAt: dateTime(values, 'observedAt'), payloadHash: required(values, 'payloadHash'),
        provider: required(values, 'provider'), purpose: required(values, 'purpose') as never, signature: required(values, 'signature'), version: positiveInteger(values, 'version'),
      }); break;
      case 'reserve.propose': result = await modules.admin.postAdminEconomyReservesProposals({
        id: optional(values, 'proposalId') ?? randomUUID(), authorizationEpoch: positiveInteger(values, 'authorizationEpoch'),
        buffers: jsonObject(values, 'buffers') as never, custodyObservationIds: (optionalJsonArray(values, 'custodyObservationIds') as string[] | undefined),
        expectedActiveVersion: optionalInteger(values, 'expectedActiveVersion'), expiresAt: dateTime(values, 'expiresAt'),
        irreversibleInFlightProviderCostUsdNanos: nonNegativeInteger(values, 'irreversibleInFlightProviderCostUsdNanos'), observedAt: dateTime(values, 'observedAt'),
        policyVersion: positiveInteger(values, 'policyVersion'), services: optionalJsonArray(values, 'services') as never, version: positiveInteger(values, 'version'),
      }); break;
      case 'reserve.approve': result = await modules.admin.postAdminEconomyReservesProposalsApprove(required(values, 'proposalId'), stepUp); break;
      case 'ledger.verify': result = await modules.admin.postAdminEconomyLedgerVerificationRuns(); break;
      case 'anchor.publish': result = await modules.admin.postAdminEconomyLedgerAnchors({ dispatchSnapshotHash: optional(values, 'dispatchSnapshotHash') }); break;
      case 'anchor.verify': result = await modules.admin.postAdminEconomyLedgerAnchorsVerificationRuns(); break;
      case 'projection.rebuild': result = await modules.admin.postAdminEconomyLedgerProjectionGenerations(); break;
      case 'projection.approve': result = await modules.admin.postAdminEconomyLedgerProjectionGenerationsApprovals(positiveInteger(values, 'generation'), stepUp); break;
      case 'kill-switch.activate': result = await modules.admin.postAdminEconomyKillSwitches({ id: optional(values, 'killSwitchId') ?? randomUUID(), capability: optional(values, 'capability') as never, reason: required(values, 'reason') }); break;
      case 'kill-switch.release.propose': result = await modules.admin.postAdminEconomyKillSwitchesReleaseProposals(required(values, 'killSwitchId'), stepUp); break;
      case 'kill-switch.release.approve': result = await modules.admin.postAdminEconomyKillSwitchesReleaseApprovals(required(values, 'killSwitchId'), stepUp); break;
      case 'kill-switch.release.execute': result = await modules.admin.postAdminEconomyKillSwitchesRelease(required(values, 'killSwitchId')); break;
      case 'ad-reward.report.import': result = await modules.admin.postAdminEconomyAdRewardsReports({
        actualRevenueUsdNanos: nonNegativeInteger(values, 'actualRevenueUsdNanos'), batchId: optional(values, 'batchId'), evidenceHash: required(values, 'evidenceHash'),
        importedAt: dateTime(values, 'importedAt'), network: required(values, 'network'), periodEnd: dateTime(values, 'periodEnd'), periodStart: dateTime(values, 'periodStart'),
        reportId: required(values, 'reportId'), signature: required(values, 'signature'), verifiedSessionIds: optionalJsonArray(values, 'verifiedSessionIds') as string[] | undefined,
        version: positiveInteger(values, 'version'),
      }); break;
      case 'marketplace.refund': result = await modules.admin.postAdminEconomyMarketplaceSettlementsRefund(required(values, 'settlementId'), {
        idempotencyKey: required(values, 'idempotencyKey'), quantity: positiveInteger(values, 'quantity'), reasonCode: required(values, 'reasonCode'),
      }); break;
      case 'treasury.propose': result = await modules.treasury.postAdminEconomyTreasuryWithdrawals({
        amountUnits: positiveInteger(values, 'amountUnits'), destinationHash: required(values, 'destinationHash'), idempotencyKey: required(values, 'idempotencyKey'),
        periodStart: required(values, 'periodStart'), ...stepUp,
      }); break;
      case 'treasury.approve': result = await modules.treasury.postAdminEconomyTreasuryWithdrawalsApprove(required(values, 'runId'), { expectedVersion: positiveInteger(values, 'expectedVersion'), ...stepUp }); break;
      case 'treasury.dispatch': result = await modules.treasury.postAdminEconomyTreasuryWithdrawalsDispatch(required(values, 'runId'), { expectedVersion: positiveInteger(values, 'expectedVersion'), ...stepUp }); break;
      case 'treasury.reconcile': result = await modules.treasury.postAdminEconomyTreasuryWithdrawalsReconcile(required(values, 'runId')); break;
      case 'legacy.capture': result = await modules.legacy.postAdminEconomyLegacyMigrationBatches({ batchId: optional(values, 'batchId') ?? randomUUID(), jurisdictionCode: required(values, 'jurisdictionCode') }); break;
      case 'legacy.backfill': result = await modules.legacy.postAdminEconomyLegacyMigrationBatchesWalletsBackfill(required(values, 'batchId'), {
        legacyWalletId: required(values, 'legacyWalletId'), operationFingerprint: required(values, 'operationFingerprint'), riskDecisionId: required(values, 'riskDecisionId'),
      }); break;
      case 'legacy.reconcile': result = await modules.legacy.postAdminEconomyLegacyMigrationBatchesReconcile(required(values, 'batchId')); break;
      case 'legacy.cutover.propose': result = await modules.legacy.postAdminEconomyLegacyMigrationBatchesCutoverPropose(required(values, 'batchId'), { reason: required(values, 'reason'), ...stepUp }); break;
      case 'legacy.cutover.approve': result = await modules.legacy.postAdminEconomyLegacyMigrationBatchesCutoverApprove(required(values, 'batchId'), stepUp); break;
      case 'legacy.cutover.rollback': result = await modules.legacy.postAdminEconomyLegacyMigrationBatchesCutoverRollback(required(values, 'batchId'), { reason: required(values, 'reason'), ...stepUp }); break;
    }

    const response = actionSucceeded(result, 'Operation accepted and recorded durably.');
    if (response.success) revalidatePath('/', 'layout');
    return response;
  } catch (error) {
    return failure(error instanceof Error ? error.message : 'The operation was not accepted.');
  }
}
