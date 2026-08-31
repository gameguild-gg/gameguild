import { describe, expect, it } from 'vitest';
import { formatPlaybackDuration } from './economy-ad-rewards-workspace';

describe('formatPlaybackDuration', () => {
  it.each([
    [0, '00:00:01'],
    [30, '00:00:30'],
    [90, '00:01:30'],
    [3661.9, '01:01:01'],
    [Number.NaN, '00:00:01'],
  ])('formats %s as a valid TimeSpan', (seconds, expected) => {
    expect(formatPlaybackDuration(seconds)).toBe(expected);
  });
});
