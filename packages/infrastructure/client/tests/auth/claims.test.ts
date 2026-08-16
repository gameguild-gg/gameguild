import { describe, expect, it } from 'vitest';
import { resolveAuthPermissions, resolveAuthRoles } from '../../src/runtime/auth/claims.js';

function unsignedToken(payload: unknown): string {
  const encode = (value: unknown) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.`;
}

describe('auth claim resolution', () => {
  it('prefers response claims over backend user and token claims', () => {
    const data = {
      accessToken: unsignedToken({ roles: ['token-role'], permissions: ['token:read'] }),
      roles: [' response-role ', 10],
      permissions: 'response:read,response:write',
    };

    expect(resolveAuthRoles(data, { roles: ['user-role'] })).toEqual(['response-role']);
    expect(resolveAuthPermissions(data, { permissions: ['user:read'] })).toEqual(['response:read', 'response:write']);
  });

  it('falls back to backend user claims', () => {
    const data = { accessToken: unsignedToken({ role: 'token-role', scope: 'token:read' }) };

    expect(resolveAuthRoles(data, { role: 'admin operator' })).toEqual(['admin', 'operator']);
    expect(resolveAuthPermissions(data, { scope: 'users:read users:write' })).toEqual(['users:read', 'users:write']);
  });

  it('resolves standard claims from the access token', () => {
    const data = {
      accessToken: unsignedToken({
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['admin', 'operator'],
        scp: 'courses:read courses:write',
      }),
    };

    expect(resolveAuthRoles(data)).toEqual(['admin', 'operator']);
    expect(resolveAuthPermissions(data)).toEqual(['courses:read', 'courses:write']);
  });

  it('resolves the product permission claim URI', () => {
    const data = {
      accessToken: unsignedToken({
        'http://schemas.gameguild.com/identity/claims/permission': ['tenant:read'],
      }),
    };

    expect(resolveAuthPermissions(data)).toEqual(['tenant:read']);
  });

  it('ignores empty claims and continues to the next source', () => {
    const data = {
      accessToken: unsignedToken({ roles: ['token-role'], permissions: ['token:read'] }),
      roles: [],
      permissions: '   ',
    };

    expect(resolveAuthRoles(data, { roles: [] })).toEqual(['token-role']);
    expect(resolveAuthPermissions(data, { permissions: [] })).toEqual(['token:read']);
  });

  it('returns undefined for missing or malformed token claims', () => {
    expect(resolveAuthRoles({})).toBeUndefined();
    expect(resolveAuthPermissions({ accessToken: 'not-a-jwt' })).toBeUndefined();
    expect(resolveAuthPermissions({ accessToken: 'header.invalid-json.signature' })).toBeUndefined();
    expect(resolveAuthRoles({ accessToken: unsignedToken('not-an-object') })).toBeUndefined();
    expect(resolveAuthPermissions({ accessToken: unsignedToken(null) })).toBeUndefined();
  });
});
