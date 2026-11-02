import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  verifyTurnstileToken: vi.fn(),
  forwardedIpHeaders: vi.fn(),
}));

vi.mock('@/lib/antibot/turnstile', () => ({ verifyTurnstileToken: mocks.verifyTurnstileToken }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: mocks.forwardedIpHeaders }));

import { submitContactLeadAction } from './actions';

const API_URL = 'http://localhost:8080';

function fetchResponding(status: number, body: unknown = {}) {
  return vi.fn().mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  );
}

const validInput = {
  name: 'Jane Doe',
  email: 'jane@example.com',
  topic: 'sales',
  message: 'I would like to know more about your enterprise plans.',
};

describe('submitContactLeadAction Turnstile gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', API_URL);
    mocks.verifyTurnstileToken.mockResolvedValue({ success: true });
    mocks.forwardedIpHeaders.mockResolvedValue({ 'X-Forwarded-For': '203.0.113.7' });
  });

  it('returns typed failure and does NOT call the API when verification fails', async () => {
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'verify-failed' });
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    const result = await submitContactLeadAction({ ...validInput, turnstileToken: 'bad-token' });

    expect(result).toEqual({ success: false, error: 'Bot check failed. Please refresh and try again.' });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('proceeds to the API when verification succeeds and forwards XFF but not the token', async () => {
    const fetchMock = fetchResponding(200, {});
    vi.stubGlobal('fetch', fetchMock);

    const result = await submitContactLeadAction({ ...validInput, turnstileToken: 'tok-789' });

    expect(result.success).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe(`${API_URL}/v1/marketing/leads`);
    expect(init.headers['X-Forwarded-For']).toBe('203.0.113.7');
    const body = JSON.parse(init.body);
    expect(body.turnstileToken).toBeUndefined();
    expect(body.name).toBe('Jane Doe');
  });

  it('passes when no token is provided while verification is disabled (site/secret unset)', async () => {
    const fetchMock = fetchResponding(200, {});
    vi.stubGlobal('fetch', fetchMock);

    const result = await submitContactLeadAction(validInput);

    expect(result.success).toBe(true);
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
  });
});