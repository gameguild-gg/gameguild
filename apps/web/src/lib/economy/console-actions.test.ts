import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => {
  const api = vi.fn(async () => ({ ok: true, data: { id: 'result' } }));
  const module = () => new Proxy({}, { get: (_target, property) => (...args: unknown[]) => api(String(property), args) });
  return {
    api,
    modules: { admin: module(), authStepUp: module(), compliance: module(), holds: module(), legacy: module(), risk: module(), treasury: module() },
    requireSurface: vi.fn(async () => ({})),
    revalidatePath: vi.fn(),
  };
});

vi.mock('./console', () => ({
  createEconomyConsoleModules: vi.fn(async () => mocks.modules),
  requireEconomyConsoleSurface: mocks.requireSurface,
}));
vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

import {
  beginEconomyConsoleStepUpAction,
  executeEconomyConsoleAction,
  verifyEconomyConsoleStepUpAction,
  type EconomyConsoleAction,
} from './console-actions';

const uuid = '11111111-2222-3333-4444-555555555555';
const values: Record<string, unknown> = {
  capability: 'PayoutExecution', subjectReference: 'subject', jurisdictionCode: 'BR', riskDecisionId: uuid,
  operationFingerprint: 'fingerprint', providerHash: 'provider', destinationHash: 'destination', sourceRootHashes: 'one, two',
  requestId: uuid, operationId: uuid, expectedVersion: 2, reviewId: uuid, decisionCode: 'RiskAccepted', resolution: 'reviewed',
  holdId: uuid, caseId: uuid, decisionId: uuid, evidenceHash: 'evidence', expiresAt: '2026-09-01T00:00:00Z',
  outcome: 'Approved', policyVersion: 1, reasonCode: 'reason', decisionVersion: 1, rawObjectReference: 'object',
  kind: 'SAR', referenceHash: 'reference', appealId: uuid, overturn: true, policyId: uuid, version: 1,
  effectiveAt: '2026-08-31T00:00:00Z', payload: '{"limit":1}', providerReady: true,
  observationId: uuid, assetKey: 'usd', eligibleUsdNanos: 0, observedAt: '2026-08-30T00:00:00Z', provider: 'bank',
  purpose: 'HardCoin', payloadHash: 'payload', keyId: 'key', signature: 'signature', proposalId: uuid,
  expectedActiveVersion: 1, authorizationEpoch: 1, buffers: '{}', services: '[]', custodyObservationIds: '[]',
  irreversibleInFlightProviderCostUsdNanos: 0, dispatchSnapshotHash: 'snapshot', generation: 2, killSwitchId: uuid,
  reason: 'required', reportId: 'report', batchId: uuid, network: 'google-ad-manager', periodStart: '2026-08-01',
  periodEnd: '2026-08-31T23:59:59Z', importedAt: '2026-08-31T23:59:59Z', actualRevenueUsdNanos: 0,
  verifiedSessionIds: '[]', settlementId: uuid, quantity: 1, idempotencyKey: 'idempotency', amountUnits: 10,
  runId: uuid, legacyWalletId: uuid,
};

const actionMethods: Array<[EconomyConsoleAction, string, boolean?]> = [
  ['readiness.inspect', 'postAdminEconomyCapabilitiesReadiness'],
  ['payout.reserve', 'postAdminEconomyPayoutRequestsReserve', true],
  ['payout.dispatch', 'postAdminEconomyPayoutRequestsOperationsDispatch', true],
  ['payout.reconcile', 'postAdminEconomyPayoutRequestsOperationsReconcile'],
  ['risk.approve', 'postAdminEconomyRiskReviewsApprove'], ['risk.reject', 'postAdminEconomyRiskReviewsReject'],
  ['hold.release.propose', 'postAdminEconomyComplianceHoldsReleaseProposals', true], ['hold.release.approve', 'postAdminEconomyComplianceHoldsReleaseApprovals', true],
  ['financial-crime.assign', 'postAdminEconomyComplianceFinancialCrimeCasesAssignment'],
  ['financial-crime.decide', 'postAdminEconomyComplianceFinancialCrimeCasesDecisions'],
  ['financial-crime.reference', 'postAdminEconomyComplianceFinancialCrimeCasesRegulatoryReferences'],
  ['trust-safety.assign', 'postAdminEconomyComplianceTrustSafetyAppealsAssignment'], ['trust-safety.decide', 'postAdminEconomyComplianceTrustSafetyAppealsDecisions'],
  ['policy.propose', 'postAdminEconomyPolicies'], ['policy.approve', 'postAdminEconomyPoliciesApprove', true],
  ['custody.record', 'postAdminEconomyCustodyObservations'], ['reserve.propose', 'postAdminEconomyReservesProposals'], ['reserve.approve', 'postAdminEconomyReservesProposalsApprove', true],
  ['ledger.verify', 'postAdminEconomyLedgerVerificationRuns'], ['anchor.publish', 'postAdminEconomyLedgerAnchors'], ['anchor.verify', 'postAdminEconomyLedgerAnchorsVerificationRuns'],
  ['projection.rebuild', 'postAdminEconomyLedgerProjectionGenerations'], ['projection.approve', 'postAdminEconomyLedgerProjectionGenerationsApprovals', true],
  ['kill-switch.activate', 'postAdminEconomyKillSwitches'], ['kill-switch.release.propose', 'postAdminEconomyKillSwitchesReleaseProposals', true],
  ['kill-switch.release.approve', 'postAdminEconomyKillSwitchesReleaseApprovals', true], ['kill-switch.release.execute', 'postAdminEconomyKillSwitchesRelease'],
  ['ad-reward.report.import', 'postAdminEconomyAdRewardsReports'], ['marketplace.refund', 'postAdminEconomyMarketplaceSettlementsRefund'],
  ['treasury.propose', 'postAdminEconomyTreasuryWithdrawals', true], ['treasury.approve', 'postAdminEconomyTreasuryWithdrawalsApprove', true],
  ['treasury.dispatch', 'postAdminEconomyTreasuryWithdrawalsDispatch', true], ['treasury.reconcile', 'postAdminEconomyTreasuryWithdrawalsReconcile'],
  ['legacy.capture', 'postAdminEconomyLegacyMigrationBatches'], ['legacy.backfill', 'postAdminEconomyLegacyMigrationBatchesWalletsBackfill'],
  ['legacy.reconcile', 'postAdminEconomyLegacyMigrationBatchesReconcile'], ['legacy.cutover.propose', 'postAdminEconomyLegacyMigrationBatchesCutoverPropose', true],
  ['legacy.cutover.approve', 'postAdminEconomyLegacyMigrationBatchesCutoverApprove', true], ['legacy.cutover.rollback', 'postAdminEconomyLegacyMigrationBatchesCutoverRollback', true],
];

