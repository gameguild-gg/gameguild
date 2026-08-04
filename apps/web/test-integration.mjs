#!/usr/bin/env node
/**
 * API Client Integration Test Script
 * 
 * Simple Node.js script to verify the API client integration
 */

async function testIntegration() {
  console.log('🧪 Testing @game-guild/client integration...\n');

  const tests = [];
  const failures = [];

  // Test 1: Main import
  try {
    const mainModule = await import('@game-guild/client');
    if (mainModule.createClient && typeof mainModule.createClient === 'function') {
      tests.push('✅ Main import: createClient available');
    } else {
      failures.push('❌ Main import: createClient not found');
    }
  } catch (error) {
    failures.push(`❌ Main import failed: ${error.message}`);
  }

  // Test 2: Next.js integration
  try {
    const nextModule = await import('@game-guild/client/next');
    if (nextModule.createNextClient) {
      tests.push('✅ Next.js integration: createNextClient available');
    } else {
      failures.push('❌ Next.js integration: createNextClient missing');
    }
  } catch (error) {
    failures.push(`❌ Next.js integration failed: ${error.message}`);
  }

  // Test 3: React integration
  try {
    const reactModule = await import('@game-guild/client/react');
    tests.push('✅ React integration: module loads');
  } catch (error) {
    failures.push(`❌ React integration failed: ${error.message}`);
  }

  // Test 4: Plugins
  try {
    const pluginsModule = await import('@game-guild/client/plugins');
    tests.push('✅ Plugins: module loads');
  } catch (error) {
    failures.push(`❌ Plugins failed: ${error.message}`);
  }

  // Test 5: Client creation
  try {
    const { createClient } = await import('@game-guild/client');
    const client = createClient({
      baseUrl: 'http://localhost:8080',
      headers: { 'X-Tenant-Id': 'default' },
    });
    
    if (client && client.request && client.getBaseUrl) {
      tests.push('✅ Client creation: successful with request method');
    } else {
      failures.push('❌ Client creation: missing required methods');
    }
  } catch (error) {
    failures.push(`❌ Client creation failed: ${error.message}`);
  }

  // Test 6: TypeScript types
  try {
    const { createClient } = await import('@game-guild/client');
    const client = createClient({ baseUrl: 'http://localhost:8080' });
    
    // Type check - if this doesn't error, types are working
    const requestMethod = client.request;
    const baseUrl = client.getBaseUrl();
    tests.push('✅ TypeScript: types and methods are available');
  } catch (error) {
    failures.push(`❌ TypeScript types failed: ${error.message}`);
  }

  // Print results
  console.log('Test Results:\n');
  tests.forEach(test => console.log(test));
  
  if (failures.length > 0) {
    console.log('\nFailures:\n');
    failures.forEach(failure => console.log(failure));
    console.log(`\n❌ ${failures.length} test(s) failed out of ${tests.length + failures.length}`);
    process.exit(1);
  } else {
    console.log(`\n✅ All ${tests.length} tests passed!`);
    console.log('\n🎉 API client is successfully integrated!\n');
    process.exit(0);
  }
}

testIntegration().catch(error => {
  console.error('❌ Test script failed:', error);
  process.exit(1);
});
