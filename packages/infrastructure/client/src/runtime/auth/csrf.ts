/**
 * CSRF Protection
 *
 * Generates and validates CSRF tokens to protect mutation routes
 * (sign-in, sign-out, sign-up) from cross-site request forgery.
 *
 * The token is a hash of a random value + the secret.
 * The random value is stored in a cookie; the hash is sent in the request body.
 */

/**
 * Generate a CSRF token pair: a cookie value and a hash.
 *
 * @param secret - The auth secret for hashing
 * @returns Object with `cookie` (random value) and `token` (hash)
 */
export async function createCSRFToken(secret: string): Promise<{
  cookie: string;
  token: string;
}> {
  const csrfTokenValue = generateRandomString(32);
  const csrfTokenHash = await hashToken(csrfTokenValue, secret);

  return {
    cookie: `${csrfTokenValue}|${csrfTokenHash}`,
    token: csrfTokenHash,
  };
}

/**
 * Validate a CSRF token from a request against the cookie value.
 *
 * @param cookieValue - The CSRF cookie value (`randomValue|hash`)
 * @param bodyToken - The CSRF token from the request body/header
 * @param secret - The auth secret
 * @returns True if valid
 */
export async function validateCSRFToken(cookieValue: string | null | undefined, bodyToken: string | null | undefined, secret: string): Promise<boolean> {
  if (!cookieValue || !bodyToken) return false;

  const [tokenValue, tokenHash] = cookieValue.split('|');

  if (!tokenValue || !tokenHash) return false;

  // Re-hash the cookie value and compare
  const expectedHash = await hashToken(tokenValue, secret);

  // Check that both the cookie hash and body token match
  return constantTimeEqual(tokenHash, expectedHash) && constantTimeEqual(bodyToken, expectedHash);
}

/**
 * Hash a token with the secret using SHA-256.
 */
async function hashToken(token: string, secret: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(`${token}${secret}`);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  return Array.from(new Uint8Array(hashBuffer))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

/**
 * Constant-time string comparison to prevent timing attacks.
 */
function constantTimeEqual(a: string, b: string): boolean {
  if (a.length !== b.length) return false;

  let result = 0;
  for (let i = 0; i < a.length; i++) {
    result |= a.charCodeAt(i) ^ b.charCodeAt(i);
  }
  return result === 0;
}

/**
 * Generate a cryptographically random hex string.
 */
function generateRandomString(length: number): string {
  const bytes = new Uint8Array(length);
  crypto.getRandomValues(bytes);
  return Array.from(bytes)
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}
