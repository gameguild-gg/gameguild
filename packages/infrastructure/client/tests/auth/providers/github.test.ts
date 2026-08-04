/**
 * GitHub Provider Tests
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { GitHubProvider } from '../../../src/runtime/auth/providers/github.js';
import { OAuthError } from '../../../src/runtime/auth/errors.js';

describe('GitHubProvider', () => {
  let originalFetch: typeof globalThis.fetch;

  beforeEach(() => {
    originalFetch = globalThis.fetch;
    vi.restoreAllMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('should create provider with correct config', () => {
    const provider = GitHubProvider({
      clientId: 'gh-client-id',
      clientSecret: 'gh-client-secret',
    });

    expect(provider.id).toBe('github');
    expect(provider.name).toBe('GitHub');
    expect(provider.type).toBe('oauth');
    expect(provider.clientId).toBe('gh-client-id');
    expect(provider.clientSecret).toBe('gh-client-secret');
    expect(provider.getAuthorizeUrl).toBeDefined();
    expect(provider.handleCallback).toBeDefined();
  });

  it('should have correct default authorization URLs', () => {
    const provider = GitHubProvider({
      clientId: 'id',
      clientSecret: 'secret',
    });

    expect(provider.authorization.url).toBe(
      'https://github.com/login/oauth/authorize',
    );
    expect(provider.authorization.params.scope).toBe('read:user user:email');
    expect(provider.token.url).toBe(
      'https://github.com/login/oauth/access_token',
    );
    expect(provider.userinfo.url).toBe('https://api.github.com/user');
  });

  describe('getAuthorizeUrl', () => {
    it('should call backend authorize endpoint', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ authUrl: 'https://github.com/login?state=abc' }),
      });
      globalThis.fetch = mockFetch;

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
      });

      const url = await provider.getAuthorizeUrl('http://localhost:8080');

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining(
          'http://localhost:8080/v1/auth/github:authorize',
        ),
        { method: 'GET' },
      );
      expect(url).toBe('https://github.com/login?state=abc');
    });

    it('should use custom authorizePath', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ authUrl: 'https://github.com/login' }),
      });
      globalThis.fetch = mockFetch;

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
        authorizePath: '/custom/github/auth',
      });

      await provider.getAuthorizeUrl('http://localhost:8080');

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/custom/github/auth'),
        expect.anything(),
      );
    });

    it('should include redirectUri in query params', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ authUrl: 'https://github.com/login' }),
      });
      globalThis.fetch = mockFetch;

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
      });

      await provider.getAuthorizeUrl(
        'http://localhost:8080',
        'http://localhost:3000/callback',
      );

      const calledUrl = mockFetch.mock.calls[0][0] as string;
      expect(calledUrl).toContain('redirectUri=');
    });

    it('should prefer options.apiUrl over passed apiUrl', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ authUrl: 'https://github.com/login' }),
      });
      globalThis.fetch = mockFetch;

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
        apiUrl: 'http://options-api:5295',
      });

      await provider.getAuthorizeUrl('http://passed-api:5295');

      const calledUrl = mockFetch.mock.calls[0][0] as string;
      expect(calledUrl).toContain('http://options-api:5295');
    });

    it('should throw OAuthError on non-ok response', async () => {
      globalThis.fetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 500,
      });

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
      });

      await expect(
        provider.getAuthorizeUrl('http://localhost:8080'),
      ).rejects.toThrow(OAuthError);
    });
  });

  describe('handleCallback', () => {
    it('should exchange code for tokens', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          accessToken: 'gh-access-token',
          refreshToken: 'gh-refresh-token',
          userId: 'user-1',
          email: 'test@example.com',
          user: { displayName: 'GitHub User', profilePictureUrl: 'https://pic.com/a.png' },
        }),
      });
      globalThis.fetch = mockFetch;

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
      });

      const result = await provider.handleCallback(
        'http://localhost:8080',
        'auth-code-123',
        'state-value',
      );

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:8080/v1/auth/github:callback',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ code: 'auth-code-123', state: 'state-value' }),
        }),
      );

      expect(result.tokens.accessToken).toBe('gh-access-token');
      expect(result.user.id).toBe('user-1');
      expect(result.user.name).toBe('GitHub User');
    });

    it('should throw OAuthError on non-ok response', async () => {
      globalThis.fetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ message: 'Invalid code' }),
      });

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
      });

      await expect(
        provider.handleCallback('http://localhost:8080', 'bad-code'),
      ).rejects.toThrow(OAuthError);
    });

    it('should handle non-JSON error response', async () => {
      globalThis.fetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 500,
        json: async () => { throw new Error('not json'); },
      });

      const provider = GitHubProvider({
        clientId: 'id',
        clientSecret: 'secret',
      });

      await expect(
        provider.handleCallback('http://localhost:8080', 'code'),
      ).rejects.toThrow(OAuthError);
    });
  });
});
