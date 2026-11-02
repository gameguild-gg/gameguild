/**
 * Forwarded IP Header Interceptor
 *
 * Forwards the end-user IP to the API from server-side clients. Cloudflare
 * guarantees `cf-connecting-ip` holds the real client IP in both ingress
 * modes, so it is the only trusted source. `x-forwarded-for` entries are
 * client-controlled when `cf-connecting-ip` is absent and must never be
 * used as a fallback.
 */

import { createHeaderInterceptor } from './fetch.js';
import type { Interceptor } from './types.js';

/**
 * Resolve the forwarded-IP headers from a header getter.
 *
 * Returns `{ 'X-Forwarded-For': <ip> }` when `cf-connecting-ip` is present
 * (trimmed, non-empty) AND `cf-ray` is present — `cf-ray` is set by the
 * Cloudflare edge and corroborates that the request actually traversed
 * Cloudflare, so `cf-connecting-ip` alone (direct-origin spoof) yields `{}`.
 * `cf-ray` alone yields nothing; the forwarded value is still the
 * `cf-connecting-ip` value. Deliberately no `x-forwarded-for` fallback: its
 * entries are client-controlled when the Cloudflare headers are missing.
 */
export function resolveForwardedIpHeader(get: (name: string) => string | null): Record<string, string> {
  if (!get('cf-ray')?.trim()) return {};
  const ip = get('cf-connecting-ip')?.trim();
  return ip ? { 'X-Forwarded-For': ip } : {};
}

/**
 * Create an interceptor that forwards the visitor IP from Next.js request
 * headers. `next/headers` is imported dynamically so `next` stays an optional
 * peer dependency: outside a Next.js request scope (other runtimes, no
 * request context, build phase) the interceptor resolves to no headers
 * instead of throwing.
 */
export function createForwardedIpHeaderInterceptor(): Interceptor {
  return createHeaderInterceptor(async () => {
    try {
      const { headers } = await import('next/headers');
      const requestHeaders = await headers();
      return resolveForwardedIpHeader((name) => requestHeaders.get(name));
    } catch {
      // Not running in a Next.js request scope (or `next` is not resolvable).
      return {};
    }
  });
}
