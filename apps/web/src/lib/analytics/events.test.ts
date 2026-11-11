import { describe, expect, it } from 'vitest';
import {
  ANALYTICS_EVENT_CONTRACT_VERSION,
  EVENT_VERSION_PARAM,
  PAGE_LOAD_TIMING_EVENT,
  PAGE_VIEW_EVENT,
  WEB_VITALS_EVENT,
  withContractVersion,
} from './events';

describe('analytics event contract (issue #1221)', () => {
  it('exposes the documented event name constants', () => {
    expect(WEB_VITALS_EVENT).toBe('web_vitals');
    expect(PAGE_LOAD_TIMING_EVENT).toBe('page_load_timing');
    expect(PAGE_VIEW_EVENT).toBe('page_view');
  });

  it('keeps event names within GA4/Firebase limits', () => {
    for (const name of [WEB_VITALS_EVENT, PAGE_LOAD_TIMING_EVENT, PAGE_VIEW_EVENT]) {
      expect(name.length).toBeLessThanOrEqual(40);
      expect(name).toMatch(/^[A-Za-z0-9_]+$/);
    }
  });

  it('stamps event_version onto payloads without mutating the input', () => {
    const input = { metric_name: 'LCP', metric_value: 2451 };
    const stamped = withContractVersion(input);
    expect(stamped).toEqual({ ...input, [EVENT_VERSION_PARAM]: ANALYTICS_EVENT_CONTRACT_VERSION });
    expect(input).not.toHaveProperty(EVENT_VERSION_PARAM);
  });
});
