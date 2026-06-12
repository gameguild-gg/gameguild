import { getToken } from '@/auth';
import { NextRequest, NextResponse } from 'next/server';

const DEFAULT_API_URL = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function getApiUrl(): string {
  return DEFAULT_API_URL.replace(/\/$/, '');
}

function mapReason(reason: unknown): number {
  const normalized = typeof reason === 'string' ? reason.trim().toLowerCase() : '';
  switch (normalized) {
    case 'inappropriate':
      return 1;
    case 'copyright':
      return 2;
    case 'spam':
      return 3;
    case 'violence':
      return 4;
    case 'harassment':
      return 5;
    case 'misinformation':
      return 6;
    case 'technical':
    default:
      return 99;
  }
}

function mapReviewDecision(status: unknown): number {
  const normalized = typeof status === 'string' ? status.trim().toLowerCase() : '';
  if (normalized === 'dismissed') return 0;
  return 5;
}

function badRequest(message: string): NextResponse {
  return NextResponse.json({ error: message }, { status: 400 });
}

async function getBackendError(response: Response): Promise<string> {
  try {
    const body = await response.json();
    if (typeof body?.title === 'string') return body.title;
    if (typeof body?.error === 'string') return body.error;
    if (typeof body?.message === 'string') return body.message;
  } catch {
    // Use the HTTP status text below.
  }

  return response.statusText || 'Backend request failed.';
}

async function getAuthHeader(): Promise<Record<string, string> | NextResponse> {
  const token = await getToken();
  if (!token) {
    return NextResponse.json({ error: 'You must be signed in to report content.' }, { status: 401 });
  }

  return { Authorization: `Bearer ${token}` };
}

export async function POST(request: NextRequest): Promise<NextResponse> {
  const authHeader = await getAuthHeader();
  if (authHeader instanceof NextResponse) return authHeader;

  const body = await request.json();
  const contentId = typeof body?.contentId === 'string' ? body.contentId.trim() : '';
  if (!GUID_PATTERN.test(contentId)) {
    return badRequest('This learning content is not backed by a reportable asset reference yet.');
  }

  const response = await fetch(`${getApiUrl()}/v1/assets/${contentId}:report`, {
    method: 'POST',
    headers: {
      ...authHeader,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      reason: mapReason(body?.reason ?? body?.reportType),
      description: typeof body?.description === 'string' ? body.description : null,
    }),
    cache: 'no-store',
  });

  if (!response.ok) {
    return NextResponse.json({ error: await getBackendError(response) }, { status: response.status });
  }

  const result = await response.json();
  return NextResponse.json(result);
}

export async function GET(request: NextRequest): Promise<NextResponse> {
  const authHeader = await getAuthHeader();
  if (authHeader instanceof NextResponse) return authHeader;

  const contentId = request.nextUrl.searchParams.get('contentId')?.trim() ?? '';
  if (!GUID_PATTERN.test(contentId)) {
    return badRequest('A valid asset reference id is required to load reports.');
  }

  const response = await fetch(`${getApiUrl()}/v1/admin/assets/${contentId}/reports`, {
    method: 'GET',
    headers: authHeader,
    cache: 'no-store',
  });

  if (!response.ok) {
    return NextResponse.json({ error: await getBackendError(response) }, { status: response.status });
  }

  return NextResponse.json(await response.json());
}

export async function PATCH(request: NextRequest): Promise<NextResponse> {
  const authHeader = await getAuthHeader();
  if (authHeader instanceof NextResponse) return authHeader;

  const body = await request.json();
  const reportId = typeof body?.reportId === 'string' ? body.reportId.trim() : '';
  if (!GUID_PATTERN.test(reportId)) {
    return badRequest('A valid report id is required.');
  }

  const response = await fetch(`${getApiUrl()}/v1/admin/assets/reports/${reportId}:review`, {
    method: 'POST',
    headers: {
      ...authHeader,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      decision: mapReviewDecision(body?.status),
      notes: typeof body?.notes === 'string' ? body.notes : null,
    }),
    cache: 'no-store',
  });

  if (!response.ok) {
    return NextResponse.json({ error: await getBackendError(response) }, { status: response.status });
  }

  return NextResponse.json(await response.json());
}
