import { NextRequest } from 'next/server';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ fetch: vi.fn() }));

import { GET, POST } from './route';

const guestSessionPayload = {
  guestSessionId: '11111111-2222-3333-4444-555555555555',
  tokenType: 'Bearer',
  accessToken: 'guest-access-token',
  expiresAt: '2026-10-22T12:30:00Z',
  expiresIn: 1800,
  permissions: ['guest:browse-catalog', 'guest:read-published-content'],
};

describe('guest-session proxy', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', mocks.fetch);
    vi.stubEnv('API_URL', 'http://localhost:8080');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  describe('POST issuance', () => {
    it('forwards an empty body to the upstream guest-session endpoint', async () => {
      mocks.fetch.mockResolvedValue(
        new Response(JSON.stringify(guestSessionPayload), { status: 200 }),
      );

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: '',
        }),
      );

      expect(response.status).toBe(200);
      expect(mocks.fetch).toHaveBeenCalledWith(
        'http://localhost:8080/v1/auth/guest-session',
        expect.objectContaining({
          method: 'POST',
          cache: 'no-store',
          body: JSON.stringify({}),
        }),
      );
      expect(response.headers.get('cache-control')).toBe('no-store');
      await expect(response.json()).resolves.toEqual({
        guestSessionId: guestSessionPayload.guestSessionId,
        accessToken: 'guest-access-token',
        expiresAt: guestSessionPayload.expiresAt,
        permissions: guestSessionPayload.permissions,
      });
    });

    it('forwards a trimmed device fingerprint', async () => {
      mocks.fetch.mockResolvedValue(
        new Response(JSON.stringify(guestSessionPayload), { status: 200 }),
      );

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: JSON.stringify({ deviceFingerprint: ' browser-fp-1 ' }),
        }),
      );

      expect(response.status).toBe(200);
      expect(mocks.fetch).toHaveBeenCalledWith(
        'http://localhost:8080/v1/auth/guest-session',
        expect.objectContaining({
          body: JSON.stringify({ deviceFingerprint: 'browser-fp-1' }),
        }),
      );
    });

    it('drops a non-string device fingerprint', async () => {
      mocks.fetch.mockResolvedValue(
        new Response(JSON.stringify(guestSessionPayload), { status: 200 }),
      );

      await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: JSON.stringify({ deviceFingerprint: 42 }),
        }),
      );

      expect(mocks.fetch).toHaveBeenCalledWith(
        'http://localhost:8080/v1/auth/guest-session',
        expect.objectContaining({ body: JSON.stringify({}) }),
      );
    });

    it('maps an upstream 403 to the disabled-policy error', async () => {
      mocks.fetch.mockResolvedValue(new Response('', { status: 403 }));

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: '',
        }),
      );

      expect(response.status).toBe(403);
      await expect(response.json()).resolves.toEqual({
        error: 'guest_access_disabled',
      });
    });

    it('maps an upstream 429 rate-limit rejection through', async () => {
      mocks.fetch.mockResolvedValue(new Response('', { status: 429 }));

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: '',
        }),
      );

      expect(response.status).toBe(429);
    });

    it('collapses an upstream 5xx into 502', async () => {
      mocks.fetch.mockResolvedValue(new Response('boom', { status: 500 }));

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: '',
        }),
      );

      expect(response.status).toBe(502);
    });

    it('returns 503 when the API host is unreachable', async () => {
      mocks.fetch.mockRejectedValue(new Error('network down'));

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: '',
        }),
      );

      expect(response.status).toBe(503);
      await expect(response.json()).resolves.toEqual({
        error: 'guest_service_unavailable',
      });
    });

    it('rejects an upstream 200 with a malformed payload', async () => {
      mocks.fetch.mockResolvedValue(
        new Response(JSON.stringify({ guestSessionId: 'x' }), { status: 200 }),
      );

      const response = await POST(
        new NextRequest('http://localhost/api/auth/guest-session', {
          method: 'POST',
          body: '',
        }),
      );

      expect(response.status).toBe(502);
      await expect(response.json()).resolves.toEqual({
        error: 'invalid_guest_session_response',
      });
    });
  });

  describe('GET introspection', () => {
    it('forwards the guest bearer credential upstream', async () => {
      mocks.fetch.mockResolvedValue(
        new Response(
          JSON.stringify({
            guestSessionId: guestSessionPayload.guestSessionId,
            role: 'guest',
            permissions: guestSessionPayload.permissions,
          }),
          { status: 200 },
        ),
      );

      const request = new NextRequest('http://localhost/api/auth/guest-session', {
        headers: { 'x-guest-token': 'guest-access-token' },
      });
      const response = await GET(request);

      expect(response.status).toBe(200);
      expect(mocks.fetch).toHaveBeenCalledWith(
        'http://localhost:8080/v1/auth/guest-session',
        expect.objectContaining({
          cache: 'no-store',
          headers: { authorization: 'Bearer guest-access-token' },
          signal: request.signal,
        }),
      );
      expect(response.headers.get('cache-control')).toBe('no-store');
    });

    it('rejects callers without a guest token before any upstream call', async () => {
      const response = await GET(
        new NextRequest('http://localhost/api/auth/guest-session'),
      );

      expect(response.status).toBe(401);
      expect(mocks.fetch).not.toHaveBeenCalled();
    });

    it('passes upstream authorization failures through with their status', async () => {
      mocks.fetch.mockResolvedValue(
        new Response(JSON.stringify({ detail: 'expired guest token' }), { status: 401 }),
      );

      const response = await GET(
        new NextRequest('http://localhost/api/auth/guest-session', {
          headers: { 'x-guest-token': 'stale-token' },
        }),
      );

      expect(response.status).toBe(401);
    });
  });
});
