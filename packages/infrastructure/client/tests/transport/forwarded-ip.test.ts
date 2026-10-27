/**
 * Forwarded IP Header Interceptor Tests
 */

import { describe, it, expect, vi } from 'vitest';
import { resolveForwardedIpHeader, createForwardedIpHeaderInterceptor } from '../../src/runtime/transport/forwarded-ip.js';
import { createServerClient } from '../../src/server.js';

function getterFrom(headers: Record<string, string>): (name: string) => string | null {
  return (name) => headers[name] ?? null;
}

describe('resolveForwardedIpHeader', () => {
  it('returns X-Forwarded-For when cf-connecting-ip and cf-ray are present', () => {
    expect(resolveForwardedIpHeader(getterFrom({ 'cf-connecting-ip': '203.0.113.7', 'cf-ray': '9f3a-1' }))).toEqual({
      'X-Forwarded-For': '203.0.113.7',
    });
  });

  it('trims the cf-connecting-ip value', () => {
    expect(
      resolveForwardedIpHeader(getterFrom({ 'cf-connecting-ip': '  203.0.113.7  ', 'cf-ray': '9f3a-1' })),
    ).toEqual({
      'X-Forwarded-For': '203.0.113.7',
    });
  });

  it('returns {} when cf-connecting-ip is absent, ignoring client-controlled x-forwarded-for', () => {
    const get = getterFrom({ 'x-forwarded-for': '203.0.113.7, 10.0.0.1', 'cf-ray': '9f3a-1' });
    expect(resolveForwardedIpHeader(get)).toEqual({});
  });

  it('returns {} when cf-connecting-ip is present but cf-ray is missing (direct-origin spoof)', () => {
    const get = getterFrom({ 'cf-connecting-ip': '203.0.113.7' });
    expect(resolveForwardedIpHeader(get)).toEqual({});
  });

  it('returns {} for a whitespace-only cf-connecting-ip', () => {
    expect(resolveForwardedIpHeader(getterFrom({ 'cf-connecting-ip': '   ', 'cf-ray': '9f3a-1' }))).toEqual({});
  });

  it('returns {} when the getter has no headers at all', () => {
    expect(resolveForwardedIpHeader(() => null)).toEqual({});
  });
});

describe('createForwardedIpHeaderInterceptor', () => {
  it('resolves to no headers and does not throw outside a Next.js request scope', async () => {
    // Plain node/vitest context: no Next mock, no request scope. The dynamic
    // next/headers import either fails or headers() rejects — both must yield {}.
    const interceptor = createForwardedIpHeaderInterceptor();
    const config = { method: 'GET' as const, path: '/test', headers: {} };

    const result = await interceptor.onRequest!(config);

    expect(result.headers).toEqual({});
  });

  it('keeps a bare createServerClient request working with the interceptor registered', async () => {
    const fetchMock = vi.fn(
      async () =>
        new Response(JSON.stringify({ message: 'success' }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetchMock);

    try {
      const client = createServerClient({ baseUrl: 'http://localhost:5295' });
      const result = await client.request({ method: 'GET', path: '/test' });

      expect(result.ok).toBe(true);
      if (result.ok) {
        expect(result.data).toEqual({ message: 'success' });
      }
      expect(fetchMock).toHaveBeenCalledTimes(1);
    } finally {
      vi.unstubAllGlobals();
    }
  });
});
