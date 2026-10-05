import { afterEach, afterAll, beforeAll, describe, expect, it } from 'vitest';

import { assertSafeRemoteUrl, UnsafeRemoteUrlError } from '../../src/runtime/security/safe-remote-url.js';

const originalNodeEnv = process.env.NODE_ENV;

beforeAll(() => {
  // The guard bypasses validation under NODE_ENV=test (existing fixtures use
  // local HTTP bases); these tests exercise the enforced path.
  process.env.NODE_ENV = 'production';
});

afterEach(() => {
  delete process.env.REMOTE_ASSET_ALLOWED_HOSTS;
  delete process.env.ALLOW_UNSAFE_REMOTE_URL;
});

afterAll(() => {
  process.env.NODE_ENV = originalNodeEnv;
});

describe('assertSafeRemoteUrl', () => {
  it('allows allowlisted https URLs', () => {
    expect(assertSafeRemoteUrl('https://api.gameguild.gg/v1/auth/mfa/methods').toString()).toBe(
      'https://api.gameguild.gg/v1/auth/mfa/methods',
    );
  });

  it('allows same-origin relative paths', () => {
    expect(new URL(assertSafeRemoteUrl('/api/auth/signin/discord').toString(), 'https://app.gameguild.gg').pathname).toBe('/api/auth/signin/discord');
  });

  it('rejects http', () => {
    expect(() => assertSafeRemoteUrl('http://api.gameguild.gg/v1/auth/mfa/verify')).toThrow(UnsafeRemoteUrlError);
  });

  it('rejects hosts outside the allowlist', () => {
    expect(() => assertSafeRemoteUrl('https://evil.example.com/x')).toThrow(/allowlist/);
  });

  it.each([
    'https://127.0.0.1/x',
    'https://10.0.0.5/x',
    'https://172.16.0.9/x',
    'https://192.168.1.4/x',
    'https://169.254.169.254/latest/meta-data',
    'https://localhost/x',
    'https://[::1]/x',
  ])('rejects private or loopback host %s', (url) => {
    expect(() => assertSafeRemoteUrl(url)).toThrow(UnsafeRemoteUrlError);
  });

  it('rejects embedded credentials', () => {
    expect(() => assertSafeRemoteUrl('https://user:pass@api.gameguild.gg/x')).toThrow(/credentials/);
  });

  it('honours REMOTE_ASSET_ALLOWED_HOSTS additions', () => {
    process.env.REMOTE_ASSET_ALLOWED_HOSTS = 'extra.example.com';
    expect(new URL(assertSafeRemoteUrl('https://extra.example.com/x').toString()).hostname).toBe('extra.example.com');
    expect(() => assertSafeRemoteUrl('https://api.gameguild.gg/x')).toThrow(/allowlist/);
  });

  it('ALLOW_UNSAFE_REMOTE_URL=true allows localhost http', () => {
    process.env.ALLOW_UNSAFE_REMOTE_URL = 'true';
    expect(assertSafeRemoteUrl('http://localhost:8080/v1/auth/sessions')).toBe('http://localhost:8080/v1/auth/sessions');
  });
});
