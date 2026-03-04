/**
 * Fetch bridge with optional CORS proxy fallback.
 */

export interface FetchResult {
  status: number;
  headers: Record<string, string>;
  body: Uint8Array;
}

export class FetchBridge {
  private corsProxyUrl: string | null;

  constructor(options: { corsProxy?: string | null } = {}) {
    this.corsProxyUrl = options.corsProxy ?? null;
  }

  async fetch(url: string, options: { followRedirects?: boolean } = {}): Promise<FetchResult> {
    const redirect = options.followRedirects !== false ? ('follow' as RequestRedirect) : 'manual';
    try {
      const response = await fetch(url, { redirect });
      if (response.ok || response.type === 'opaqueredirect') {
        return {
          status: response.status,
          headers: Object.fromEntries(response.headers.entries()),
          body: new Uint8Array(await response.arrayBuffer()),
        };
      }
    } catch {
      // CORS error — fall through to proxy if available
    }
    if (this.corsProxyUrl) {
      const proxied = `${this.corsProxyUrl}/${encodeURIComponent(url)}`;
      const response = await fetch(proxied, { redirect });
      return {
        status: response.status,
        headers: Object.fromEntries(response.headers.entries()),
        body: new Uint8Array(await response.arrayBuffer()),
      };
    }
    throw new Error(`CORS blocked and no proxy configured: ${url}`);
  }
}
