import { describe, expect, it } from 'vitest';
import { getAccessTokenRoles } from './roles';

const LONG_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function token(payload: Record<string, unknown>): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value), 'utf8').toString('base64url');
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.${encode({ sig: 'x' })}`;
}

describe('getAccessTokenRoles', () => {
  it('reads the long ClaimTypes.Role serialization', () => {
    const accessToken = token({ sub: 'user-1', [LONG_ROLE_CLAIM]: 'SystemAdmin' });

    expect(getAccessTokenRoles(accessToken)).toEqual(['SystemAdmin']);
  });

  it('reads plain role claim (string)', () => {
    const accessToken = token({ sub: 'user-1', role: 'Member' });

    expect(getAccessTokenRoles(accessToken)).toEqual(['Member']);
  });

  it('reads roles claim (array)', () => {
    const accessToken = token({ sub: 'user-1', roles: ['Owner', 'User'] });

    expect(getAccessTokenRoles(accessToken)).toEqual(['Owner', 'User']);
  });

  it('aggregates claim spellings and ignores non-string entries', () => {
    const accessToken = token({ sub: 'user-1', role: 'Admin', roles: ['Owner', 42, null] });

    expect(getAccessTokenRoles(accessToken)).toEqual(['Admin', 'Owner']);
  });

  it('returns [] for garbage tokens', () => {
    expect(getAccessTokenRoles('not-a-jwt')).toEqual([]);
    expect(getAccessTokenRoles('a.????.c')).toEqual([]);
    expect(getAccessTokenRoles('header.payload-without-json.sig')).toEqual([]);
  });

  it('returns [] for undefined or empty input', () => {
    expect(getAccessTokenRoles(undefined)).toEqual([]);
    expect(getAccessTokenRoles(null)).toEqual([]);
    expect(getAccessTokenRoles('')).toEqual([]);
  });
});
