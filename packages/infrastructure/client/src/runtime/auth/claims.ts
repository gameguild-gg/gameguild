const ROLE_CLAIM_URIS = [
  'roles',
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role',
];

const PERMISSION_CLAIM_URIS = ['permissions', 'permission', 'scope', 'scp', 'http://schemas.gameguild.com/identity/claims/permission'];

function decodeBase64Url(value: string): string {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
  const binary = globalThis.atob(padded);
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}

function decodeJwtPayload(accessToken?: unknown): Record<string, unknown> | undefined {
  if (typeof accessToken !== 'string') {
    return undefined;
  }

  const [, payload] = accessToken.split('.');
  if (!payload) {
    return undefined;
  }

  try {
    const decoded = decodeBase64Url(payload);
    const parsed = JSON.parse(decoded) as unknown;
    return parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : undefined;
  } catch {
    return undefined;
  }
}

function normalizeStringArray(value: unknown): string[] | undefined {
  if (Array.isArray(value)) {
    const normalized = value
      .filter((item): item is string => typeof item === 'string')
      .map((item) => item.trim())
      .filter(Boolean);
    return normalized.length > 0 ? normalized : undefined;
  }

  if (typeof value === 'string') {
    const normalized = value
      .split(/[\s,]+/)
      .map((item) => item.trim())
      .filter(Boolean);
    return normalized.length > 0 ? normalized : undefined;
  }

  return undefined;
}

function firstClaimValue(data: Record<string, unknown>, backendUser: Record<string, unknown> | undefined, claimKeys: string[]): string[] | undefined {
  const tokenClaims = decodeJwtPayload(data.accessToken);

  for (const key of claimKeys) {
    const value = normalizeStringArray(data[key]) ?? normalizeStringArray(backendUser?.[key]) ?? normalizeStringArray(tokenClaims?.[key]);

    if (value) {
      return value;
    }
  }

  return undefined;
}

export function resolveAuthRoles(data: Record<string, unknown>, backendUser?: Record<string, unknown>): string[] | undefined {
  return firstClaimValue(data, backendUser, ROLE_CLAIM_URIS);
}

export function resolveAuthPermissions(data: Record<string, unknown>, backendUser?: Record<string, unknown>): string[] | undefined {
  return firstClaimValue(data, backendUser, PERMISSION_CLAIM_URIS);
}
