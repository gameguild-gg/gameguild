import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest';

import { assertSafeRemoteUrl, UnsafeRemoteUrlError } from './safe-remote-url';

const env = vi.hoisted(() => ({ NODE_ENV: 'production', REMOTE_ASSET_ALLOWED_HOSTS: undefined as string | undefined, ALLOW_UNSAFE_REMOTE_URL: undefined as string | undefined }));

vi.mock('node:process', () => ({ default: { get env() { return env; } } }));
vi.stubGlobal('process', { env });

beforeAll(() => {
  // The guard bypasses validation under NODE_ENV=test (existing fixtures use
  // local HTTP bases); these tests exercise the enforced path.
  env.NODE_ENV = 'production';
});

afterEach(() => {
  env.REMOTE_ASSET_ALLOWED_HOSTS = undefined;
  env.ALLOW_UNSAFE_REMOTE_URL = undefined; env.NODE_ENV = 'production';
});

describe('assertSafeRemoteUrl', () => {
  it('allows allowlisted https URLs', () => {
    expect(new URL(assertSafeRemoteUrl('https://cdn.gameguild.gg/logo.png').toString()).href).toBe('https://cdn.gameguild.gg/logo.png');
  });

  it('allows same-origin relative paths', () => {
    expect(new URL(assertSafeRemoteUrl('/api/assets/123/content').toString(), 'https://app.gameguild.gg').pathname).toBe('/api/assets/123/content');
  });

  it('rejects http', () => {
    expect(() => assertSafeRemoteUrl('http://cdn.gameguild.gg/logo.png')).toThrow(UnsafeRemoteUrlError);
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
    expect(() => assertSafeRemoteUrl('https://user:pass@cdn.gameguild.gg/x')).toThrow(/credentials/);
  });

  it('rejects protocol-relative URLs', () => {
    expect(() => assertSafeRemoteUrl('//evil.example.com/x')).toThrow(UnsafeRemoteUrlError);
  });

  it('honours REMOTE_ASSET_ALLOWED_HOSTS additions', () => {
    env.REMOTE_ASSET_ALLOWED_HOSTS = 'extra.example.com';
    expect(new URL(assertSafeRemoteUrl('https://extra.example.com/x').toString()).hostname).toBe('extra.example.com');
    expect(() => assertSafeRemoteUrl('https://cdn.gameguild.gg/x')).toThrow(/allowlist/);
  });

  it('ALLOW_UNSAFE_REMOTE_URL=true allows localhost http', () => {
    env.ALLOW_UNSAFE_REMOTE_URL = 'true';
    expect(assertSafeRemoteUrl('http://localhost:8080/v1/assets')).toBe('http://localhost:8080/v1/assets');
  });
});
