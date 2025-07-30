// Test script to debug token refresh issues
// This can be run in the browser console on an authenticated page

async function debugTokenRefresh() {
  console.log('🔍 Debugging token refresh mechanism...');

  // Get current session
  const sessionResponse = await fetch('/api/auth/session');
  const session = await sessionResponse.json();

  console.log('Current session:', {
    hasSession: !!session,
    hasAccessToken: !!session?.accessToken,
    error: session?.error,
    user: session?.user?.email,
    expires: session?.expires,
  });

  if (session?.error === 'RefreshTokenError') {
    console.log('🚨 Token refresh error detected!');
    console.log('Triggering sign in...');
    window.location.href = '/api/auth/signin';
    return;
  }

  // Test API call
  console.log('Testing API call...');
  try {
    const response = await fetch('/api/test-create-project', {
      method: 'GET', // Use GET to just test auth
      headers: {
        'Content-Type': 'application/json',
      },
    });

    console.log('API Response:', response.status, response.statusText);

    if (response.status === 401) {
      console.log('🚨 API returned 401 - token is expired or invalid');
      console.log('Triggering sign in...');
      window.location.href = '/api/auth/signin';
    } else {
      console.log('✅ API call successful');
    }
  } catch (error) {
    console.error('API call failed:', error);
  }
}

// Auto-run the debug function
debugTokenRefresh();
