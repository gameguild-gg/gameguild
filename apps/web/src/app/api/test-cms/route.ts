import { NextRequest, NextResponse } from 'next/server';
import { auth } from '@/auth';
import { environment } from '@/configs/environment';
import { getUsersMe } from '@/lib/api/generated';
import { createClient } from '@/lib/api/generated/client';

export async function GET(request: NextRequest) {
  try {
    // Get the session to extract the access token
    const session = await auth();

    if (!session?.accessToken) {
      return NextResponse.json({ error: 'No access token found in session' }, { status: 401 });
    }

    // Get tenant ID from headers
    const tenantId = request.headers.get('X-Tenant-Id');

    // Create a configured API client
    const apiClient = createClient({
      baseUrl: environment.apiBaseUrl,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(tenantId && { 'X-Tenant-Id': tenantId }),
      },
    });

    // Use the generated API client to call the /users/me endpoint
    const result = await getUsersMe({
      client: apiClient,
    });

    return NextResponse.json({
      success: true,
      cmsBackendUrl: environment.apiBaseUrl,
      tenantId: tenantId || null,
      sessionHasToken: !!session.accessToken,
      user: result.data,
      headers: {
        authorization: `Bearer ${session.accessToken?.substring(0, 20)}...`,
        tenantId: tenantId || null,
      },
    });
  } catch (error) {
    console.error('CMS API test error:', error);

    return NextResponse.json(
      {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        cmsBackendUrl: environment.apiBaseUrl,
        // Include more details about the error for debugging
        errorDetails:
          error instanceof Error
            ? {
                name: error.name,
                message: error.message,
                stack: error.stack?.split('\n').slice(0, 5), // First 5 lines of stack trace
              }
            : null,
      },
      { status: 500 },
    );
  }
}
