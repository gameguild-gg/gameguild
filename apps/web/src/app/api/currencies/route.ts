import { getRequestAuthContext } from '@/auth';
import { type NextRequest, NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
}

/**
 * Browser-facing currency surface (issue #412 spec E). One authenticated fetch
 * returns the tenant's enabled currencies (selector list) and the GDPR-minimized
 * location-based suggestion. Anonymous callers keep the suggestion — it consumes
 * only the edge-proxy country code — and receive an empty enabled list, which the
 * client replaces with its static fallback.
 */
export async function GET(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };

  // Edge proxies deliver the caller's country code; forward it so the API never
  // sees (or stores) an IP address for currency suggestion.
  const country =
    request.headers.get('cf-ipcountry') ??
    request.headers.get('x-country-code') ??
    request.headers.get('x-vercel-ip-country') ??
    '';
  const suggestionHeaders: Record<string, string> = { accept: 'application/json' };
  if (country) {
    suggestionHeaders['cf-ipcountry'] = country;
  }

  const suggestionPromise = fetch(`${apiBaseUrl()}/api/v1/currencies/suggestion`, {
    headers: suggestionHeaders,
    cache: 'no-store',
    signal: request.signal,
  }).catch(() => null);

  const currenciesPromise = token
    ? fetch(`${apiBaseUrl()}/api/v1/currencies`, {
        headers: { authorization: `Bearer ${token}`, accept: 'application/json' },
        cache: 'no-store',
        signal: request.signal,
      }).catch(() => null)
    : Promise.resolve(null);

  const [suggestionResult, currenciesResult] = await Promise.all([suggestionPromise, currenciesPromise]);

  const suggestion: unknown = suggestionResult?.ok
    ? await suggestionResult.json().catch(() => null)
    : null;
  const currencies: unknown = currenciesResult?.ok
    ? await currenciesResult.json().catch(() => null)
    : [];

  return NextResponse.json(
    { currencies, suggestion },
    { headers: { 'cache-control': 'no-store' } },
  );
}
