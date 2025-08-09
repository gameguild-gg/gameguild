/**
 * JWT debugging utilities for troubleshooting token issues
 */

interface JwtPayload {
  iss?: string; // issuer
  sub?: string; // subject
  aud?: string; // audience
  exp?: number; // expiration time
  iat?: number; // issued at
  nbf?: number; // not before
  jti?: string; // JWT ID
  [key: string]: any; // additional claims
}

interface TokenDebugInfo {
  isValid: boolean;
  payload: JwtPayload | null;
  header: any;
  isExpired: boolean;
  expiresAt: Date | null;
  issuedAt: Date | null;
  timeToExpiry: number | null; // milliseconds
  errors: string[];
}

/**
 * Debug a JWT token and return detailed information
 */
export function debugJwtToken(token: string): TokenDebugInfo {
  const errors: string[] = [];
  let payload: JwtPayload | null = null;
  let header: any = null;
  let isValid = false;
  let isExpired = false;
  let expiresAt: Date | null = null;
  let issuedAt: Date | null = null;
  let timeToExpiry: number | null = null;

  try {
    if (!token || typeof token !== 'string') {
      errors.push('Token is empty or not a string');
      return { isValid, payload, header, isExpired, expiresAt, issuedAt, timeToExpiry, errors };
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      errors.push('Invalid JWT format - should have 3 parts separated by dots');
      return { isValid, payload, header, isExpired, expiresAt, issuedAt, timeToExpiry, errors };
    }

    try {
      // Decode header
      let headerB64 = parts[0] || '';
      while (headerB64.length % 4) {
        headerB64 += '=';
      }
      const headerJson = Buffer.from(headerB64, 'base64').toString('utf-8');
      header = JSON.parse(headerJson);
    } catch (e) {
      errors.push(`Failed to decode header: ${e}`);
    }

    try {
      // Decode payload
      let payloadB64 = parts[1] || '';
      while (payloadB64.length % 4) {
        payloadB64 += '=';
      }
      const payloadJson = Buffer.from(payloadB64, 'base64').toString('utf-8');
      payload = JSON.parse(payloadJson);
      
      if (payload) {
        // Check expiration
        if (payload.exp) {
          expiresAt = new Date(payload.exp * 1000);
          const now = new Date();
          isExpired = now > expiresAt;
          timeToExpiry = expiresAt.getTime() - now.getTime();
        }

        // Check issued at
        if (payload.iat) {
          issuedAt = new Date(payload.iat * 1000);
        }

        isValid = true;
      }
    } catch (e) {
      errors.push(`Failed to decode payload: ${e}`);
    }

  } catch (e) {
    errors.push(`General parsing error: ${e}`);
  }

  return {
    isValid,
    payload,
    header,
    isExpired,
    expiresAt,
    issuedAt,
    timeToExpiry,
    errors
  };
}

/**
 * Log detailed JWT token information for debugging
 */
export function logTokenDebugInfo(token: string, label: string = 'JWT Token') {
  const debugInfo = debugJwtToken(token);
  
  console.group(`🔍 ${label} Debug Info`);
  
  console.log('✅ Basic Info:', {
    isValid: debugInfo.isValid,
    isExpired: debugInfo.isExpired,
    hasErrors: debugInfo.errors.length > 0
  });

  if (debugInfo.errors.length > 0) {
    console.error('❌ Errors:', debugInfo.errors);
  }

  if (debugInfo.header) {
    console.log('📋 Header:', debugInfo.header);
  }

  if (debugInfo.payload) {
    console.log('📋 Payload:', {
      ...debugInfo.payload,
      // Mask sensitive data
      sub: debugInfo.payload.sub ? `${debugInfo.payload.sub.substring(0, 8)}...` : undefined
    });
  }

  if (debugInfo.expiresAt) {
    console.log('⏰ Timing Info:', {
      expiresAt: debugInfo.expiresAt.toISOString(),
      issuedAt: debugInfo.issuedAt?.toISOString(),
      timeToExpiryMs: debugInfo.timeToExpiry,
      timeToExpiryMin: debugInfo.timeToExpiry ? Math.round(debugInfo.timeToExpiry / 60000) : null,
      currentTime: new Date().toISOString()
    });
  }

  console.groupEnd();
}

/**
 * Check if a token needs refreshing based on expiration time
 */
export function shouldRefreshToken(token: string, bufferTimeMs: number = 30000): boolean {
  const debugInfo = debugJwtToken(token);
  
  if (!debugInfo.isValid || !debugInfo.timeToExpiry) {
    return true; // Refresh invalid or expired tokens
  }
  
  return debugInfo.timeToExpiry <= bufferTimeMs;
}

/**
 * Extract specific claim from JWT token
 */
export function extractTokenClaim<T = any>(token: string, claimName: string): T | null {
  const debugInfo = debugJwtToken(token);
  
  if (!debugInfo.isValid || !debugInfo.payload) {
    return null;
  }
  
  return debugInfo.payload[claimName] ?? null;
}
