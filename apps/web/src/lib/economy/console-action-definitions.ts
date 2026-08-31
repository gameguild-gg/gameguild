import type { EconomyConsoleAction } from './console-actions';
import type { EconomyConsoleSurface } from './console';

export interface EconomyConsoleActionField {
  key: string;
  kind: 'checkbox' | 'date' | 'datetime-local' | 'number' | 'select' | 'textarea' | 'text';
  options: string[];
  required?: boolean;
}

export interface EconomyConsoleActionDefinition {
  action: EconomyConsoleAction;
  fields: EconomyConsoleActionField[];
  stepUp?: boolean;
}

const field = (key: string, kind: EconomyConsoleActionField['kind'] = 'text', required = true, options?: string[]): EconomyConsoleActionField => ({ key, kind, required, options: options ?? [] });
const id = (key: string) => field(key);
const version = field('expectedVersion', 'number');
const stepUp = <T extends EconomyConsoleActionDefinition>(definition: T): T => ({ ...definition, stepUp: true });
const action = (name: EconomyConsoleAction, fields: EconomyConsoleActionField[] = []): EconomyConsoleActionDefinition => ({ action: name, fields });

const capabilities = [
  'ConfirmHardCoinFunding', 'ConvertHardToSoft', 'ReverseProviderFunding', 'Transfer', 'IssueAdReward',
  'BountyEscrow', 'BountyClaim', 'MarketplaceSettlement', 'PayoutExecution', 'AdminWithdrawalExecution',
  'MarketplaceRefund', 'BountyReclaim', 'LegacyBalanceBackfill',
];

export const economyConsoleActionDefinitions: Partial<Record<EconomyConsoleSurface, EconomyConsoleActionDefinition[]>> = {
  readiness: [action('readiness.inspect', [field('capability', 'select', true, capabilities), field('subjectReference'), field('jurisdictionCode'), field('riskDecisionId', 'text', false), field('operationFingerprint', 'text', false), field('providerHash', 'text', false), field('destinationHash', 'text', false), field('sourceRootHashes', 'text', false)])],
  'payout-operations': [
    stepUp(action('payout.reserve', [id('requestId')])),
    stepUp(action('payout.dispatch', [id('operationId'), version])),
    action('payout.reconcile', [id('operationId')]),
  ],
  'risk-reviews': [
    action('risk.approve', [id('reviewId'), field('decisionCode', 'select', true, ['EvidenceVerified', 'RiskAccepted']), field('resolution', 'textarea')]),
    action('risk.reject', [id('reviewId'), field('decisionCode', 'select', true, ['PolicyViolation', 'FraudConfirmed']), field('resolution', 'textarea')]),
    stepUp(action('hold.release.propose', [id('holdId')])),
    stepUp(action('hold.release.approve', [id('holdId')])),
  ],
  'financial-crime': [
    action('financial-crime.assign', [id('caseId'), version]),
    action('financial-crime.decide', [id('caseId'), id('decisionId'), field('evidenceHash'), version, field('expiresAt', 'datetime-local'), field('outcome', 'select', true, ['Approved', 'Rejected', 'NeedsReview', 'Unavailable']), field('policyVersion', 'number'), field('reasonCode'), field('decisionVersion', 'number'), field('rawObjectReference', 'text', false)]),
    action('financial-crime.reference', [id('caseId'), field('jurisdictionCode'), field('kind'), field('referenceHash')]),
  ],
  'trust-safety': [
    action('trust-safety.assign', [id('appealId'), version]),
    action('trust-safety.decide', [id('appealId'), version, field('evidenceHash'), field('reasonCode'), field('overturn', 'checkbox', false)]),
  ],
  policies: [
    action('policy.propose', [field('policyId', 'text', false), field('capability', 'select', true, capabilities), field('jurisdictionCode'), field('version', 'number'), field('effectiveAt', 'datetime-local'), field('expiresAt', 'datetime-local'), field('payload', 'textarea'), field('providerReady', 'checkbox', false)]),
    stepUp(action('policy.approve', [id('policyId')])),
  ],
  reserves: [
    action('custody.record', [field('observationId', 'text', false), field('assetKey'), field('eligibleUsdNanos', 'number'), field('observedAt', 'datetime-local'), field('expiresAt', 'datetime-local'), field('provider'), field('purpose', 'select', true, ['HardCoin', 'SoftCoin']), field('payloadHash'), field('keyId'), field('signature'), field('version', 'number')]),
    action('reserve.propose', [field('proposalId', 'text', false), field('version', 'number'), field('expectedActiveVersion', 'number', false), field('policyVersion', 'number'), field('authorizationEpoch', 'number'), field('observedAt', 'datetime-local'), field('expiresAt', 'datetime-local'), field('buffers', 'textarea'), field('services', 'textarea', false), field('custodyObservationIds', 'textarea', false), field('irreversibleInFlightProviderCostUsdNanos', 'number')]),
    stepUp(action('reserve.approve', [id('proposalId')])),
  ],
  ledger: [
    action('ledger.verify'), action('anchor.publish', [field('dispatchSnapshotHash', 'text', false)]), action('anchor.verify'), action('projection.rebuild'),
    stepUp(action('projection.approve', [field('generation', 'number')])),
  ],
  'kill-switches': [
    action('kill-switch.activate', [field('killSwitchId', 'text', false), field('capability', 'select', false, capabilities), field('reason', 'textarea')]),
    stepUp(action('kill-switch.release.propose', [id('killSwitchId')])),
    stepUp(action('kill-switch.release.approve', [id('killSwitchId')])),
    action('kill-switch.release.execute', [id('killSwitchId')]),
  ],
  'ad-rewards': [action('ad-reward.report.import', [field('reportId'), field('batchId', 'text', false), field('network'), field('version', 'number'), field('periodStart', 'datetime-local'), field('periodEnd', 'datetime-local'), field('importedAt', 'datetime-local'), field('actualRevenueUsdNanos', 'number'), field('verifiedSessionIds', 'textarea', false), field('evidenceHash'), field('signature')])],
  marketplace: [action('marketplace.refund', [id('settlementId'), field('quantity', 'number'), field('reasonCode'), field('idempotencyKey')])],
  treasury: [
    stepUp(action('treasury.propose', [field('periodStart', 'date'), field('amountUnits', 'number'), field('destinationHash'), field('idempotencyKey')])),
    stepUp(action('treasury.approve', [id('runId'), version])),
    stepUp(action('treasury.dispatch', [id('runId'), version])),
    action('treasury.reconcile', [id('runId')]),
  ],
  'legacy-migration': [
    action('legacy.capture', [field('batchId', 'text', false), field('jurisdictionCode')]),
    action('legacy.backfill', [id('batchId'), field('legacyWalletId'), field('operationFingerprint'), field('riskDecisionId')]),
    action('legacy.reconcile', [id('batchId')]),
    stepUp(action('legacy.cutover.propose', [id('batchId'), field('reason', 'textarea')])),
    stepUp(action('legacy.cutover.approve', [id('batchId')])),
    stepUp(action('legacy.cutover.rollback', [id('batchId'), field('reason', 'textarea')])),
  ],
};
