import { getRequestAuthContext } from '@/auth';
import { type NextRequest, NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
}

/**
 * Browser-facing conversion proxy (issue #412 spec E): converts published prices
 * into the visitor's selected display currency through the platform FX seam so
 * price displays and checkout stay transparent about the original amount. The
 * caller's session token authorizes the upstream read; the conversion itself is
 * read-only and rate-limited upstream.
 */
export async function POST(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) {
    return NextResponse.json({ error: 'Authentication required.' }, { status: 401 });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ error: 'Invalid JSON body.' }, { status: 400 });
  }

  const upstream = await fetch(`${apiBaseUrl()}/api/v1/currency/convert`, {
    method: 'POST',
    headers: { authorization: `Bearer ${token}`, accept: 'application/json', 'content-type': 'application/json' },
    body: JSON.stringify(body),
    cache: 'no-store',
    signal: request.signal,
  }).catch(() => null);

  if (!upstream) {
    return NextResponse.json({ error: 'Currency service unavailable.' }, { status: 502 });
  }

  const payload = await upstream.text();
  return new Response(payload || null, {
    status: upstream.status,
    headers: {
      'content-type': upstream.headers.get('content-type') ?? 'application/json',
      'cache-control': 'no-store',
    },
  });
}
