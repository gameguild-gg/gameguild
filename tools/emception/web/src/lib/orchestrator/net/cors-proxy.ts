/**
 * Lightweight CORS proxy configuration and helpers.
 *
 * This module provides utilities for working with CORS proxies when
 * fetching external resources (e.g. GitHub tarballs) from the browser.
 * It supports multiple proxy strategies:
 *
 * 1. Self-hosted: a Cloudflare Worker or similar that adds CORS headers
 * 2. Public proxies: allorigins, corsproxy.io, etc. (rate-limited)
 * 3. No proxy: direct fetch with CORS mode (works for github.com, etc.)
 */

export interface CorsProxyConfig {
  /** Base URL of the CORS proxy service (e.g. https://proxy.example.com). */
  url: string;
  /** How the target URL is passed: 'path' = /{url}, 'query' = ?url={url} */
  mode: 'path' | 'query';
  /** Query parameter name when mode is 'query' (default: 'url'). */
  queryParam?: string;
  /** Maximum request size in bytes (0 = unlimited). */
  maxSize?: number;
  /** Allowed origin domains for the target URL (empty = allow all). */
  allowedDomains?: string[];
}

const DEFAULT_PROXY_CONFIGS: CorsProxyConfig[] = [
  {
    url: 'https://corsproxy.io',
    mode: 'query',
    queryParam: 'url',
    maxSize: 50 * 1024 * 1024,
    allowedDomains: [],
  },
];

/**
 * Build a proxied URL from a target URL and proxy config.
 */
export function buildProxiedUrl(targetUrl: string, proxy: CorsProxyConfig): string {
  if (proxy.mode === 'query') {
    const param = proxy.queryParam ?? 'url';
    const separator = proxy.url.includes('?') ? '&' : '?';
    return `${proxy.url}${separator}${param}=${encodeURIComponent(targetUrl)}`;
  }
  // path mode: append the encoded URL as a path segment
  const base = proxy.url.replace(/\/+$/, '');
  return `${base}/${encodeURIComponent(targetUrl)}`;
}

/**
 * Check if a target URL matches the proxy's allowed domains.
 * Returns true if the proxy allows this domain (or allows all domains).
 */
export function isAllowedByProxy(targetUrl: string, proxy: CorsProxyConfig): boolean {
  if (!proxy.allowedDomains || proxy.allowedDomains.length === 0) return true;
  try {
    const hostname = new URL(targetUrl).hostname;
    return proxy.allowedDomains.some((d) => hostname === d || hostname.endsWith(`.${d}`));
  } catch {
    return false;
  }
}

/**
 * Try to fetch a URL, falling back through CORS proxies if direct fetch fails.
 * Returns the first successful response.
 */
export async function fetchWithCorsProxy(
  targetUrl: string,
  proxies: CorsProxyConfig[] = DEFAULT_PROXY_CONFIGS,
): Promise<Response> {
  // Try direct fetch first (works for CORS-friendly hosts like github.com)
  try {
    const response = await fetch(targetUrl, { mode: 'cors' });
    if (response.ok) return response;
  } catch {
    // CORS error — try proxies
  }

  // Try each proxy in order
  for (const proxy of proxies) {
    if (!isAllowedByProxy(targetUrl, proxy)) continue;

    const proxiedUrl = buildProxiedUrl(targetUrl, proxy);
    try {
      const response = await fetch(proxiedUrl, { mode: 'cors' });
      if (response.ok) return response;
    } catch {
      continue;
    }
  }

  throw new Error(
    `Failed to fetch ${targetUrl}: CORS blocked and no proxy succeeded. ` +
    `Tried ${proxies.length} proxy(ies).`,
  );
}

/**
 * Cloudflare Worker script template for a self-hosted CORS proxy.
 * Deploy this to Cloudflare Workers to run your own proxy.
 */
export const CLOUDFLARE_WORKER_TEMPLATE = `
// Deploy: wrangler deploy --name cors-proxy
export default {
  async fetch(request) {
    const url = new URL(request.url);
    const targetUrl = url.searchParams.get('url') || url.pathname.slice(1);

    if (!targetUrl) {
      return new Response('Usage: ?url=<target> or /<encoded-target>', { status: 400 });
    }

    const decoded = decodeURIComponent(targetUrl);
    const target = decoded.startsWith('http') ? decoded : 'https://' + decoded;

    const response = await fetch(target, {
      method: request.method,
      headers: {
        'User-Agent': 'BrowserToolchain-CORS-Proxy/1.0',
      },
    });

    const headers = new Headers(response.headers);
    headers.set('Access-Control-Allow-Origin', '*');
    headers.set('Access-Control-Allow-Methods', 'GET, HEAD, OPTIONS');
    headers.set('Access-Control-Allow-Headers', '*');

    if (request.method === 'OPTIONS') {
      return new Response(null, { status: 204, headers });
    }

    return new Response(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers,
    });
  },
};
`;
