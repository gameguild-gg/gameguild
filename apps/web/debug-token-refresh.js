#!/usr/bin/env node

/**
 * Token Refresh Debug Tool
 * 
 * This script helps debug the JWT token refresh process by:
 * 1. Creating a test user
 * 2. Signing in to get initial tokens
 * 3. Testing the refresh token endpoint
 * 4. Validating token expiry handling
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

// Helper function to make HTTP requests
async function makeRequest(url, options = {}) {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  });
  
  const data = await response.json().catch(() => null);
  
  return {
    ok: response.ok,
    status: response.status,
    statusText: response.statusText,
    data,
    response
  };
}

// Decode JWT payload (without verification)
function decodeJWT(token) {
  try {
    if (!token || !token.includes('.')) return null;
    
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    
    let payload = parts[1];
    while (payload.length % 4) {
      payload += '=';
    }
    
    return JSON.parse(Buffer.from(payload, 'base64').toString('utf-8'));
  } catch (e) {
    console.error('❌ Failed to decode JWT:', e.message);
    return null;
  }
}

// Test user credentials
const TEST_USER = {
  email: `test-refresh-${Date.now()}@example.com`,
  password: 'TestPassword123!',
  username: `testuser${Date.now()}`
};

console.log('🚀 Starting Token Refresh Debug Session');
console.log('=' .repeat(50));
console.log(`API Base URL: ${API_BASE_URL}`);
console.log(`Test User Email: ${TEST_USER.email}`);
console.log('=' .repeat(50));

async function debugTokenRefresh() {
  try {
    // Step 1: Create test user
    console.log('\n📝 Step 1: Creating test user...');
    const signUpResult = await makeRequest(`${API_BASE_URL}/api/auth/sign-up`, {
      method: 'POST',
      body: JSON.stringify({
        Email: TEST_USER.email,
        Password: TEST_USER.password,
        Name: TEST_USER.username,
      }),
    });
    
    if (!signUpResult.ok) {
      if (signUpResult.status === 409) {
        console.log('⚠️ User already exists, continuing with sign-in...');
      } else {
        console.error('❌ Sign-up failed:', signUpResult.status, signUpResult.statusText);
        console.error('Response:', signUpResult.data);
        return;
      }
    } else {
      console.log('✅ Test user created successfully');
    }

    // Step 2: Sign in to get initial tokens
    console.log('\n🔐 Step 2: Signing in to get initial tokens...');
    const signInResult = await makeRequest(`${API_BASE_URL}/api/auth/sign-in`, {
      method: 'POST',
      body: JSON.stringify({
        Email: TEST_USER.email,
        Password: TEST_USER.password,
      }),
    });
    
    if (!signInResult.ok) {
      console.error('❌ Sign-in failed:', signInResult.status, signInResult.statusText);
      console.error('Response:', signInResult.data);
      return;
    }
    
    console.log('✅ Sign-in successful');
    const { accessToken, refreshToken, expiresAt, accessTokenExpiresAt, refreshTokenExpiresAt } = signInResult.data;
    
    if (!accessToken || !refreshToken) {
      console.error('❌ Missing tokens in sign-in response');
      console.log('Response data:', signInResult.data);
      return;
    }
    
    // Step 3: Analyze initial tokens
    console.log('\n🔍 Step 3: Analyzing initial tokens...');
    const accessPayload = decodeJWT(accessToken);
    const refreshPayload = decodeJWT(refreshToken);
    
    console.log('📋 Access Token Info:');
    console.log(`  Token: ${accessToken.substring(0, 50)}...`);
    if (accessPayload) {
      console.log(`  Issued At: ${new Date(accessPayload.iat * 1000).toISOString()}`);
      console.log(`  Expires At: ${new Date(accessPayload.exp * 1000).toISOString()}`);
      console.log(`  Time Until Expiry: ${Math.round((accessPayload.exp * 1000 - Date.now()) / 1000)}s`);
      console.log(`  Subject: ${accessPayload.sub}`);
      console.log(`  Issuer: ${accessPayload.iss}`);
    }
    
    console.log('\n📋 Refresh Token Info:');
    console.log(`  Token: ${refreshToken.substring(0, 50)}...`);
    if (refreshPayload) {
      console.log(`  Issued At: ${new Date(refreshPayload.iat * 1000).toISOString()}`);
      console.log(`  Expires At: ${new Date(refreshPayload.exp * 1000).toISOString()}`);
      console.log(`  Time Until Expiry: ${Math.round((refreshPayload.exp * 1000 - Date.now()) / 1000)}s`);
    }
    
    console.log('\n📋 Server Response Expiry Fields:');
    console.log(`  expiresAt: ${expiresAt}`);
    console.log(`  accessTokenExpiresAt: ${accessTokenExpiresAt}`);
    console.log(`  refreshTokenExpiresAt: ${refreshTokenExpiresAt}`);
    
    // Step 4: Test refresh token endpoint
    console.log('\n🔄 Step 4: Testing refresh token endpoint...');
    const refreshResult = await makeRequest(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      body: JSON.stringify({
        RefreshToken: refreshToken,
      }),
    });
    
    if (!refreshResult.ok) {
      console.error('❌ Token refresh failed:', refreshResult.status, refreshResult.statusText);
      console.error('Response:', refreshResult.data);
      return;
    }
    
    console.log('✅ Token refresh successful');
    const refreshData = refreshResult.data;
    
    // Step 5: Analyze refreshed tokens
    console.log('\n🔍 Step 5: Analyzing refreshed tokens...');
    const newAccessPayload = decodeJWT(refreshData.accessToken);
    const newRefreshPayload = decodeJWT(refreshData.refreshToken);
    
    console.log('📋 New Access Token Info:');
    console.log(`  Token: ${refreshData.accessToken.substring(0, 50)}...`);
    if (newAccessPayload) {
      console.log(`  Issued At: ${new Date(newAccessPayload.iat * 1000).toISOString()}`);
      console.log(`  Expires At: ${new Date(newAccessPayload.exp * 1000).toISOString()}`);
      console.log(`  Time Until Expiry: ${Math.round((newAccessPayload.exp * 1000 - Date.now()) / 1000)}s`);
      console.log(`  Subject: ${newAccessPayload.sub}`);
    }
    
    console.log('\n📋 New Refresh Token Info:');
    console.log(`  Token: ${refreshData.refreshToken.substring(0, 50)}...`);
    if (newRefreshPayload) {
      console.log(`  Issued At: ${new Date(newRefreshPayload.iat * 1000).toISOString()}`);
      console.log(`  Expires At: ${new Date(newRefreshPayload.exp * 1000).toISOString()}`);
      console.log(`  Time Until Expiry: ${Math.round((newRefreshPayload.exp * 1000 - Date.now()) / 1000)}s`);
    }
    
    console.log('\n📋 New Server Response Expiry Fields:');
    console.log(`  expiresAt: ${refreshData.expiresAt}`);
    console.log(`  accessTokenExpiresAt: ${refreshData.accessTokenExpiresAt}`);
    console.log(`  refreshTokenExpiresAt: ${refreshData.refreshTokenExpiresAt}`);
    
    // Step 6: Test authenticated endpoint with new token
    console.log('\n🔒 Step 6: Testing authenticated endpoint with new access token...');
    const authTestResult = await makeRequest(`${API_BASE_URL}/api/users/me`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${refreshData.accessToken}`,
      },
    });
    
    if (!authTestResult.ok) {
      console.error('❌ Authenticated request failed:', authTestResult.status, authTestResult.statusText);
      console.error('Response:', authTestResult.data);
    } else {
      console.log('✅ Authenticated request successful');
      console.log('User info:', authTestResult.data);
    }
    
    // Step 7: Test with old refresh token (should fail)
    console.log('\n🚫 Step 7: Testing with old refresh token (should fail)...');
    const oldRefreshResult = await makeRequest(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      body: JSON.stringify({
        RefreshToken: refreshToken, // old token
      }),
    });
    
    if (!oldRefreshResult.ok) {
      console.log('✅ Old refresh token correctly rejected');
      console.log(`   Status: ${oldRefreshResult.status} - ${oldRefreshResult.statusText}`);
    } else {
      console.warn('⚠️ Old refresh token was accepted (this might be a security issue)');
    }
    
    console.log('\n🎉 Token refresh debugging completed successfully!');
    
  } catch (error) {
    console.error('❌ Debugging failed with error:', error.message);
    console.error('Stack trace:', error.stack);
  }
}

// Run the debug session
if (typeof fetch === 'undefined') {
  // Node.js environment, need to import fetch
  import('node-fetch').then(({ default: fetch }) => {
    global.fetch = fetch;
    debugTokenRefresh();
  }).catch(() => {
    console.error('❌ Please install node-fetch: npm install node-fetch');
    console.log('Or run this in a browser environment where fetch is available');
  });
} else {
  // Browser environment
  debugTokenRefresh();
}
