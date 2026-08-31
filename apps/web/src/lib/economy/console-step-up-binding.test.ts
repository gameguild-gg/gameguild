import { describe, expect, it } from 'vitest';
import { buildEconomyStepUpBinding } from './console-step-up-binding';
import type { EconomyConsoleAction } from './console-actions';

const uuid = '11111111-2222-3333-4444-555555555555';

describe('buildEconomyStepUpBinding', () => {
  it.each<[EconomyConsoleAction, Record<string, unknown>, string, string]>([
    ['payout.reserve', { requestId: uuid }, 'economy.payout.reserve', 'payout-request:11111111222233334444555555555555'],
    ['payout.dispatch', { operationId: uuid, expectedVersion: 2 }, 'economy.payout.dispatch', 'payout-operation:11111111222233334444555555555555'],
    ['policy.approve', { policyId: uuid }, 'economy.policy.approve', 'policy:11111111222233334444555555555555'],
    ['reserve.approve', { proposalId: uuid }, 'economy.reserve.approve', 'reserve:11111111222233334444555555555555'],
    ['projection.approve', { generation: 7 }, 'economy.projection.approve', 'projection:7'],
    ['kill-switch.release.propose', { killSwitchId: uuid }, 'economy.kill-switch.release.propose', 'kill-switch:11111111222233334444555555555555'],
    ['kill-switch.release.approve', { killSwitchId: uuid }, 'economy.kill-switch.release.approve', 'kill-switch:11111111222233334444555555555555'],
    ['hold.release.propose', { holdId: uuid }, 'economy.compliance-hold.release.propose', 'compliance-hold:11111111222233334444555555555555'],
    ['hold.release.approve', { holdId: uuid }, 'economy.compliance-hold.release.approve', 'compliance-hold:11111111222233334444555555555555'],
    ['treasury.propose', { periodStart: '2026-08-01', amountUnits: 10, destinationHash: 'ABC', idempotencyKey: 'key' }, 'economy.treasury.propose', 'treasury-period:2026-08-01'],
    ['treasury.approve', { runId: uuid, expectedVersion: 3 }, 'economy.treasury.approve', 'treasury-withdrawal:11111111222233334444555555555555'],
    ['treasury.dispatch', { runId: uuid, expectedVersion: 4 }, 'economy.treasury.dispatch', 'treasury-withdrawal:11111111222233334444555555555555'],
    ['legacy.cutover.propose', { batchId: uuid, reason: 'ready' }, 'economy.legacy-cutover.propose', 'legacy-cutover:11111111222233334444555555555555'],
    ['legacy.cutover.approve', { batchId: uuid }, 'economy.legacy-cutover.approve', 'legacy-cutover:11111111222233334444555555555555'],
    ['legacy.cutover.rollback', { batchId: uuid, reason: 'variance' }, 'economy.legacy-cutover.rollback', 'legacy-cutover:11111111222233334444555555555555'],
  ])('binds %s to the exact operation and target', (action, values, operationType, targetReference) => {
    const binding = buildEconomyStepUpBinding(action, values);
    expect(binding).toMatchObject({ operationType, targetReference });
    expect(binding.payloadHash).toMatch(/^[0-9a-f]{64}$/);
  });

  it('normalizes UUIDs and destination hashes before binding', () => {
    expect(buildEconomyStepUpBinding('policy.approve', { policyId: uuid.toUpperCase() })).toEqual(
      buildEconomyStepUpBinding('policy.approve', { policyId: uuid }),
    );
    expect(buildEconomyStepUpBinding('treasury.propose', { periodStart: '2026-08-01', amountUnits: 10, destinationHash: 'ABC', idempotencyKey: 'key' })).toEqual(
      buildEconomyStepUpBinding('treasury.propose', { periodStart: '2026-08-01', amountUnits: 10, destinationHash: 'abc', idempotencyKey: 'key' }),
    );
    expect(buildEconomyStepUpBinding('legacy.cutover.propose', { batchId: uuid }).payloadHash)
      .toMatch(/^[0-9a-f]{64}$/);
    expect(buildEconomyStepUpBinding('legacy.cutover.rollback', { batchId: uuid, reason: null }).payloadHash)
      .toMatch(/^[0-9a-f]{64}$/);
  });

  it.each([
    ['policy.approve', {}, 'policyId is required.'],
    ['policy.approve', { policyId: 'invalid' }, 'A valid identifier is required.'],
    ['projection.approve', { generation: 0 }, 'generation must be a positive integer.'],
    ['treasury.propose', { periodStart: '2026-08-02', amountUnits: 1, destinationHash: 'a', idempotencyKey: 'b' }, 'periodStart must be the first day of a month.'],
    ['readiness.inspect', {}, 'This action does not require step-up authentication.'],
  ] as const)('rejects invalid binding input for %s', (action, values, message) => {
    expect(() => buildEconomyStepUpBinding(action, values)).toThrow(message);
  });
});
