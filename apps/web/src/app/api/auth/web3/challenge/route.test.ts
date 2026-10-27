import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  verifyTurnstileToken: vi.fn(),
  forwardedIpHeaders: vi.fn(),
  fetch: vi.fn(),
}));

vi.mock('@/lib/antibot/turnstile', () => ({ verifyTurnstileToken: mocks.verifyTurnstileToken }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: mocks.forwardedIpHeaders }));

import { POST } from './route';

const WALLET = '0x' + 'a'.repeat(40);

function jsonRequest(body: unknown): Request {
  return new Request('http://localhost/api/auth/web3/challenge', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

describe('web3 challenge proxy turnstile gate', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', mocks.fetch);
    vi.stubEnv('API_URL', 'http://localhost:8080');
    mocks.verifyTurnstileToken.mockResolvedValue({ success: true });
    mocks.forwardedIpHeaders.mockResolvedValue({ 'X-Forwarded-For': '203.0.113.7' });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it('returns 403 bot-check-failed before proxying when verification fails', async () => {
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'verify-failed' });

    const response = await POST(jsonRequest({ walletAddress: WALLET, turnstileToken: 'stale' }));

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: 'bot-check-failed' });
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith('stale');
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('fails closed on a missing token before proxying', async () => {
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'missing-token' });

    const response = await POST(jsonRequest({ walletAddress: WALLET }));

    expect(response.status).toBe(403);
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('proxies a valid challenge with the token stripped and the client IP forwarded', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ challenge: 'c', nonce: 'n' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const response = await POST(jsonRequest({ walletAddress: WALLET, chainId: '1', turnstileToken: 'fresh' }));

    expect(response.status).toBe(200);
    expect(mocks.fetch).toHaveBeenCalledTimes(1);
    const [, init] = mocks.fetch.mock.calls[0];
    const body = JSON.parse(init.body as string);
    expect(body).toEqual({ walletAddress: WALLET, chainId: '1' });
    expect(body).not.toHaveProperty('turnstileToken');
    expect(init.headers['X-Forwarded-For']).toBe('203.0.113.7');
    await expect(response.json()).resolves.toEqual({ challenge: 'c', nonce: 'n' });
  });
});
