/**
 * Role helpers for the NextAuth access token.
 *
 * The API emits one ClaimTypes.Role claim per role (JwtTokenService), which
 * serializes to the long WS-2018 claim name. Some flows emit plain
 * `role`/`roles` instead, so all spellings are read.
 */

const ROLE_CLAIM_NAMES = [
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  'role',
  'roles',
] as const;

/**
 * Extract role claims from a JWT access token without verifying the signature
 * (the token comes from the trusted NextAuth session, not user input).
 * Returns an empty array on malformed input; never throws.
 */
export function getAccessTokenRoles(accessToken?: string | null): string[] {
  if (!accessToken) return [];

  try {
    const payloadPart = accessToken.split('.')[1];
    if (!payloadPart) return [];

    const payload = JSON.parse(
      Buffer.from(payloadPart, 'base64url').toString('utf8'),
    ) as Record<string, unknown>;

    const roles: string[] = [];
    for (const claim of ROLE_CLAIM_NAMES) {
      const value = payload[claim];
      if (typeof value === 'string') {
        roles.push(value);
      } else if (Array.isArray(value)) {
        roles.push(...value.filter((role): role is string => typeof role === 'string'));
      }
    }
    return roles;
  } catch {
    return [];
  }
}
