import { HttpClient, HttpRequest, HttpResponse } from '@/lib/core/http';
import { auth } from '@/auth';

export const dynamic = 'force-dynamic';

export class FetchHttpClientAdapter implements HttpClient {
  async request<T>(data: HttpRequest): Promise<HttpResponse<T>> {
    const session = await auth();

    // Build headers with auth and tenant context
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...data.headers,
    };

    // Add authorization header if available
    if (session?.accessToken) {
      headers.Authorization = `Bearer ${session.accessToken}`;
    }

    // Add tenant header if current tenant is available in session
    if ((session as any)?.currentTenant?.id) {
      headers['X-Tenant-Id'] = (session as any).currentTenant.id;
    }

    const response = await fetch(data.url, {
      method: data.method,
      body: data.body ? JSON.stringify(data.body) : undefined,
      headers,
    });

    const body = await response.json();

    return {
      statusCode: response.status,
      body,
    };
  }
}
