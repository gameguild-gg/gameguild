/**
 * JWT Encryption/Decryption
 *
 * Encrypts and decrypts the session JWT stored in cookies.
 * Uses the same algorithm as next-auth: A256CBC-HS512 with HKDF key derivation.
 *
 * The JWT is NOT a standard signed JWT — it's an encrypted JWE that contains
 * the session data (including .NET backend tokens). This prevents client-side
 * reading of tokens while still being stateless.
 */

import { EncryptJWT, jwtDecrypt, type JWTPayload as JoseJWTPayload } from 'jose';
import type { JWTPayload } from './types.js';

/** Default max age for the JWT (30 days) */
const DEFAULT_MAX_AGE = 30 * 24 * 60 * 60; // 30 days in seconds

/** Encryption algorithm — same as next-auth */
const ENCRYPTION_ALGORITHM = 'A256CBC-HS512';

/** Content encryption algorithm */
const CONTENT_ENCRYPTION = 'dir';

/**
 * Derive an encryption key from the secret using HKDF.
 * Uses the Web Crypto API's HKDF key derivation — same approach as next-auth.
 *
 * Results are cached per secret to avoid redundant HKDF operations on every
 * encode/decode call (HKDF is deterministic for the same input).
 *
 * @param secret - The AUTH_SECRET string
 * @returns A CryptoKey suitable for A256CBC-HS512
 */
const keyCache = new Map<string, Uint8Array>();

async function deriveEncryptionKey(secret: string): Promise<Uint8Array> {
  const cached = keyCache.get(secret);
  if (cached) return cached;

  const encoder = new TextEncoder();
  const inputKey = encoder.encode(secret);
  const info = encoder.encode('GameGuild Auth Encrypted JWT');
  const salt = encoder.encode('');

  // Import the secret as HKDF key material
  const keyMaterial = await crypto.subtle.importKey('raw', inputKey, 'HKDF', false, ['deriveBits']);

  // Derive 512 bits (64 bytes) for A256CBC-HS512
  const derivedBits = await crypto.subtle.deriveBits(
    {
      name: 'HKDF',
      hash: 'SHA-256',
      salt,
      info,
    },
    keyMaterial,
    512,
  );

  const key = new Uint8Array(derivedBits);
  keyCache.set(secret, key);
  return key;
}

/**
 * Encode (encrypt) a JWT payload into an encrypted JWE string.
 *
 * @param params.token - The JWT payload to encrypt
 * @param params.secret - The encryption secret
 * @param params.maxAge - Maximum age in seconds (default: 30 days)
 * @returns The encrypted JWE string
 *
 * @example
 * ```typescript
 * const token = await encodeJWT({
 *   token: { user: { id: '1', email: 'a@b.com' }, accessToken: '...', refreshToken: '...' },
 *   secret: process.env.AUTH_SECRET!,
 * });
 * ```
 */
export async function encodeJWT(params: { token: JWTPayload; secret: string; maxAge?: number }): Promise<string> {
  const { token, secret, maxAge = DEFAULT_MAX_AGE } = params;
  const encryptionKey = await deriveEncryptionKey(secret);

  const now = Math.floor(Date.now() / 1000);
  const issuedAt = token.iat ?? now;
  const expiresAt = token.exp ?? issuedAt + maxAge;

  // Normal session encoding gets fresh claims, but callers can still provide
  // explicit iat/exp values when they need a token with fixed boundaries.
  const payload: JoseJWTPayload & Record<string, unknown> = {
    ...token,
    iat: issuedAt,
    exp: expiresAt,
    jti: token.jti ?? generateId(),
  };

  return await new EncryptJWT(payload)
    .setProtectedHeader({ alg: CONTENT_ENCRYPTION, enc: ENCRYPTION_ALGORITHM })
    .setIssuedAt(payload.iat)
    .setExpirationTime(payload.exp as number)
    .setJti(payload.jti as string)
    .encrypt(encryptionKey);
}

/**
 * Decode (decrypt) a JWE string back into a JWT payload.
 *
 * @param params.token - The encrypted JWE string
 * @param params.secret - The encryption secret
 * @returns The decrypted JWT payload, or null if invalid/expired
 *
 * @example
 * ```typescript
 * const payload = await decodeJWT({
 *   token: cookieValue,
 *   secret: process.env.AUTH_SECRET!,
 * });
 * if (payload) {
 *   console.log(payload.user.email);
 * }
 * ```
 */
export async function decodeJWT(params: { token: string; secret: string }): Promise<JWTPayload | null> {
  const { token, secret } = params;

  if (!token) return null;

  try {
    const encryptionKey = await deriveEncryptionKey(secret);

    const { payload } = await jwtDecrypt(token, encryptionKey, {
      contentEncryptionAlgorithms: [ENCRYPTION_ALGORITHM],
      keyManagementAlgorithms: [CONTENT_ENCRYPTION],
      clockTolerance: 15, // 15 seconds tolerance
    });

    return payload as unknown as JWTPayload;
  } catch {
    // Token is invalid, expired, or tampered with
    return null;
  }
}

/**
 * Generate a random unique identifier.
 */
function generateId(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return Array.from(bytes)
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}
