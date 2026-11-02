import { NextRequest } from 'next/server';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  verifyTurnstileToken: vi.fn(),
  forwardedIpHeaders: vi.fn(),
  fetch: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@/lib/antibot/turnstile', () => ({ verifyTurnstileToken: mocks.verifyTurnstileToken }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: mocks.forwardedIpHeaders }));

import { PATCH, POST } from './route';

const CONTENT_ID = '01234567-89ab-1def-9abc-0123456789ab';

function jsonRequest(body: unknown): NextRequest {
  return new NextRequest('http://localhost/api/courses/content-reports', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

describe('content-reports POST turnstile gate', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', mocks.fetch);
    vi.stubEnv('API_URL', 'http://localhost:8080');
    mocks.getToken.mockResolvedValue('session-token');
    mocks.verifyTurnstileToken.mockResolvedValue({ success: true });
    mocks.forwardedIpHeaders.mockResolvedValue({ 'X-Forwarded-For': '203.0.113.7' });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it.each([null, [], 'text', 42])('rejects a non-object JSON payload (%j) before proxying', async (body) => {
    for (const handler of [POST, PATCH]) {
      const response = await handler(jsonRequest(body));
      expect(response.status).toBe(400);
      await expect(response.json()).resolves.toEqual({ error: 'A valid JSON object is required.' });
    }
    expect(mocks.fetch).not.toHaveBeenCalled();
    expect(mocks.verifyTurnstileToken).not.toHaveBeenCalled();
  });

  it('rejects malformed JSON before proxying', async () => {
    for (const handler of [POST, PATCH]) {
      const response = await handler(new NextRequest('http://localhost/api/courses/content-reports', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: '{',
      }));
      expect(response.status).toBe(400);
    }
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('returns 403 bot-check-failed before proxying when verification fails', async () => {
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'verify-failed' });

    const response = await POST(jsonRequest({ contentId: CONTENT_ID, reason: 'spam', turnstileToken: 'stale' }));

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: 'bot-check-failed' });
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith('stale');
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('fails closed on a missing token before proxying', async () => {
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'missing-token' });

    const response = await POST(jsonRequest({ contentId: CONTENT_ID, reason: 'spam' }));

    expect(response.status).toBe(403);
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('proxies a valid report with the token stripped and the client IP forwarded', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'report-1' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const response = await POST(
      jsonRequest({ contentId: CONTENT_ID, reason: 'spam', description: 'ad farm', turnstileToken: 'fresh' }),
    );

    expect(response.status).toBe(200);
    expect(mocks.fetch).toHaveBeenCalledTimes(1);
    const [url, init] = mocks.fetch.mock.calls[0];
    expect(url).toBe(`http://localhost:8080/v1/assets/${CONTENT_ID}:report`);
    const body = JSON.parse(init.body as string);
    expect(body).toEqual({ reason: 3, description: 'ad farm' });
    expect(body).not.toHaveProperty('turnstileToken');
    expect(init.headers['X-Forwarded-For']).toBe('203.0.113.7');
    expect(init.headers.Authorization).toBe('Bearer session-token');
  });
});
