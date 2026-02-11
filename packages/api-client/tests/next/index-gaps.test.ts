/**
 * Gap coverage for integrations/next/index.ts — lines 180-185
 * Tests createClientFromCookies and createRouteClient closures.
 * Uses a separate file to avoid module-mock conflicts.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

describe('createClientFromCookies — gap coverage (lines 180-185)', () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    vi.resetModules();
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ data: 'ok' }),
      text: async () => '{"data":"ok"}',
    });
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it('should execute getCookies and read cookie names', async () => {
    const { createClientFromCookies } = await import('../../src/integrations/next/index.js');

    const getCookiesMock = {
      get: vi.fn((name: string) => {
        if (name === 'access_token') return { value: 'my-token' };
        if (name === 'tenant_id') return { value: 'my-tenant' };
        return undefined;
      }),
    };

    const client = await createClientFromCookies({
      baseUrl: 'http://localhost:5000',
      getCookies: async () => getCookiesMock,
    });

    expect(client).toBeDefined();
    // Verify the client can make a request (triggers the closure)
    await client.request({ path: '/test', method: 'GET', headers: {} });
    expect(globalThis.fetch).toHaveBeenCalled();
  }, 30000);

  it('should use custom cookie names', async () => {
    const { createClientFromCookies } = await import('../../src/integrations/next/index.js');

    const getCookiesMock = {
      get: vi.fn((name: string) => {
        if (name === 'custom_tok') return { value: 'ct' };
        if (name === 'custom_tenant') return { value: 'ctn' };
        return undefined;
      }),
    };

    const client = await createClientFromCookies({
      baseUrl: 'http://localhost:5000',
      getCookies: async () => getCookiesMock,
      accessTokenCookie: 'custom_tok',
      tenantCookie: 'custom_tenant',
    });

    expect(client).toBeDefined();
    await client.request({ path: '/test', method: 'GET', headers: {} });
  }, 15000);

  it('should handle missing token gracefully', async () => {
    const { createClientFromCookies } = await import('../../src/integrations/next/index.js');

    const getCookiesMock = {
      get: vi.fn(() => undefined),
    };

    const client = await createClientFromCookies({
      baseUrl: 'http://localhost:5000',
      getCookies: async () => getCookiesMock,
    });

    expect(client).toBeDefined();
  }, 15000);
});

describe('createRouteClient — gap coverage (header extraction closures)', () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    vi.resetModules();
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ data: 'ok' }),
      text: async () => '{"data":"ok"}',
    });
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it('should extract Bearer token and tenant from headers and use them in request', async () => {
    const { createRouteClient } = await import('../../src/integrations/next/index.js');

    const headers = new Headers({
      Authorization: 'Bearer route-token-123',
      'X-Tenant-Id': 'route-tenant-456',
    });

    const client = createRouteClient({
      baseUrl: 'http://localhost:5000',
      headers,
    });

    // Make a request to trigger the closure execution
    await client.request({ path: '/api/data', method: 'GET', headers: {} });

    expect(globalThis.fetch).toHaveBeenCalled();
    const [, opts] = (globalThis.fetch as any).mock.calls[0];
    expect(opts.headers.get('Authorization')).toBe('Bearer route-token-123');
    expect(opts.headers.get('X-Tenant-Id')).toBe('route-tenant-456');
  });
});