describe('economy console actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.api.mockResolvedValue({ ok: true, data: { id: 'result' } });
  });

  it.each(actionMethods)('executes %s through %s', async (action, method, protectedAction) => {
    const result = await executeEconomyConsoleAction(action, values, protectedAction ? 'receipt' : undefined);
    expect(result).toEqual({ success: true, message: 'Operation accepted and recorded durably.' });
    expect(mocks.api).toHaveBeenCalledWith(method, expect.any(Array));
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
  });

  it('creates and verifies a challenge without exposing a reusable password hash', async () => {
    mocks.api
      .mockResolvedValueOnce({ ok: true, data: { challengeId: uuid, expiresAt: '2026-08-30T01:00:00Z' } })
      .mockResolvedValueOnce({ ok: true, data: { receipt: 'opaque-single-use', expiresAt: '2026-08-30T01:00:00Z' } });
    const challenge = await beginEconomyConsoleStepUpAction('policy.approve', values);
    expect(challenge.challengeId).toBe(uuid);
    const verified = await verifyEconomyConsoleStepUpAction(uuid, '123456');
    expect(verified.receipt).toBe('opaque-single-use');
    expect(mocks.api).toHaveBeenLastCalledWith('postAuthStepUpChallengesVerify', [uuid, { method: 'Totp', evidence: '123456' }]);
  });

  it('fails closed for missing MFA, invalid challenge input, and rejected API operations', async () => {
    await expect(executeEconomyConsoleAction('policy.approve', values)).resolves.toMatchObject({ success: false, message: 'Fresh MFA verification is required.' });
    await expect(beginEconomyConsoleStepUpAction('readiness.inspect', values)).resolves.toMatchObject({ success: false });
    await expect(verifyEconomyConsoleStepUpAction('', '')).resolves.toMatchObject({ success: false });
    mocks.api.mockResolvedValueOnce({ ok: false, error: { message: 'policy rejected' } });
    await expect(executeEconomyConsoleAction('policy.propose', values)).resolves.toEqual({ success: false, message: 'policy rejected' });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('reports malformed structured, numeric, and datetime fields safely', async () => {
    await expect(executeEconomyConsoleAction('policy.propose', { ...values, payload: '[]' })).resolves.toMatchObject({ success: false, message: 'payload must be a JSON object.' });
    await expect(executeEconomyConsoleAction('reserve.propose', { ...values, services: '{}' })).resolves.toMatchObject({ success: false, message: 'services must be a JSON array.' });
    await expect(executeEconomyConsoleAction('marketplace.refund', { ...values, quantity: 0 })).resolves.toMatchObject({ success: false, message: 'quantity must be a positive integer.' });
    await expect(executeEconomyConsoleAction('custody.record', { ...values, eligibleUsdNanos: -1 })).resolves.toMatchObject({ success: false, message: 'eligibleUsdNanos must be a non-negative integer.' });
    await expect(executeEconomyConsoleAction('policy.propose', { ...values, effectiveAt: 'invalid' })).resolves.toMatchObject({ success: false, message: 'effectiveAt must be a valid date and time.' });
  });

  it('handles provider responses that omit challenge or receipt and thrown errors', async () => {
    mocks.api.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(beginEconomyConsoleStepUpAction('policy.approve', values)).resolves.toMatchObject({ success: false });
    mocks.api.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(verifyEconomyConsoleStepUpAction(uuid, '123456')).resolves.toMatchObject({ success: false });
    mocks.api.mockRejectedValueOnce(new Error('network unavailable'));
    await expect(executeEconomyConsoleAction('ledger.verify', values)).resolves.toEqual({ success: false, message: 'network unavailable' });
  });

  it('normalizes missing optional values, generated identifiers, booleans, and empty arrays', async () => {
    await expect(executeEconomyConsoleAction('readiness.inspect', { capability: 'PayoutExecution' })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('trust-safety.decide', { ...values, overturn: 'true' })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('trust-safety.decide', { ...values, overturn: 'on' })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('trust-safety.decide', { ...values, overturn: false })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('policy.propose', { ...values, policyId: '', providerReady: 'on' })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('custody.record', { ...values, observationId: '' })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('reserve.propose', {
      ...values, proposalId: '', expectedActiveVersion: '', services: '', custodyObservationIds: '',
    })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('kill-switch.activate', { ...values, killSwitchId: '', capability: '' })).resolves.toMatchObject({ success: true });
    await expect(executeEconomyConsoleAction('legacy.capture', { ...values, batchId: '' })).resolves.toMatchObject({ success: true });
  });

  it('rejects missing, unsafe, and structurally invalid values through client-safe messages', async () => {
    await expect(executeEconomyConsoleAction('risk.approve', {})).resolves.toMatchObject({ success: false, message: 'reviewId is required.' });
    await expect(executeEconomyConsoleAction('marketplace.refund', { ...values, quantity: '1.5' })).resolves.toMatchObject({ success: false, message: 'quantity must be a positive integer.' });
    await expect(executeEconomyConsoleAction('custody.record', { ...values, eligibleUsdNanos: 'invalid' })).resolves.toMatchObject({ success: false, message: 'eligibleUsdNanos must be a non-negative integer.' });
    await expect(executeEconomyConsoleAction('policy.propose', { ...values, payload: 'null' })).resolves.toMatchObject({ success: false, message: 'payload must be a JSON object.' });
    await expect(executeEconomyConsoleAction('policy.propose', { ...values, payload: '"text"' })).resolves.toMatchObject({ success: false, message: 'payload must be a JSON object.' });
    await expect(executeEconomyConsoleAction('reserve.propose', { ...values, services: 'null' })).resolves.toMatchObject({ success: false, message: 'services must be a JSON array.' });
  });

  it('normalizes provider rejection variants and unknown thrown values', async () => {
    mocks.api.mockResolvedValueOnce({ ok: false, error: { message: 'challenge rejected' } });
    await expect(beginEconomyConsoleStepUpAction('policy.approve', values)).resolves.toEqual({ success: false, message: 'challenge rejected' });
    mocks.api.mockResolvedValueOnce({ ok: false, error: { message: null } });
    await expect(beginEconomyConsoleStepUpAction('policy.approve', values)).resolves.toEqual({ success: false, message: 'The MFA challenge was not created.' });
    mocks.api.mockRejectedValueOnce('offline');
    await expect(beginEconomyConsoleStepUpAction('policy.approve', values)).resolves.toEqual({ success: false, message: 'The MFA challenge was not created.' });
    mocks.api.mockRejectedValueOnce(new Error('challenge network error'));
    await expect(beginEconomyConsoleStepUpAction('policy.approve', values)).resolves.toEqual({ success: false, message: 'challenge network error' });

    mocks.api.mockResolvedValueOnce({ ok: false, error: { message: 'verification rejected' } });
    await expect(verifyEconomyConsoleStepUpAction(uuid, '123456')).resolves.toEqual({ success: false, message: 'verification rejected' });
    mocks.api.mockResolvedValueOnce({ ok: false, error: { message: null } });
    await expect(verifyEconomyConsoleStepUpAction(uuid, '123456')).resolves.toEqual({ success: false, message: 'MFA verification failed.' });
    mocks.api.mockRejectedValueOnce('offline');
    await expect(verifyEconomyConsoleStepUpAction(uuid, '123456')).resolves.toEqual({ success: false, message: 'MFA verification failed.' });
    mocks.api.mockRejectedValueOnce(new Error('verification network error'));
    await expect(verifyEconomyConsoleStepUpAction(uuid, '123456')).resolves.toEqual({ success: false, message: 'verification network error' });

    mocks.api.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(executeEconomyConsoleAction('ledger.verify', values)).resolves.toEqual({ success: false, message: 'The operation was not accepted.' });
    mocks.requireSurface.mockRejectedValueOnce('forbidden');
    await expect(executeEconomyConsoleAction('ledger.verify', values)).resolves.toEqual({ success: false, message: 'The operation was not accepted.' });
  });
});
