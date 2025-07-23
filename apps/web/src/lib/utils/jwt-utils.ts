/**
 * JWT Utilities for token handling
 */

/**
 * JWT payload interface for type safety
 */
interface JwtPayload {
  exp?: number;
  iat?: number;
  sub?: string;
  [key: string]: unknown;
}

/**
 * Decode a JWT token without verification (for reading claims)
 * Note: This is only for reading token payload, not for verification
 */
export function decodeJwt(token: string): JwtPayload | null {
  try {
    // Handle non-JWT tokens (like admin placeholders)
    if (!token || !token.includes('.')) {
      return null;
    }

    // JWT tokens have 3 parts separated by dots: header.payload.signature
    const parts = token.split('.');
    if (parts.length !== 3) {
      return null;
    }

    // Decode the payload (second part)
    const payload = parts[1];

    // Add padding if needed for base64 decoding
    const paddedPayload = payload + '='.repeat((4 - (payload.length % 4)) % 4);

    // Decode from base64url
    const decoded = atob(paddedPayload.replace(/-/g, '+').replace(/_/g, '/'));

    return JSON.parse(decoded) as JwtPayload;
  } catch {
    // Silent fail for invalid tokens
    return null;
  }
}

/**
 * Get the expiry date from a JWT token
 */
export function getJwtExpiryDate(token: string): Date | null {
  try {
    // Handle non-JWT tokens gracefully
    if (!token || !token.includes('.')) {
      console.warn('Token is not a valid JWT format, skipping expiry check');
      return null;
    }

    const payload = decodeJwt(token);
    if (!payload || !payload.exp) {
      return null;
    }

    // JWT exp claim is in seconds, Date constructor expects milliseconds
    return new Date(payload.exp * 1000);
  } catch (error) {
    console.error('Failed to get JWT expiry date:', error);
    return null;
  }
}

/**
 * Check if a JWT token is expired
 */
export function isJwtExpired(token: string): boolean {
  const expiryDate = getJwtExpiryDate(token);
  // Assume expired if we can't read the expiry
  if (!expiryDate) {
    return true;
  }

  return new Date() > expiryDate;
}

/**
 * Get all claims from a JWT token
 */
export function getJwtClaims(token: string): JwtPayload | null {
  return decodeJwt(token);
}

/**
 * Get a specific claim from a JWT token
 */
export function getJwtClaim(token: string, claimName: string): unknown {
  const payload = decodeJwt(token);
  return payload?.[claimName] ?? null;
}

/**
 * Check if a JWT token is valid (not expired and properly formatted)
 */
export function isJwtValid(token: string): boolean {
  const payload = decodeJwt(token);
  if (!payload) {
    return false;
  }

  // Check if token is expired
  return !isJwtExpired(token);
}
