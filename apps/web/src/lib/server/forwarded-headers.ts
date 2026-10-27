import 'server-only';

import { headers } from 'next/headers';

/**
 * Returns the client IP as set by Cloudflare (`cf-connecting-ip`).
 *
 * `cf-connecting-ip` is only trustworthy if the request actually traversed
 * Cloudflare, so `cf-ray` (set by the edge) must be present too. Direct-origin
 * requests yield null. Deliberately does NOT fall back to `x-forwarded-for`:
 * when Cloudflare headers are absent (direct access), the first XFF entry is
 * client-controlled and trivially spoofable. No header → `null`.
 */
export async function getClientIp(): Promise<string | null> {
  const requestHeaders = await headers();
  if (!requestHeaders.get('cf-ray')?.trim()) return null;
  const ip = requestHeaders.get('cf-connecting-ip')?.trim();
  return ip ? ip : null;
}

/**
 * Headers to forward on proxied API calls so the backend sees the real client
 * IP. Empty when no trusted client IP is available.
 */
export async function forwardedIpHeaders(): Promise<Record<string, string>> {
  const ip = await getClientIp();
  return ip ? { 'X-Forwarded-For': ip } : {};
}
