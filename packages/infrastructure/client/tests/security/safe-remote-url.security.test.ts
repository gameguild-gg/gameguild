import { afterEach, afterAll, beforeAll, describe, expect, it } from 'vitest';

import { assertSafeRemoteUrl, UnsafeRemoteUrlError } from '../../src/runtime/security/safe-remote-url.js';

const originalNodeEnv = process.env.NODE_ENV;

beforeAll(() => {
  // The guard bypasses validation under NODE_ENV=test (existing fixtures use
  // local HTTP bases); these tests exercise the enforced path.
  process.env.NODE_ENV = 'production';
});

afterEach(() => {
  process.env.NODE_ENV = 'production';
  delete process.env.REMOTE_ASSET_ALLOWED_HOSTS;
  delete process.env.ALLOW_UNSAFE_REMOTE_URL;
});

afterAll(() => {
  process.env.NODE_ENV = originalNodeEnv;
});

describe('assertSafeRemoteUrl', () => {
  it('allows allowlisted https URLs', () => {
    expect(assertSafeRemoteUrl('https://api.gameguild.gg/v1/auth/mfa/methods').toString()).toBe('https://api.gameguild.gg/v1/auth/mfa/methods');
  });

  it('allows same-origin relative paths', () => {
    const target = assertSafeRemoteUrl('/api/auth/signin/discord');
    expect(target).toBe('/api/auth/signin/discord');
    expect(new URL(target, 'https://app.gameguild.gg').origin).toBe('https://app.gameguild.gg');
  });

  it.each(['auth/signin/discord', '?page=2', '/api/assets?id=%2F%2Fevil.example.com'])('preserves relative target %s verbatim', (target) => {
    expect(assertSafeRemoteUrl(target)).toBe(target);
  });

  it.each(['  //evil.example.com/x', '\t//evil.example.com/x', '\\\\evil.example.com/x', '/\\evil.example.com/x', '  https://evil.example.com/x'])(
    'rejects disguised cross-origin target %s',
    (target) => {
      expect(() => assertSafeRemoteUrl(target)).toThrow(UnsafeRemoteUrlError);
    },
  );

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

  it('ignores the development bypass in production', () => {
    process.env.ALLOW_UNSAFE_REMOTE_URL = 'true';
    expect(() => assertSafeRemoteUrl('http://localhost:8080/v1/auth/sessions')).toThrow(UnsafeRemoteUrlError);
  });

  it('ALLOW_UNSAFE_REMOTE_URL=true allows localhost http only in development', () => {
    process.env.NODE_ENV = 'development';
    process.env.ALLOW_UNSAFE_REMOTE_URL = 'true';
    expect(assertSafeRemoteUrl('http://localhost:8080/v1/auth/sessions')).toBe('http://localhost:8080/v1/auth/sessions');
  });
});
