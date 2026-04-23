/**
 * Client tests - Auth validation
 */

import { describe, it, expect, vi } from 'vitest';
import { createClient } from '../../src/client.js';
import type { ClientConfig } from '../../src/client.js';
import { ok } from '../../src/runtime/result/helpers.js';

// Mock fetch
global.fetch = vi.fn();

describe('Client - Auth Validation', () => {
  it('should return error when auth required but no token', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ success: true }),
    });

    const client = createClient({
      baseUrl: 'http://localhost:5000',
      auth: {
        getAccessToken: async () => null, // No token
      },
    });

    const result = await client.request({
      method: 'GET',
      path: '/protected',
      requiresAuth: true,
    });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.code).toBe('TOKEN_MISSING');
      expect(result.error.message).toContain('Authentication required');
    }
  });

  it('should allow authenticated requests with token provider', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'Content-Type': 'application/json' }),
      json: async () => ({ success: true }),
    });

    const client = createClient({
      baseUrl: 'http://localhost:5000',
      auth: {
        getAccessToken: async () => 'valid-token',
      },
    });

    const result = await client.request({
      method: 'GET',
      path: '/protected',
    });

    expect(result.ok).toBe(true);
    expect(global.fetch).toHaveBeenCalled();
    
    // Check Authorization header was added
    const fetchCall = (global.fetch as any).mock.calls[0];
    const headers = fetchCall[1].headers as Headers;
    expect(headers.get('Authorization')).toBe('Bearer valid-token');
  });

  it('should allow non-auth requests without token provider', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'Content-Type': 'application/json' }),
      json: async () => ({ success: true }),
    });

    const client = createClient({
      baseUrl: 'http://localhost:5000',
    });

    const result = await client.request({
      method: 'GET',
      path: '/public',
    });

    expect(result.ok).toBe(true);
    expect(global.fetch).toHaveBeenCalled();
  });
});
