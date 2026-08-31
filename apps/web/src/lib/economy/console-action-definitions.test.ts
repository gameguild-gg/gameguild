import { describe, expect, it } from 'vitest';
import { economyConsoleActionDefinitions } from './console-action-definitions';

describe('economyConsoleActionDefinitions', () => {
  it('exposes real commands for every operational surface that has an administrative mutation', () => {
    const actionNames = Object.values(economyConsoleActionDefinitions).flatMap((definitions) => definitions?.map((item) => item.action) ?? []);
    expect(actionNames).toContain('readiness.inspect');
    expect(actionNames).toContain('payout.dispatch');
    expect(actionNames).toContain('financial-crime.decide');
    expect(actionNames).toContain('trust-safety.decide');
    expect(actionNames).toContain('policy.approve');
    expect(actionNames).toContain('reserve.approve');
    expect(actionNames).toContain('anchor.verify');
    expect(actionNames).toContain('kill-switch.release.execute');
    expect(actionNames).toContain('ad-reward.report.import');
    expect(actionNames).toContain('marketplace.refund');
    expect(actionNames).toContain('treasury.dispatch');
    expect(actionNames).toContain('legacy.cutover.rollback');
    expect(new Set(actionNames).size).toBe(actionNames.length);
  });

  it('marks every receipt-protected command as step-up', () => {
    const protectedActions = Object.values(economyConsoleActionDefinitions)
      .flatMap((definitions) => definitions ?? [])
      .filter((item) => item.stepUp)
      .map((item) => item.action);
    expect(protectedActions).toEqual(expect.arrayContaining([
      'payout.reserve', 'payout.dispatch', 'policy.approve', 'reserve.approve', 'projection.approve',
      'kill-switch.release.propose', 'kill-switch.release.approve', 'treasury.propose', 'treasury.approve',
      'treasury.dispatch', 'legacy.cutover.propose', 'legacy.cutover.approve', 'legacy.cutover.rollback',
    ]));
  });
});

