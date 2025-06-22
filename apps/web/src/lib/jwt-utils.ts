/**
 * JWT Utilities for token handling
 */

/**
 * Decode a JWT token without verification (for reading claims)
 * Note: This is only for reading token payload, not for verification
 */
export function decodeJwt(token: string): any {
  try {
    // JWT tokens have 3 parts separated by dots: header.payload.signature
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid JWT format');
    }

    // Decode the payload (second part)
    const payload = parts[1];
    
    // Add padding if needed for base64 decoding
    const paddedPayload = payload + '='.repeat((4 - payload.length % 4) % 4);
    
    // Decode from base64url
    const decoded = atob(paddedPayload.replace(/-/g, '+').replace(/_/g, '/'));
    
    return JSON.parse(decoded);
  } catch (error) {
    console.error('Failed to decode JWT:', error);
    return null;
  }
}

/**
 * Get the expiry date from a JWT token
 */
export function getJwtExpiryDate(token: string): Date | null {
  try {
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
  if (!expiryDate) {
    return true; // Assume expired if we can't read the expiry
  }

  return new Date() > expiryDate;
}

/**
 * Get all claims from a JWT token
 */
export function getJwtClaims(token: string): any {
  return decodeJwt(token);
}
