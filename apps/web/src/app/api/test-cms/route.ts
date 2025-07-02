import { NextRequest, NextResponse } from 'next/server';
import { auth } from '@/auth';
import { environment } from '@/configs/environment';

export async function GET(request: NextRequest) {
  try {
    // Get the session to extract the access token
    const session = await auth();

    if (!session?.accessToken) {
      return NextResponse.json({ error: 'No access token found in session' }, { status: 401 });
    }

    // Get tenant ID from headers
    const tenantId = request.headers.get('X-Tenant-Id');

    // Make a test call to the CMS backend
    const headers: Record<string, string> = {
      Authorization: `Bearer ${session.accessToken}`,
      'Content-Type': 'application/json',
    };

    if (tenantId) {
      headers['X-Tenant-Id'] = tenantId;
    }

    // Test the auth/me endpoint
    const response = await fetch(`${environment.apiBaseUrl}/auth/me`, {
      method: 'GET',
      headers,
    });

    const data = await response.json();

    return NextResponse.json({
      success: true,
      status: response.status,
      cmsBackendUrl: environment.apiBaseUrl,
      tenantId: tenantId || null,
      sessionHasToken: !!session.accessToken,
      response: data,
      headers: {
        authorization: `Bearer ${session.accessToken?.substring(0, 20)}...`,
        tenantId: tenantId || null,
      },
    });
  } catch (error) {
    console.error('Direct CMS API test error:', error);

    return NextResponse.json(
      {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        cmsBackendUrl: environment.apiBaseUrl,
      },
      { status: 500 },
    );
  }
}
