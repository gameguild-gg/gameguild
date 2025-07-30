'use server';

export async function testCmsConnection() {
  const { auth } = await import('@/auth');
  const { environment } = await import('@/configs/environment');
  const { getHealth } = await import('@/lib/api/generated');
  const { createClient } = await import('@/lib/api/generated/client');

  try {
    // Get the session to extract the access token
    const session = await auth();

    if (!session?.accessToken) {
      throw new Error('No access token found in session');
    }

    // Create a configured API client
    const apiClient = createClient({
      baseUrl: environment.apiBaseUrl,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    // Test the CMS backend connection
    const result = await getHealth({
      client: apiClient,
    });

    return {
      success: true,
      cmsBackendUrl: environment.apiBaseUrl,
      sessionHasToken: !!session.accessToken,
      healthCheck: result.data,
      headers: {
        authorization: `Bearer ${session.accessToken?.substring(0, 20)}...`,
      },
    };
  } catch (error) {
    console.error('CMS API test error:', error);

    throw new Error(error instanceof Error ? error.message : 'CMS connection test failed');
  }
}
