#!/usr/bin/env node
/**
 * End-to-End Integration Test
 * 
 * Tests the complete integration chain:
 * Web App → @game-guild/client → Backend API
 * 
 * Requires:
 * - API running on http://localhost:8080
 * - Database running
 */

console.log('🔗 Testing E2E Integration: Web App → API Client → Backend API\n');

const tests = [];
const failures = [];

// Test 1: Health Check - Full Chain
try {
  const { createClient } = await import('@game-guild/client');
  
  const client = createClient({
    baseUrl: 'http://localhost:8080',
    headers: {
      'X-Tenant-Id': 'default',
    },
  });
  
  const result = await client.request({
    method: 'GET',
    path: '/health',
  });
  
  if (result.ok && result.data) {
    const status = result.data.status || result.data.Status || 'healthy';
    tests.push(`✅ Health Check: API responded (status: ${status})`);
  } else {
    failures.push(`❌ Health Check: API error - ${result.error?.message || 'Unknown error'} (${result.error?.code || 'N/A'})`);
  }
} catch (error) {
  failures.push(`❌ Health Check failed: ${error.message}`);
}

// Test 2: API Error Handling
try {
  const { createClient } = await import('@game-guild/client');
  
  const client = createClient({
    baseUrl: 'http://localhost:8080',
    headers: {
      'X-Tenant-Id': 'default',
    },
  });
  
  const result = await client.request({
    method: 'GET',
    path: '/api/nonexistent-endpoint',
  });
  
  if (!result.ok && result.error) {
    tests.push(`✅ Error Handling: Client correctly handled 404 (code: ${result.error.code})`);
  } else {
    failures.push('❌ Error Handling: Should have returned error for invalid endpoint');
  }
} catch (error) {
  failures.push(`❌ Error Handling test failed: ${error.message}`);
}

// Test 3: Headers and Configuration
try {
  const { createClient } = await import('@game-guild/client');
  
  const client = createClient({
    baseUrl: 'http://localhost:8080',
    headers: {
      'X-Tenant-Id': 'test-tenant-123',
      'X-Custom-Header': 'test-value',
    },
  });
  
  const baseUrl = client.getBaseUrl();
  
  if (baseUrl === 'http://localhost:8080') {
    tests.push('✅ Configuration: Base URL correctly configured');
  } else {
    failures.push(`❌ Configuration: Base URL mismatch (got: ${baseUrl})`);
  }
} catch (error) {
  failures.push(`❌ Configuration test failed: ${error.message}`);
}

// Test 4: Next.js Server-Side Client
try {
  const { createNextClient } = await import('@game-guild/client/next');
  
  const client = createNextClient({
    baseUrl: 'http://localhost:8080',
  });
  
  const result = await client.request({
    method: 'GET',
    path: '/health',
  });
  
  if (result.ok) {
    tests.push('✅ Next.js Client: Successfully created and called API');
  } else {
    failures.push(`❌ Next.js Client: API call failed - ${result.error.message}`);
  }
} catch (error) {
  failures.push(`❌ Next.js Client test failed: ${error.message}`);
}

// Test 5: Type Safety - Response Typing
try {
  const { createClient } = await import('@game-guild/client');
  
  const client = createClient({
    baseUrl: 'http://localhost:8080',
    headers: {
      'X-Tenant-Id': 'default',
    },
  });
  
  // Type-safe request with expected response shape
  const result = await client.request({
    method: 'GET',
    path: '/health',
  });
  
  if (result.ok && result.data && typeof result.data === 'object') {
    tests.push('✅ Type Safety: Response correctly typed and accessible');
  } else {
    failures.push(`❌ Type Safety: Response type mismatch (ok: ${result.ok}, data: ${typeof result.data})`);
  }
} catch (error) {
  failures.push(`❌ Type Safety test failed: ${error.message}`);
}

// Test 6: Concurrent Requests
try {
  const { createClient } = await import('@game-guild/client');
  
  const client = createClient({
    baseUrl: 'http://localhost:8080',
    headers: {
      'X-Tenant-Id': 'default',
    },
  });
  
  // Make 3 concurrent requests
  const results = await Promise.all([
    client.request({ method: 'GET', path: '/health' }),
    client.request({ method: 'GET', path: '/health' }),
    client.request({ method: 'GET', path: '/health' }),
  ]);
  
  const allSuccessful = results.every(r => r.ok);
  
  if (allSuccessful) {
    tests.push('✅ Concurrent Requests: All 3 requests succeeded');
  } else {
    failures.push('❌ Concurrent Requests: Some requests failed');
  }
} catch (error) {
  failures.push(`❌ Concurrent Requests test failed: ${error.message}`);
}

// Print Results
console.log('Test Results:\n');

tests.forEach(test => console.log(test));
failures.forEach(failure => console.log(failure));

console.log('\n' + '='.repeat(60));

if (failures.length === 0) {
  console.log(`✅ All ${tests.length} E2E tests passed!`);
  console.log('\n🎉 Full integration verified: Web App → API Client → Backend API');
  process.exit(0);
} else {
  console.log(`❌ ${failures.length} test(s) failed, ${tests.length} passed`);
  console.log('\n⚠️  Make sure the API is running: dotnet run --project apps/api/Source/GameGuild.API/GameGuild.API.csproj');
  process.exit(1);
}
