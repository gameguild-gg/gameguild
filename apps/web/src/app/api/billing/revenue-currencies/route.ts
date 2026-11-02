import { getRequestAuthContext } from '@/auth';
import { type NextRequest, NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
}

/** Allowed dashboard windows (days). */
const ALLOWED_WINDOWS = new Set([7, 30, 90]);

function resolveWindow(value: string | null): number {
  const days = Number.parseInt(value ?? '30', 10);
  return ALLOWED_WINDOWS.has(days) ? days : 30;
}

/**
 * Browser-facing multi-currency revenue dashboard surface (issue #412 spec F).
 * One scoped proxy call returns the popular-currency ranking, the base-currency
 * revenue report (accounting reconciliation to base) and the conversion-cost
 * analysis for the requested window. `format=csv` streams the base-currency
 * report CSV export instead, so operators can hand accounting a file without
 * leaving the console. The caller's session token authorizes every upstream
 * read; SystemAdmin policies stay enforced on the API.
 */
export async function GET(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) {
    return NextResponse.json({ error: 'Authentication required.' }, { status: 401 });
  }

  const params = request.nextUrl.searchParams;
  const days = resolveWindow(params.get('days'));
  const toUtc = new Date();
  const fromUtc = new Date(toUtc.getTime() - days * 24 * 60 * 60 * 1000);
  const query = `fromUtc=${fromUtc.toISOString()}&toUtc=${toUtc.toISOString()}`;
  const headers = { authorization: `Bearer ${token}`, accept: 'application/json' };

  if (params.get('format') === 'csv') {
    const upstream = await fetch(
      `${apiBaseUrl()}/api/v1/billing/revenue/auditing/currencies/base/export?${query}`,
      { headers, cache: 'no-store', signal: request.signal },
    ).catch(() => null);
    if (!upstream?.ok) {
      return NextResponse.json({ error: 'Revenue export unavailable.' }, { status: 502 });
    }
    const payload = (await upstream.json().catch(() => null)) as
      | { content?: string; contentType?: string; fileName?: string }
      | null;
    if (!payload?.content) {
      return NextResponse.json({ error: 'Revenue export unavailable.' }, { status: 502 });
    }
    return new NextResponse(payload.content, {
      headers: {
        'content-type': payload.contentType ?? 'text/csv; charset=utf-8',
        'content-disposition': `attachment; filename="${payload.fileName ?? 'base-currency-revenue.csv'}"`,
        'cache-control': 'no-store',
      },
    });
  }

  const [popularResult, baseResult, costsResult] = await Promise.all([
    fetch(`${apiBaseUrl()}/api/v1/billing/revenue/auditing/currencies/popular?${query}&limit=12`, {
      headers,
      cache: 'no-store',
      signal: request.signal,
    }).catch(() => null),
    fetch(`${apiBaseUrl()}/api/v1/billing/revenue/auditing/currencies/base?${query}`, {
      headers,
      cache: 'no-store',
      signal: request.signal,
    }).catch(() => null),
    fetch(`${apiBaseUrl()}/api/v1/billing/revenue/auditing/conversion-costs?${query}`, {
      headers,
      cache: 'no-store',
      signal: request.signal,
    }).catch(() => null),
  ]);

  const popular: unknown = popularResult?.ok ? await popularResult.json().catch(() => null) : null;
  const base: unknown = baseResult?.ok ? await baseResult.json().catch(() => null) : null;
  const costs: unknown = costsResult?.ok ? await costsResult.json().catch(() => null) : null;

  return NextResponse.json(
    { days, popular, base, costs },
    { headers: { 'cache-control': 'no-store' } },
  );
}
