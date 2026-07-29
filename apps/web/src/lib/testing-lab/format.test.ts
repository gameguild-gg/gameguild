import { describe, expect, it } from 'vitest';
import { formatTestingEventStatus } from './format';

describe('formatTestingEventStatus', () => {
  it.each([
    [undefined, 'Draft'],
    [null, 'Draft'],
    ['ApplicationsOpen', 'Applications Open'],
    ['ManagerOnly', 'Manager Only'],
  ])('formats %s as %s', (value, expected) => {
    expect(formatTestingEventStatus(value)).toBe(expected);
  });
});
