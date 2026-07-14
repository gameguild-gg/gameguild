/**
 * Fetch bridge — direct fetch with no proxy.
 */

export interface FetchResult {
  status: number;
  headers: Record<string, string>;
  body: Uint8Array;
}

export class FetchBridge {
  async fetch(url: string, options: { followRedirects?: boolean } = {}): Promise<FetchResult> {
    const redirect = options.followRedirects !== false ? ('follow' as RequestRedirect) : 'manual';
    const response = await fetch(url, { redirect });
    return {
      status: response.status,
      headers: Object.fromEntries(response.headers.entries()),
      body: new Uint8Array(await response.arrayBuffer()),
    };
  }
}
